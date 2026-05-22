using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Outbound;
using Wms.Domain.Entity.Outbound;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Application.Services.Outbound
{
    public class AllocationService : IAllocationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<AllocationService> _logger;

        public AllocationService(
            AppDbContext dbContext,
            IInventoryService inventoryService,
            ILogger<AllocationService>? logger = null)
        {
            _dbContext = dbContext;
            _inventoryService = inventoryService;
            _logger = logger ?? NullLogger<AllocationService>.Instance;
        }

        public async Task AllocateInventoryAsync(GoodsIssueItem item, Guid warehouseId)
        {
            decimal remainingQty = item.BaseQuantity;

            // 1. Retrieve Available Locations
            var locations = await _inventoryService.GetAvailableLocations(
                item.ProductId,
                warehouseId
            );

            // 2. FEFO Sorting & Batches Expiry Filter
            var validLocations = locations
                .Where(loc => (!loc.ExpiryDate.HasValue || loc.ExpiryDate.Value > DateTime.UtcNow)
                           && (loc.Type == Wms.Domain.Enums.location.LocationType.Storage 
                            || loc.Type == Wms.Domain.Enums.location.LocationType.Picking))
                .ToList();

            if (!validLocations.Any())
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                throw new BusinessException(
                    "NO_VALID_BATCHES",
                    $"Không thể phân bổ sản phẩm '{product?.Name ?? item.ProductId.ToString()}'. " +
                    $"Tất cả {locations.Count} lô hàng trong kho đều đã hết hạn sử dụng. " +
                    $"Vui lòng kiểm tra lại tồn kho."
                );
            }

            // 3. Expiry Warning Logic
            decimal totalValidQty = validLocations.Sum(loc => loc.AvailableQty);
            if (totalValidQty < item.BaseQuantity)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                var expiredQty = locations
                    .Where(loc => loc.ExpiryDate.HasValue && loc.ExpiryDate.Value <= DateTime.UtcNow)
                    .Sum(loc => loc.AvailableQty);

                _logger.LogWarning(
                    "Sản phẩm '{ProductName}' không đủ hàng còn hạn! Yêu cầu: {Required}, Có sẵn: {Available}, Hết hạn: {Expired}",
                    product?.Name ?? item.ProductId.ToString(),
                    item.BaseQuantity,
                    totalValidQty,
                    expiredQty);
            }

            var sortedLocations = validLocations
                .OrderBy(loc => loc.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(loc => loc.LotCode)
                .ToList();

            _logger.LogInformation(
                "Allocating {Qty} units of ProductId={ProductId} in WarehouseId={WarehouseId}",
                item.BaseQuantity,
                item.ProductId,
                warehouseId);

            // 4. Allocation entries & soft stock reservation
            foreach (var loc in sortedLocations)
            {
                if (remainingQty <= 0)
                    break;

                var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                // Expiry Batch warnings (< 30 days away)
                if (loc.ExpiryDate.HasValue)
                {
                    var daysUntilExpiry = (loc.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
                    if (daysUntilExpiry < 30)
                    {
                        _logger.LogWarning(
                            "Allocation batch LotCode={LotCode} expires in {Days} days!",
                            loc.LotCode,
                            Math.Round(daysUntilExpiry));
                    }
                }

                var gia = new GoodsIssueAllocate
                {
                    Id = Guid.NewGuid(),
                    GoodsIssueItemId = item.Id,
                    LocationId = loc.Id,
                    LotId = loc.LotId,
                    AllocatedQty = allocQty,
                    PickedQty = 0,
                    IssuedQty = 0,
                    Status = GIAStatus.Planned
                };
                _dbContext.GoodsIssueAllocates.Add(gia);

                // Call soft reservation lock
                await _inventoryService.LockStockAsync(
                    warehouseId,
                    loc.Id,
                    item.ProductId,
                    allocQty,
                    $"Locked for allocation GI Item {item.Id}",
                    loc.LotId
                );

                remainingQty -= allocQty;
            }

            // 5. Backorder allocation if insufficient stock
            if (remainingQty > 0)
            {
                _logger.LogWarning(
                    "Backorder created for ProductId={ProductId}: {Qty} units",
                    item.ProductId,
                    remainingQty);

                var backorderGia = new GoodsIssueAllocate
                {
                    Id = Guid.NewGuid(),
                    GoodsIssueItemId = item.Id,
                    LocationId = null,
                    LotId = Guid.Empty,
                    AllocatedQty = remainingQty,
                    PickedQty = 0,
                    IssuedQty = 0,
                    Status = GIAStatus.Planned
                };
                _dbContext.GoodsIssueAllocates.Add(backorderGia);
            }
        }
    }
}
