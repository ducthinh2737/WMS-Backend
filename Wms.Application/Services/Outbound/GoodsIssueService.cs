using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.DTOS.Outbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Application.Interfaces.Services.Outbound;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Entity.Outbound;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Application.Services.Outbound
{
    public class GoodsIssueService : IGoodsIssueService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAllocationService _allocationService;
        private readonly IProductUomService _productUomService;
        private readonly IInventoryService _inventoryService;
        private readonly IMapper _mapper;
        private readonly ILogger<GoodsIssueService> _logger;

        public GoodsIssueService(
            AppDbContext dbContext,
            IAllocationService allocationService,
            IProductUomService productUomService,
            IInventoryService inventoryService,
            IMapper mapper,
            ILogger<GoodsIssueService>? logger = null)
        {
            _dbContext = dbContext;
            _allocationService = allocationService;
            _productUomService = productUomService;
            _inventoryService = inventoryService;
            _mapper = mapper;
            _logger = logger ?? NullLogger<GoodsIssueService>.Instance;
        }

        private string GenerateGICode()
        {
            var today = DateTime.UtcNow.Date;
            var suffix = Guid.NewGuid().ToString()[..8].ToUpper();
            return $"GI-{today:yyyyMMdd}-{suffix}";
        }

        public async Task<GoodsIssueDto> CreateProductionGIAsync(ProductionGoodsIssueCreateDto dto)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);

            if (warehouse == null)
                throw new BusinessException("WAREHOUSE_NOT_FOUND", "Kho không tồn tại");

            var giItems = new List<GoodsIssueItem>();
            foreach (var i in dto.Items)
            {
                var baseQty = await _productUomService.ConvertToBaseQuantityAsync(i.ProductId, i.UnitId, i.Quantity);
                giItems.Add(new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitId = i.UnitId,
                    BaseQuantity = baseQty,
                    Issued_Qty = 0,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var gi = new GoodsIssue
            {
                Id = Guid.NewGuid(),
                Code = GenerateGICode(),
                Type = GIType.Production,
                WarehouseId = dto.WarehouseId,
                CustomerId = dto.CustomerId,
                Address = dto.Address,
                Status = GIStatus.Pending,
                CreateAt = DateTime.UtcNow,
                Items = giItems
            };

            _dbContext.GoodsIssues.Add(gi);

            int retryCount = 0;
            while (true)
            {
                try
                {
                    await _dbContext.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex) when (retryCount < 5 && (ex.InnerException?.Message.Contains("Duplicate") == true || ex.InnerException?.Message.Contains("unique") == true))
                {
                    retryCount++;
                    gi.Code = GenerateGICode();
                }
            }

            var result = _mapper.Map<GoodsIssueDto>(gi);
            await PopulateUnitNamesAsync(result);
            return result;
        }

        public async Task<GoodsIssue> CreateGIAsync(GoodsIssueDto dto)
        {
            var warehousecheck = await _dbContext.Warehouses.FirstOrDefaultAsync(s => s.Id == dto.WarehouseId);
            if (warehousecheck == null)
                throw new BusinessException("WAREHOUSE_NOT_FOUND", "Kho không tồn tại");

            if (warehousecheck.WarehouseType != WarehouseType.RawMaterial)
                throw new BusinessException("INVALID_WAREHOUSE_TYPE", "Không thể xuất kho không thuộc loại vật liệu");

            var giItems = new List<GoodsIssueItem>();
            foreach (var i in dto.Items)
            {
                var baseQty = await _productUomService.ConvertToBaseQuantityAsync(i.ProductId, i.UnitId, i.Quantity);
                giItems.Add(new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitId = i.UnitId,
                    BaseQuantity = baseQty,
                    Issued_Qty = 0,
                    GoodsIssueId = dto.Id,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            var gi = new GoodsIssue
            {
                Id = dto.Id,
                Code = GenerateGICode(),
                OutboundOrderId = dto.OutboundOrderId,
                // FIX BUG 6: Thêm WarehouseId và gán Type lấy từ dto thay vì hardcode
                WarehouseId = dto.WarehouseId,
                Type = dto.Type,
                Status = (GIStatus)dto.Status,
                CreateAt = DateTime.UtcNow,
                Items = giItems
            };

            _dbContext.GoodsIssues.Add(gi);
            await _dbContext.SaveChangesAsync();

            return gi;
        }

        public async Task<GoodsIssueDto> ApproveGIAsync(Guid giId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Atomic approve check
                    var affected = await _dbContext.GoodsIssues
                        .Where(x => x.Id == giId && x.Status == GIStatus.Pending)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.Status, GIStatus.Approve)
                            .SetProperty(x => x.UpdateAt, DateTime.UtcNow)
                        );

                    if (affected == 0)
                        throw new BusinessException("GI_NOT_PENDING", "GoodsIssue đã được approve hoặc không tồn tại");

                    var gi = await _dbContext.GoodsIssues
                        .Include(x => x.Items)
                        .Include(x => x.Warehouse)
                        .FirstAsync(x => x.Id == giId);

                    // FIX BUG 1: Approve GI chỉ cập nhật status, không allocate. Allocation xảy ra tại bước Picking.
                    foreach (var item in gi.Items)
                    {
                        item.Status = GIStatus.Approve;
                        item.UpdatedAt = DateTime.UtcNow;
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var result = MapToDto(gi);
                    await PopulateUnitNamesAsync(result);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error approving GI: {Message}", ex.Message);
                    throw;
                }
            });
        }

        public async Task UpdateGIStatusAsync(Guid giId, GIStatus status)
        {
            var gi = await _dbContext.GoodsIssues.FirstOrDefaultAsync(x => x.Id == giId);
            if (gi == null)
                throw new BusinessException("GI_NOT_FOUND", "GoodsIssue không tồn tại");

            gi.Status = status;
            gi.UpdateAt = DateTime.UtcNow;

            var items = await _dbContext.GoodsIssueItems
                .Where(x => x.GoodsIssueId == giId)
                .ToListAsync();

            foreach (var item in items)
            {
                item.Status = status;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId)
        {
            var gi = await _dbContext.Set<GoodsIssue>()
                .Where(g => g.Id == goodsIssueId)
                .Include(g => g.OutboundOrder)
                .Include(g => g.Warehouse)
                .Include(g => g.Customer)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Product)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.Location)
                .FirstOrDefaultAsync();

            if (gi == null) return null;

            // FIX BUG 3 (Hỗ trợ hiển thị): Tự động allocate khi xem chi tiết để frontend có allocation hiển thị
            // AND: Tự động tái phân bổ (re-allocate) các phân bổ "Chưa xác định" (backorder) khi có hàng mới về.
            bool needSave = false;
            foreach (var item in gi.Items)
            {
                if (gi.Status == GIStatus.Approve || gi.Status == GIStatus.Picking)
                {
                    if (!item.Allocations.Any())
                    {
                        needSave = true;
                        break;
                    }
                    else
                    {
                        var backorders = item.Allocations.Where(a => a.LocationId == null && a.PickedQty == 0).ToList();
                        if (backorders.Any())
                        {
                            var available = await _inventoryService.GetAvailableLocations(item.ProductId, gi.WarehouseId);
                            var hasStock = available.Any(loc => 
                                (!loc.ExpiryDate.HasValue || loc.ExpiryDate.Value.Date >= DateTime.UtcNow.Date) &&
                                (loc.Type == LocationType.Storage || loc.Type == LocationType.Picking));

                            if (hasStock)
                            {
                                needSave = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (needSave)
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dbContext.Database.BeginTransactionAsync();
                    try
                    {
                        foreach (var item in gi.Items)
                        {
                            if (gi.Status == GIStatus.Approve || gi.Status == GIStatus.Picking)
                            {
                                if (!item.Allocations.Any())
                                {
                                    await _allocationService.AllocateInventoryAsync(item, gi.WarehouseId);
                                }
                                else
                                {
                                    var backorders = item.Allocations.Where(a => a.LocationId == null && a.PickedQty == 0).ToList();
                                    if (backorders.Any())
                                    {
                                        var available = await _inventoryService.GetAvailableLocations(item.ProductId, gi.WarehouseId);
                                        var hasStock = available.Any(loc => 
                                            (!loc.ExpiryDate.HasValue || loc.ExpiryDate.Value.Date >= DateTime.UtcNow.Date) &&
                                            (loc.Type == LocationType.Storage || loc.Type == LocationType.Picking));

                                        if (hasStock)
                                        {
                                            decimal qtyToReallocate = backorders.Sum(b => b.AllocatedQty);
                                            
                                            // Delete old backorders
                                            _dbContext.GoodsIssueAllocates.RemoveRange(backorders);
                                            
                                            // Re-run allocation for this remaining quantity
                                            await _allocationService.AllocateInventoryAsync(item, gi.WarehouseId, qtyToReallocate);
                                        }
                                    }
                                }
                            }
                        }
                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning(ex, "Auto-allocation failed during detail view of GoodsIssue {GoodsIssueId}: {Message}", goodsIssueId, ex.Message);
                    }
                });

                // Reload to populate navigation properties correctly
                gi = await _dbContext.Set<GoodsIssue>()
                    .Where(g => g.Id == goodsIssueId)
                    .Include(g => g.OutboundOrder)
                    .Include(g => g.Warehouse)
                    .Include(g => g.Customer)
                    .Include(g => g.Items)
                        .ThenInclude(i => i.Product)
                    .Include(g => g.Items)
                        .ThenInclude(i => i.Allocations)
                            .ThenInclude(a => a.Location)
                    .FirstOrDefaultAsync();

                if (gi == null) return null;
            }

            var productIds = gi.Items.Select(i => i.ProductId).Distinct().ToList();
            var uoms = await _dbContext.ProductUoms
                .Where(u => productIds.Contains(u.ProductId))
                .ToListAsync();

            var unitNames = await _dbContext.Units.ToDictionaryAsync(u => u.Id, u => u.Name);

            var itemsDto = new List<GoodsIssueItemDtoForFrontend>();

            foreach (var i in gi.Items)
            {
                var factor = uoms.FirstOrDefault(u => u.ProductId == i.ProductId && u.UnitId == i.UnitId)?.Factor ?? 1m;
                if (factor <= 0) factor = 1m;

                var itemDto = new GoodsIssueItemDtoForFrontend
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductCode = i.Product != null ? i.Product.Code : string.Empty,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    Quantity = i.Quantity,
                    UnitId = i.UnitId,
                    BaseQuantity = i.BaseQuantity,
                    PickedQty = Math.Round(i.Allocations.Sum(a => a.PickedQty) / factor, 4),
                    IssuedQty = i.Issued_Qty,
                    Status = (int)i.Status,
                    Allocations = i.Allocations.Select(a => new GoodsIssueAllocate1Dto
                    {
                        Id = a.Id,
                        LocationId = a.LocationId,
                        LocationCode = a.Location != null ? (a.Location.Code ?? "Chưa xác định") : "Chưa xác định",
                        AllocatedQty = Math.Round(a.AllocatedQty / factor, 4),
                        PickedQty = Math.Round(a.PickedQty / factor, 4),
                        IssuedQty = Math.Round(a.IssuedQty / factor, 4),
                        Status = (int)a.Status
                    }).ToList()
                };

                if (unitNames.TryGetValue(i.UnitId, out var name))
                    itemDto.UnitName = name;

                itemsDto.Add(itemDto);
            }

            return new GoodsIssueDetailDto
            {
                Id = gi.Id,
                Code = gi.Code,
                OutboundOrderCode = gi.OutboundOrder != null ? gi.OutboundOrder.Code : string.Empty,
                Type = gi.Type,
                WarehouseName = gi.Warehouse != null ? gi.Warehouse.Name : string.Empty,
                Status = (int)gi.Status,
                CustomerName = gi.Customer != null ? gi.Customer.Name : string.Empty,
                Address = gi.Address,
                Items = itemsDto
            };
        }

        public async Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto)
        {
            var query = _dbContext.GoodsIssues
                .Include(x => x.OutboundOrder)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Allocations).ThenInclude(s => s.Location)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.OutboundOrderId.HasValue)
                query = query.Where(x => x.OutboundOrderId == dto.OutboundOrderId.Value);

            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == (GIStatus)dto.Status.Value);

            if (dto.IssuedFrom.HasValue)
                query = query.Where(x => x.IssuedAt >= dto.IssuedFrom.Value);

            if (dto.IssuedTo.HasValue)
                query = query.Where(x => x.IssuedAt <= dto.IssuedTo.Value);

            query = query.OrderByDescending(x => x.IssuedAt);

            var entities = await query
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var resultList = _mapper.Map<List<GoodsIssueDto>>(entities);
            await PopulateUnitNamesAsync(resultList);

            for (int i = 0; i < entities.Count; i++)
            {
                resultList[i].IssuedAt = entities[i].IssuedAt;
            }

            return resultList;
        }

        private static GoodsIssueDto MapToDto(GoodsIssue gi)
        {
            return new GoodsIssueDto
            {
                Id = gi.Id,
                Code = gi.Code,
                OutboundOrderId = gi.OutboundOrderId,
                Type = gi.Type,
                WarehouseId = gi.WarehouseId,
                Status = gi.Status,
                CreatedAt = gi.CreateAt,
                UpdatedAt = gi.UpdateAt,
                IssuedAt = gi.IssuedAt,
                Items = gi.Items.Select(item => new GoodsIssueItemDto
                {
                    Id = item.Id,
                    GoodsIssueId = item.GoodsIssueId,
                    ProductId = item.ProductId,
                    OutboundOrderItemId = item.OutboundOrderItemId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity,
                    UnitId = item.UnitId,
                    BaseQuantity = item.BaseQuantity,
                    IssuedQty = item.Issued_Qty,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    Allocations = item.Allocations.Select(a => new GoodsIssueAllocateDto
                    {
                        Id = a.Id,
                        GoodsIssueItemId = a.GoodsIssueItemId,
                        LocationId = a.LocationId ?? Guid.Empty,
                        AllocatedQty = a.AllocatedQty,
                        PickedQty = a.PickedQty,
                        Status = a.Status
                    }).ToList()
                }).ToList()
            };
        }

        private async Task PopulateUnitNamesAsync(GoodsIssueDto dto)
        {
            if (dto == null) return;
            var unitNames = await _dbContext.Units.ToDictionaryAsync(u => u.Id, u => u.Name);
            if (dto.Items != null)
            {
                foreach (var item in dto.Items)
                {
                    if (unitNames.TryGetValue(item.UnitId, out var name))
                        item.UnitName = name;
                }
            }
        }

        private async Task PopulateUnitNamesAsync(List<GoodsIssueDto> dtos)
        {
            if (dtos == null || !dtos.Any()) return;
            var unitNames = await _dbContext.Units.ToDictionaryAsync(u => u.Id, u => u.Name);
            foreach (var dto in dtos)
            {
                if (dto.Items != null)
                {
                    foreach (var item in dto.Items)
                    {
                        if (unitNames.TryGetValue(item.UnitId, out var name))
                            item.UnitName = name;
                    }
                }
            }
        }
    }
}
