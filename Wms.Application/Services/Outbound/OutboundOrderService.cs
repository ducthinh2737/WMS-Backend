using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.DTOS.Outbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Outbound;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Outbound;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;
using Wms.Application.Interfaces.Services.MasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Application.Services.Outbound
{
    public class OutboundOrderService : IOutboundOrderService
    {
        private readonly AppDbContext _dbContext;
        private readonly IInventoryService _inventoryService;
        private readonly IGoodsIssueService _goodsIssueService;
        private readonly IAllocationService _allocationService;
        private readonly IJwtService jwtService;
        private readonly IMapper _mapper;
        private readonly IWarehouseService _warehouse;
        private readonly IProductUomService _productUomService;
        private readonly ILogger<OutboundOrderService> _logger;

        public OutboundOrderService(
            AppDbContext dbContext,
            IMapper mapper,
            IInventoryService _inventory,
            IGoodsIssueService goodsIssueService,
            IAllocationService allocationService,
            IJwtService jwt,
            IWarehouseService warehouse,
            IProductUomService productUomService,
            ILogger<OutboundOrderService>? logger = null)
        {
            _inventoryService = _inventory;
            _goodsIssueService = goodsIssueService;
            _allocationService = allocationService;
            _dbContext = dbContext;
            _warehouse = warehouse;
            jwtService = jwt;
            _mapper = mapper;
            _productUomService = productUomService;
            _logger = logger ?? NullLogger<OutboundOrderService>.Instance;
        }

        #region Create / Update / Get

        public async Task<OutboundOrderDto> CreateOutboundOrderAsync(OutboundOrderDto dto)
        {
            foreach (var item in dto.Items)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    throw new BusinessException(
                        "PRODUCT_NOT_FOUND",
                        $"Sản phẩm \"{item.ProductId}\" không tồn tại"
                    );

                if (product.Type != ProductType.Production)
                    throw new BusinessException(
                        "INVALID_PRODUCT_TYPE",
                        $"Sản phẩm \"{product.Name}\" không phải là thành phẩm nên không thể bán"
                    );
            }

            var orderItems = new List<OutboundOrderItem>();
            foreach (var i in dto.Items)
            {
                var baseQty = await _productUomService.ConvertToBaseQuantityAsync(i.ProductId, i.UnitId, i.OrderQty);
                orderItems.Add(new OutboundOrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    OutboundOrderId = dto.Id,
                    Status = OutboundStatus.Pending,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.OrderQty,
                    UnitId = i.UnitId,
                    BaseQuantity = baseQty,
                    Issued_Qty = 0,
                    Price = i.Price,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var order = new OutboundOrder
            {
                Id = Guid.NewGuid(),
                Code = GenerateOutboundOrderCode(),
                CustomerId = dto.CustomerId,
                Status = OutboundStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = orderItems
            };

            if (jwtService.GetUserId().HasValue)
                order.CreatedBy = jwtService.GetUserId().Value;

            _dbContext.Set<OutboundOrder>().Add(order);

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
                    order.Code = GenerateOutboundOrderCode();
                }
            }

            var savedOrder = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == order.Id);

            var result = _mapper.Map<OutboundOrderDto>(savedOrder ?? order);
            await PopulateUnitNamesAsync(result);
            return result;
        }

        public async Task<OutboundOrderDto> GetOutboundOrderAsync(Guid orderId)
        {
            var entity = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.GoodsIssues)
                    .ThenInclude(gi => gi.Items)
                        .ThenInclude(giItem => giItem.Product)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (entity == null)
                throw new BusinessException("ORDER_NOT_FOUND", "OutboundOrder not found");

            var result = _mapper.Map<OutboundOrderDto>(entity);
            await PopulateUnitNamesAsync(result);
            return result;
        }

        public async Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId)
        {
            return await _goodsIssueService.GetGoodsIssueDetailAsync(goodsIssueId);
        }

        public async Task<List<OutboundOrderDto>> QueryOutboundOrdersAsync(OutboundOrderQueryDto dto)
        {
            var query = _dbContext.OutboundOrders
                .Include(x => x.Items)
                .Include(x => x.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.CustomerId.HasValue)
                query = query.Where(x => x.CustomerId == dto.CustomerId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == (OutboundStatus)dto.Status);

            if (dto.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= dto.CreatedFrom.Value);

            if (dto.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedAt <= dto.CreatedTo.Value);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var result = _mapper.Map<List<OutboundOrderDto>>(list);
            await PopulateUnitNamesAsync(result);
            return result;
        }

        public async Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto)
        {
            return await _goodsIssueService.QueryGoodsIssuesAsync(dto);
        }

        #endregion

        #region Approve / Reject

        public async Task<GoodsIssueDto> CreateProductionGIAsync(ProductionGoodsIssueCreateDto dto)
        {
            return await _goodsIssueService.CreateProductionGIAsync(dto);
        }

        public async Task<GoodsIssueDto> ApproveGIAsync(Guid giId)
        {
            return await _goodsIssueService.ApproveGIAsync(giId);
        }

        public async Task<OutboundOrderDto> ApproveOutboundOrderAsync(Guid orderId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var entity = await _dbContext.OutboundOrders
                        .Include(x => x.Items)
                        .FirstOrDefaultAsync(x => x.Id == orderId);

                    if (entity == null)
                        throw new BusinessException("ORDER_NOT_FOUND", "OutboundOrder not found");
                    if (entity.Status != OutboundStatus.Pending)
                        throw new BusinessException("ORDER_NOT_PENDING", "Only Pending orders can be approved");

                    entity.Status = OutboundStatus.Approve;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.ApproveBy = jwtService.GetUserId();
                    entity.ApprovedAt = DateTime.UtcNow;

                    foreach (var item in entity.Items.Where(x => x.Status == OutboundStatus.Pending))
                    {
                        item.Status = OutboundStatus.Approve;
                        item.UpdatedAt = DateTime.UtcNow;
                    }

                    var GroupWarehouse = entity.Items
                        .Where(x => x.Status == OutboundStatus.Approve)
                        .GroupBy(x => x.WarehouseId);

                    foreach (var group in GroupWarehouse)
                    {
                        var warehouseId = group.Key;

                        var gi = new GoodsIssue
                        {
                            Id = Guid.NewGuid(),
                            OutboundOrderId = entity.Id,
                            Code = GenerateGICode(),
                            Type = GIType.Outbound,
                            WarehouseId = warehouseId,
                            Status = GIStatus.Pending,
                            CreateAt = DateTime.UtcNow,
                            Items = new List<GoodsIssueItem>()
                        };

                        foreach (var item in group)
                        {
                            var gii = new GoodsIssueItem
                            {
                                Id = Guid.NewGuid(),
                                GoodsIssueId = gi.Id,
                                OutboundOrderItemId = item.Id,
                                ProductId = item.ProductId,
                                Status = GIStatus.Pending,
                                Quantity = item.Quantity,
                                UnitId = item.UnitId,
                                BaseQuantity = item.BaseQuantity,
                                Issued_Qty = item.Issued_Qty,
                                CreatedAt = DateTime.UtcNow,
                                Allocations = new List<GoodsIssueAllocate>()
                            };

                            await _allocationService.AllocateInventoryAsync(gii, warehouseId);
                            gi.Items.Add(gii);
                        }

                        _dbContext.Set<GoodsIssue>().Add(gi);
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var result = _mapper.Map<OutboundOrderDto>(entity);
                    await PopulateUnitNamesAsync(result);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error approving OutboundOrder {OrderId}: {Message}", orderId, ex.Message);
                    throw;
                }
            });
        }

        public async Task Picking(GoodsIssueItemDto dto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                int maxRetries = 3;
                int delayMs = 100;

                for (int retry = 1; retry <= maxRetries; retry++)
                {
                    using var transaction = await _dbContext.Database.BeginTransactionAsync(global::System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        _logger.LogInformation("=== START PICKING (Attempt {Retry}) - ProductId: {ProductId} ===", retry, dto.ProductId);

                        var gii = await _dbContext.GoodsIssueItems
                            .Include(x => x.Allocations)
                            .FirstOrDefaultAsync(s => s.Id == dto.Id);
                        if (gii == null)
                            throw new BusinessException("GII_NOT_FOUND", "GoodsIssueItem not found");

                        if (gii.Status == GIStatus.Complete)
                            throw new BusinessException("ITEM_ALREADY_COMPLETE", "Dòng hàng đã hoàn thành, không thể pick thêm.");

                        var gi = await _dbContext.GoodsIssues
                            .Include(s => s.Warehouse)
                            .FirstOrDefaultAsync(s => s.Id == dto.GoodsIssueId);
                        if (gi == null)
                            throw new BusinessException("GI_NOT_FOUND", "GoodsIssue not found");

                        // FIX BUG 2: Cho phép pick khi GI đang ở trạng thái Approve, Picking, Partically_Issued, hoặc Picked.
                        if (gi.Status != GIStatus.Approve && 
                            gi.Status != GIStatus.Picking && 
                            gi.Status != GIStatus.Partically_Issued && 
                            gi.Status != GIStatus.Picked)
                        {
                            throw new BusinessException(
                                "GI_NOT_APPROVED",
                                $"Phiếu xuất không ở trạng thái hợp lệ để pick (hiện tại: {gi.Status})."
                            );
                        }

                        var issuedLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                        if (issuedLocation == null)
                            throw new BusinessException("ISSUED_LOCATION_NOT_CONFIGURED", "Kho chưa cấu hình vị trí xuất hàng (Issue Location)");

                        // FIX BUG 3: Allocate inventory nếu chưa có (allocation xảy ra tại bước Pick)
                        var hasAllocations = gii.Allocations != null && gii.Allocations.Any();
                        if (!hasAllocations)
                        {
                            await _allocationService.AllocateInventoryAsync(gii, gi.WarehouseId);
                            // Reload allocations sau khi tạo
                            await _dbContext.Entry(gii).Collection(x => x.Allocations).LoadAsync();
                        }

                        var allocateIds = dto.Allocations.Select(x => x.Id).ToList();
                        var allAllocates = await _dbContext.GoodsIssueAllocates
                            .Where(a => allocateIds.Contains(a.Id))
                            .ToListAsync();

                        // 1. Calculate incoming changes and total picked target
                        var pickedInBaseMap = new Dictionary<Guid, decimal>();
                        decimal incomingQtySum = 0;
                        foreach (var itemDto in dto.Allocations)
                        {
                            var gia = allAllocates.FirstOrDefault(a => a.Id == itemDto.Id);
                            if (gia == null) continue;

                            decimal actualPicked = itemDto.PickedQty;
                            if (actualPicked <= 0) continue;

                            decimal remainingAllocatedInBase = gia.AllocatedQty - gia.PickedQty;
                            decimal remainingAllocatedInUom = await _productUomService.ConvertFromBaseQuantityAsync(dto.ProductId, gii.UnitId, remainingAllocatedInBase);

                            decimal actualPickedInBase;
                            if (Math.Abs(actualPicked - remainingAllocatedInUom) <= 0.00011m)
                            {
                                actualPickedInBase = remainingAllocatedInBase;
                            }
                            else
                            {
                                actualPickedInBase = await _productUomService.ConvertToBaseQuantityAsync(dto.ProductId, gii.UnitId, actualPicked);
                            }

                            // Check allocation-level constraint
                            if (gia.PickedQty + actualPickedInBase > gia.AllocatedQty)
                                throw new BusinessException("PICK_EXCEEDS_ALLOCATION", $"Số lượng pick vượt quá phân bổ (Allocation={gia.AllocatedQty}, Đã Pick={gia.PickedQty}, Yêu cầu thêm={actualPickedInBase}).");

                            pickedInBaseMap[gia.Id] = actualPickedInBase;
                            incomingQtySum += actualPickedInBase;
                        }

                        var currentTotalPicked = gii.Allocations.Sum(a => a.PickedQty);
                        // Check item-level constraint: totalPicked + incomingQtySum <= gii.BaseQuantity
                        if (currentTotalPicked + incomingQtySum > gii.BaseQuantity)
                            throw new BusinessException("PICK_EXCEEDS_REQUEST", $"Tổng số lượng pick vượt quá số lượng yêu cầu (Yêu cầu={gii.BaseQuantity}, Đã Pick={currentTotalPicked}, Yêu cầu thêm={incomingQtySum}).");

                        // 2. Execute stock adjustments
                        foreach (var itemDto in dto.Allocations)
                        {
                            var gia = allAllocates.FirstOrDefault(a => a.Id == itemDto.Id);
                            if (gia == null) continue;

                            if (!pickedInBaseMap.TryGetValue(gia.Id, out decimal actualPickedInBase) || actualPickedInBase <= 0)
                                continue;

                            if (!gia.LocationId.HasValue || gia.LocationId == Guid.Empty)
                            {
                                throw new BusinessException(
                                    "INVALID_LOCATION",
                                    "Allocation chưa có vị trí kho hợp lệ."
                                );
                            }

                            decimal actualPicked = await _productUomService.ConvertFromBaseQuantityAsync(dto.ProductId, gii.UnitId, actualPickedInBase);

                            gia.PickedQty += actualPickedInBase;
                            gia.Status = (gia.PickedQty >= gia.AllocatedQty) ? GIAStatus.Picked : GIAStatus.Picking;

                            // A. Unlock soft reservation on the storage location FIRST
                            // Otherwise, OnHand quantity may temporarily drop below LockedQuantity and trigger INSUFFICIENT_STOCK validation.
                            if (gia.LocationId.HasValue && gia.LocationId.Value != Guid.Empty)
                            {
                                await _inventoryService.UnlockStockAsync(
                                    gi.WarehouseId,
                                    gia.LocationId.Value,
                                    dto.ProductId,
                                    actualPickedInBase,
                                    $"Released reservation for picking GI Item {gii.Id}",
                                    gia.LotId
                                );
                            }

                            // B. Adjust storage stock DOWN (InventoryActionType.Pick) SECOND
                            await _inventoryService.AdjustPickingAsync(
                                gi.WarehouseId,
                                gia.LocationId.Value,
                                dto.ProductId,
                                actualPickedInBase,
                                InventoryActionType.Pick,
                                $"PICK-{gia.Id.ToString()[..8]}-{gia.PickedQty}",
                                gia.LotId,
                                unitId: gii.UnitId,
                                originalQty: actualPicked
                            );

                            // C. Adjust stage gate stock UP (InventoryActionType.Stage)
                            await _inventoryService.AdjustAsync(
                                gi.WarehouseId,
                                issuedLocation.Id,
                                dto.ProductId,
                                actualPickedInBase,
                                InventoryActionType.Stage,
                                gia.LotId,
                                refCode: $"STAGE-{gia.Id.ToString()[..8]}-{gia.PickedQty}",
                                unitId: gii.UnitId,
                                originalQty: actualPicked
                            );
                        }

                        // 3. Propagate Statuses
                        var totalPicked = gii.Allocations.Sum(a => a.PickedQty);
                        gii.Status = (totalPicked >= gii.BaseQuantity)
                            ? GIStatus.Picked
                            : (totalPicked > 0 ? GIStatus.Picking : gii.Status);

                        // Propagate status to parent GoodsIssue
                        var allGii = _dbContext.GoodsIssueItems.Local.Where(x => x.GoodsIssueId == gi.Id).ToList();
                        if (!allGii.Any())
                        {
                            allGii = await _dbContext.GoodsIssueItems.Where(x => x.GoodsIssueId == gi.Id).ToListAsync();
                        }
                        if (allGii.All(x => x.Status == GIStatus.Picked))
                        {
                            gi.Status = GIStatus.Picked;
                        }
                        else if (allGii.Any(x => x.Status == GIStatus.Picking || x.Status == GIStatus.Picked))
                        {
                            gi.Status = GIStatus.Picking;
                        }

                        await _dbContext.SaveChangesAsync();

                        // 4. Post-adjustment negative balance validation to prevent race conditions from producing invalid stock states
                        var updatedInventories = await _dbContext.Inventories
                            .Where(i => i.WarehouseId == gi.WarehouseId && i.ProductId == dto.ProductId)
                            .ToListAsync();

                        foreach (var inv in updatedInventories)
                        {
                            if (inv.OnHandQuantity < 0)
                                throw new BusinessException("NEGATIVE_STOCK_DETECTED", $"Số lượng tồn thực tế không được phép âm (Vị trí ID: {inv.LocationId}, Tồn: {inv.OnHandQuantity}).");
                            if (inv.LockedQuantity < 0)
                                throw new BusinessException("NEGATIVE_LOCK_DETECTED", $"Số lượng khóa không được phép âm (Vị trí ID: {inv.LocationId}, Khóa: {inv.LockedQuantity}).");
                            if ((inv.OnHandQuantity - inv.LockedQuantity) < 0)
                                throw new BusinessException("INSUFFICIENT_AVAILABLE_STOCK", $"Số lượng khả dụng không đủ (Vị trí ID: {inv.LocationId}, Thực tế: {inv.OnHandQuantity}, Đang khóa: {inv.LockedQuantity}).");
                        }

                        await transaction.CommitAsync();

                        // Enterprise Traceability Audit Logging
                        _logger.LogInformation("✅ Picking completed successfully. GoodsIssueId={GoodsIssueId}, ProductId={ProductId}, PickedBy={PickedBy}, TotalPicked={TotalPicked}, Timestamp={Timestamp}",
                            dto.GoodsIssueId, dto.ProductId, jwtService.GetUserId() ?? 0, totalPicked, DateTime.UtcNow);

                        break; // Success, exit retry loop
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await transaction.RollbackAsync();
                        if (retry == maxRetries)
                        {
                            _logger.LogError(ex, "Failed to complete Picking due to optimistic concurrency after maximum retries.");
                            throw;
                        }
                        _logger.LogWarning("Concurrency conflict detected during Picking. Retrying {Retry}/{MaxRetries}...", retry, maxRetries);
                        _dbContext.ChangeTracker.Clear();
                        await Task.Delay(delayMs * retry);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error in Picking: {Message}", ex.Message);
                        throw;
                    }
                }
            });
        }

        public async Task OutgoingStockCount(IssueGoodsDto dto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                int maxRetries = 3;
                int delayMs = 100;

                for (int retry = 1; retry <= maxRetries; retry++)
                {
                    using var tx = await _dbContext.Database.BeginTransactionAsync(global::System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        var gii = await _dbContext.GoodsIssueItems
                            .FirstOrDefaultAsync(x => x.Id == dto.GoodsIssueItemId);

                        if (gii == null) throw new BusinessException("GII_NOT_FOUND", "Không tìm thấy dòng hàng xuất kho.");
                        if (dto.IssuedQty <= 0) throw new BusinessException("INVALID_ISSUED_QTY", "Số lượng xuất phải lớn hơn 0.");

                        if (gii.Status == GIStatus.Pending || gii.Status == GIStatus.Approve)
                        {
                            throw new BusinessException(
                                "ITEM_NOT_PICKED",
                                "Dòng hàng chưa được pick."
                            );
                        }

                        decimal issuedQtyDec = dto.IssuedQty;

                        if (gii.Issued_Qty + issuedQtyDec > gii.Quantity)
                            throw new BusinessException("ISSUE_EXCEEDS_REQUEST", "Tổng số lượng xuất vượt quá số lượng yêu cầu trên phiếu.");

                        var pickedAllocates = await _dbContext.GoodsIssueAllocates
                            .Where(x => x.GoodsIssueItemId == gii.Id && x.PickedQty > 0)
                            .OrderBy(x => x.LotId)
                            .ToListAsync();

                        decimal totalCurrentlyAtGate = pickedAllocates.Sum(x => x.PickedQty - x.IssuedQty);

                        decimal totalCurrentlyAtGateInUom = await _productUomService.ConvertFromBaseQuantityAsync(gii.ProductId, gii.UnitId, totalCurrentlyAtGate);

                        decimal issuedQtyInBase;
                        if (Math.Abs(issuedQtyDec - totalCurrentlyAtGateInUom) <= 0.00011m)
                        {
                            issuedQtyInBase = totalCurrentlyAtGate;
                        }
                        else
                        {
                            issuedQtyInBase = await _productUomService.ConvertToBaseQuantityAsync(gii.ProductId, gii.UnitId, issuedQtyDec);
                        }

                        if (issuedQtyInBase > totalCurrentlyAtGate)
                            throw new BusinessException("ISSUE_EXCEEDS_PICKED", "Số lượng xuất vượt quá số lượng hàng đang có sẵn tại cổng xuất.");

                        var gi = await _dbContext.GoodsIssues
                            .FirstOrDefaultAsync(x => x.Id == gii.GoodsIssueId);

                        var issueLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                        if (issueLocation == null)
                            throw new BusinessException("ISSUE_LOCATION_NOT_CONFIGURED", "Kho chưa cấu hình vị trí xuất hàng (Issue Location).");

                        decimal qtyRemainingToIssueInBase = issuedQtyInBase;

                        foreach (var alloc in pickedAllocates)
                        {
                            if (qtyRemainingToIssueInBase <= 0) break;

                            decimal availableInThisAlloc = alloc.PickedQty - alloc.IssuedQty;
                            decimal takeFromThisLotInBase = Math.Min(qtyRemainingToIssueInBase, availableInThisAlloc);

                            if (takeFromThisLotInBase <= 0) continue;

                            decimal takeFromThisLotInUom = await _productUomService.ConvertFromBaseQuantityAsync(gii.ProductId, gii.UnitId, takeFromThisLotInBase);

                            // FIX BUG 4: Validate trước khi thực hiện bất kỳ thay đổi nào (check trước, cộng sau)
                            if (alloc.IssuedQty + takeFromThisLotInBase > alloc.PickedQty)
                            {
                                throw new BusinessException(
                                    "ISSUE_EXCEEDS_PICKED_ALLOC",
                                    $"Issued vượt picked quantity (Picked={alloc.PickedQty}, " +
                                    $"Đã issued={alloc.IssuedQty}, Yêu cầu thêm={takeFromThisLotInBase})."
                                );
                            }

                            // Adjust stock physically leaving the gate with action type Issue (trừ staging stock)
                            await _inventoryService.AdjustAsync(
                                gi.WarehouseId,
                                issueLocation.Id,
                                gii.ProductId,
                                takeFromThisLotInBase,
                                InventoryActionType.Issue,
                                alloc.LotId,
                                $"ISSUE-{alloc.Id.ToString()[..8]}-{alloc.IssuedQty}",
                                unitId: gii.UnitId,
                                originalQty: takeFromThisLotInUom
                            );

                            alloc.IssuedQty += takeFromThisLotInBase; // cập nhật sau khi thành công
                            alloc.Status = (alloc.IssuedQty >= alloc.PickedQty) ? GIAStatus.Complete : GIAStatus.Picked;
                            qtyRemainingToIssueInBase -= takeFromThisLotInBase;
                        }

                        gii.Issued_Qty += issuedQtyDec;

                        gii.Status = (gii.Issued_Qty >= gii.Quantity)
                            ? GIStatus.Complete
                            : GIStatus.Partically_Issued;

                        var allGiiOfThisGi = _dbContext.GoodsIssueItems
                            .Local
                            .Where(x => x.GoodsIssueId == gi.Id)
                            .ToList();

                        if (!allGiiOfThisGi.Any())
                        {
                            allGiiOfThisGi = await _dbContext.GoodsIssueItems
                                .Where(x => x.GoodsIssueId == gi.Id)
                                .ToListAsync();
                        }

                        var isGiComplete = allGiiOfThisGi.All(x => x.Status == GIStatus.Complete);
                        gi.Status = isGiComplete ? GIStatus.Complete : GIStatus.Partically_Issued;
                        gi.IssuedAt = DateTime.UtcNow;

                        if (gi.Type == GIType.Outbound && gii.OutboundOrderItemId.HasValue)
                        {
                            var item = await _dbContext.OutboundOrderItems
                                .FirstOrDefaultAsync(s => s.Id == gii.OutboundOrderItemId);

                            if (item != null)
                            {
                                item.Issued_Qty += issuedQtyDec;
                                item.Status = (item.Issued_Qty >= item.Quantity)
                                    ? OutboundStatus.Complete
                                    : OutboundStatus.Partically_Issued;

                                var order = await _dbContext.OutboundOrders
                                    .FirstOrDefaultAsync(s => s.Id == item.OutboundOrderId);

                                if (order != null)
                                {
                                    // FIX BUG 5: item đã được EF track trong session này, nên Local sẽ thấy status mới. Fallback DB, đảm bảo include item đang tracking.
                                    var allItemsOfThisOrder = _dbContext.OutboundOrderItems
                                        .Local
                                        .Where(s => s.OutboundOrderId == order.Id)
                                        .ToList();

                                    if (!allItemsOfThisOrder.Any())
                                    {
                                        allItemsOfThisOrder = await _dbContext.OutboundOrderItems
                                            .Where(s => s.OutboundOrderId == order.Id)
                                            .ToListAsync();
                                    }

                                    // Nếu vẫn không thấy item hiện tại trong list (edge case), thêm vào
                                    if (allItemsOfThisOrder.All(s => s.Id != item.Id))
                                        allItemsOfThisOrder.Add(item);

                                    var isOrderComplete = allItemsOfThisOrder.All(s => s.Status == OutboundStatus.Complete);
                                    order.Status = isOrderComplete ? OutboundStatus.Complete : OutboundStatus.Partically_Issued;
                                }
                            }
                        }

                        await _dbContext.SaveChangesAsync();

                        // Post-adjustment negative balance validation to prevent race conditions from producing invalid stock states
                        var updatedInventories = await _dbContext.Inventories
                            .Where(i => i.WarehouseId == gi.WarehouseId && i.ProductId == gii.ProductId)
                            .ToListAsync();

                        foreach (var inv in updatedInventories)
                        {
                            if (inv.OnHandQuantity < 0)
                                throw new BusinessException("NEGATIVE_STOCK_DETECTED", $"Số lượng tồn thực tế không được phép âm (Vị trí ID: {inv.LocationId}, Tồn: {inv.OnHandQuantity}).");
                            if (inv.LockedQuantity < 0)
                                throw new BusinessException("NEGATIVE_LOCK_DETECTED", $"Số lượng khóa không được phép âm (Vị trí ID: {inv.LocationId}, Khóa: {inv.LockedQuantity}).");
                            if ((inv.OnHandQuantity - inv.LockedQuantity) < 0)
                                throw new BusinessException("INSUFFICIENT_AVAILABLE_STOCK", $"Số lượng khả dụng không đủ (Vị trí ID: {inv.LocationId}, Thực tế: {inv.OnHandQuantity}, Đang khóa: {inv.LockedQuantity}).");
                        }

                        await tx.CommitAsync();

                        // Enterprise Traceability Audit Logging
                        _logger.LogInformation("✅ Issue Goods completed successfully. GoodsIssueId={GoodsIssueId}, ProductId={ProductId}, IssuedBy={IssuedBy}, TotalIssued={TotalIssued}, Timestamp={Timestamp}",
                            gii.GoodsIssueId, gii.ProductId, jwtService.GetUserId() ?? 0, gii.Issued_Qty, DateTime.UtcNow);

                        break; // Success, exit retry loop
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await tx.RollbackAsync();
                        if (retry == maxRetries)
                        {
                            _logger.LogError(ex, "Failed to complete Issue Goods due to optimistic concurrency after maximum retries.");
                            throw;
                        }
                        _logger.LogWarning("Concurrency conflict detected during Issue Goods. Retrying {Retry}/{MaxRetries}...", retry, maxRetries);
                        _dbContext.ChangeTracker.Clear();
                        await Task.Delay(delayMs * retry);
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Error in OutgoingStockCount: {Message}", ex.Message);
                        throw;
                    }
                }
            });
        }

        public async Task<GoodsIssue> CreateGIAsync(GoodsIssueDto dto)
        {
            return await _goodsIssueService.CreateGIAsync(dto);
        }

        private string GenerateGICode()
        {
            var today = DateTime.UtcNow.Date;
            var suffix = Guid.NewGuid().ToString()[..8].ToUpper();
            return $"GI-{today:yyyyMMdd}-{suffix}";
        }

        private string GenerateOutboundOrderCode()
        {
            var today = DateTime.UtcNow.Date;
            var suffix = Guid.NewGuid().ToString()[..8].ToUpper();
            return $"ORD-{today:yyyyMMdd}-{suffix}";
        }

        public async Task UpdateGIStatusAsync(Guid giId, GIStatus status)
        {
            await _goodsIssueService.UpdateGIStatusAsync(giId, status);
        }

        public async Task<OutboundOrderDto> RejectOutboundOrderAsync(Guid orderId)
        {
            var entity = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (entity == null)
                throw new BusinessException("ORDER_NOT_FOUND", "OutboundOrder not found");

            if (entity.Status != OutboundStatus.Pending)
                throw new BusinessException("ORDER_NOT_PENDING", "Only DRAFT orders can be rejected");

            entity.Status = OutboundStatus.Rejected;
            entity.UpdatedAt = DateTime.UtcNow;
            foreach (var item in entity.Items)
            {
                item.Status = OutboundStatus.Rejected;
            }

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<OutboundOrderDto>(entity);
        }

        #endregion

        #region Private Helpers

        private async Task PopulateUnitNamesAsync(OutboundOrderDto dto)
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
            if (dto.GoodsIssues != null)
            {
                foreach (var gi in dto.GoodsIssues)
                {
                    if (gi.Items != null)
                    {
                        foreach (var item in gi.Items)
                        {
                            if (unitNames.TryGetValue(item.UnitId, out var name))
                                item.UnitName = name;
                        }
                    }
                }
            }
        }

        private async Task PopulateUnitNamesAsync(List<OutboundOrderDto> dtos)
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
                if (dto.GoodsIssues != null)
                {
                    foreach (var gi in dto.GoodsIssues)
                    {
                        if (gi.Items != null)
                        {
                            foreach (var item in gi.Items)
                            {
                                if (unitNames.TryGetValue(item.UnitId, out var name))
                                    item.UnitName = name;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}