using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Inventorys
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        private readonly IWarehouseService warehouseService;

        public InventoryService(AppDbContext db, IWarehouseService warehouse)
        {
            _db = db;
            warehouseService = warehouse;
        }

        // =========================
        // GET INVENTORY
        // =========================
        public async Task<Inventory> GetOrCreateInventoryAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            Guid lotId)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId &&
                    x.LotId == lotId);

            if (inventory == null)
            {
                // Check local tracker in case it was added in the same transaction
                inventory = _db.Inventories.Local
                    .FirstOrDefault(x =>
                        x.WarehouseId == warehouseId &&
                        x.LocationId == locationId &&
                        x.ProductId == productId &&
                        x.LotId == lotId);
            }

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = lotId,
                    OnHandQuantity = 0,
                    LockedQuantity = 0,
                    InTransitQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Inventories.Add(inventory);
            }

            return inventory;
        }

        public async Task<List<LocationQtyDto>> GetAvailableLocations(int productId, Guid warehouseId)
        {
            return await _db.Inventories
                .Include(x => x.Lot)
                .Include(x => x.Product)
                .Where(inv => inv.WarehouseId == warehouseId
                    && inv.ProductId == productId
                    && (inv.OnHandQuantity - inv.LockedQuantity) > 0)
                .Select(inv => new LocationQtyDto
                {
                    Id = inv.LocationId ?? Guid.Empty,
                    WarehouseId = inv.WarehouseId,
                    Type = _db.Locations.Where(l => l.Id == inv.LocationId).Select(l => l.Type).FirstOrDefault(),
                    Code = _db.Locations.Where(l => l.Id == inv.LocationId).Select(l => l.Code).FirstOrDefault(),
                    AvailableQty = inv.OnHandQuantity - inv.LockedQuantity,
                    LotId = inv.LotId,
                    LotCode = inv.Lot.Code,
                    ExpiryDate = inv.Lot.ExpiryDate,
                    ManufacturingDate = inv.Lot.ManufacturingDate
                })
                .ToListAsync();
        }

        public async Task PutAway(PutawayDto dto)
        {
            if (dto.Qty <= 0) throw new BusinessException("INVALID_QUANTITY", "Số lượng putaway phải > 0");

            var fromLoc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == dto.FromLocationId);
            var toLoc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == dto.ToLocationId);

            if (fromLoc == null || toLoc == null) throw new BusinessException("LOCATION_NOT_FOUND", "Vị trí không tồn tại");

            // Validate: Receiving -> Storage
            if (fromLoc.Type != LocationType.Receiving || toLoc.Type != LocationType.Storage)
            {
                throw new BusinessException("INVALID_PUTAWAY_FLOW", "Putaway chỉ được phép từ Receiving sang Storage");
            }

            var lot = await _db.Lots.FirstOrDefaultAsync(s => s.Id == dto.LotId);
            if (lot == null) throw new BusinessException("LOT_NOT_FOUND", "Lô hàng không tồn tại");

            var transferRef = $"PUTAWAY-{Guid.NewGuid()}";

// OUT
await AdjustAsync(
    dto.WarehouseId,
    dto.FromLocationId,
    dto.ProductId,
    dto.Qty,
    InventoryActionType.TransferOut,
    lotId: dto.LotId,
    refCode: $"{transferRef}-OUT",
    note: $"Putaway from {fromLoc.Code} to {toLoc.Code}"
);

// IN
await AdjustAsync(
    dto.WarehouseId,
    dto.ToLocationId,
    dto.ProductId,
    dto.Qty,
    InventoryActionType.TransferIn,
    lotId: dto.LotId,
    refCode: $"{transferRef}-IN",
    note: $"Putaway from {fromLoc.Code} to {toLoc.Code}"
);

            // SaveChanges handled by caller
        }

        public async Task<InventoryDto?> GetAsync(Guid id)
        {
            return await _db.Inventories
                .Include(x => x.Lot)
                .Include(x => x.Product)
                .Select(inv => new InventoryDto
                {
                    Id = inv.Id,
                    WarehouseId = inv.WarehouseId,
                    LocationId = inv.LocationId,
                    ProductId = inv.ProductId,
                    LotId = inv.LotId,
                    LotCode = inv.Lot.Code,
                    OnHandQuantity = inv.OnHandQuantity,
                    LockedQuantity = inv.LockedQuantity,
                    InTransitQuantity = inv.InTransitQuantity,
                    LocationType = _db.Locations.Where(l => l.Id == inv.LocationId).Select(l => l.Type).FirstOrDefault()
                })
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<InventoryDto>> GetInventoryByProductType(ProductType1Dto dto)
        {
            return await _db.Inventories
                .Where(i => i.Product.Type == dto.ProductType)
                .GroupBy(i => new { i.ProductId, i.WarehouseId })
                .Select(g => new InventoryDto
                {
                    ProductId = g.Key.ProductId,
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = _db.Warehouses.Where(w => w.Id == g.Key.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                    ProductName = _db.Products.Where(p => p.Id == g.Key.ProductId).Select(p => p.Name).FirstOrDefault(),
                    ProductCode = _db.Products.Where(p => p.Id == g.Key.ProductId).Select(p => p.Code).FirstOrDefault(),
                    OnHandQuantity = g.Sum(x => x.OnHandQuantity),
                    LockedQuantity = g.Sum(x => x.LockedQuantity),
                    InTransitQuantity = g.Sum(x => x.InTransitQuantity),
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            var query = _db.Inventories
                .Include(x => x.Product)
                .Include(x => x.Lot)
                .AsNoTracking()
                .AsQueryable();

            if (dto.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == dto.WarehouseId);
            if (dto.LocationId.HasValue) query = query.Where(x => x.LocationId == dto.LocationId);
            if (dto.ProductId.HasValue) query = query.Where(x => x.ProductId == dto.ProductId);
            if (dto.ProductIds != null && dto.ProductIds.Any()) query = query.Where(x => dto.ProductIds.Contains(x.ProductId));
            if (!string.IsNullOrEmpty(dto.LotCode)) query = query.Where(x => x.Lot.Code.Contains(dto.LotCode));

            return await query
                .Select(inv => new InventoryDto
                {
                    Id = inv.Id,
                    WarehouseId = inv.WarehouseId,
                    WarehouseName = _db.Warehouses.Where(w => w.Id == inv.WarehouseId).Select(w => w.Name).FirstOrDefault(),
                    LocationId = inv.LocationId,
                    LocationCode = _db.Locations.Where(l => l.Id == inv.LocationId).Select(l => l.Code).FirstOrDefault(),
                    LocationType = _db.Locations.Where(l => l.Id == inv.LocationId).Select(l => l.Type).FirstOrDefault(),
                    ProductId = inv.ProductId,
                    ProductName = inv.Product.Name,
                    ProductCode = inv.Product.Code,
                    LotId = inv.LotId,
                    LotCode = inv.Lot.Code,
                    ExpiryDate = inv.Lot.ExpiryDate,
                    OnHandQuantity = inv.OnHandQuantity,
                    LockedQuantity = inv.LockedQuantity,
                    InTransitQuantity = inv.InTransitQuantity,
                })
                .ToListAsync();
        }

        public async Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId)
        {
            return await _db.InventoryHistories
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new InventoryHistoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    LotId = x.LotId,
                    LotCode = x.LotCode,
                    QuantityChange = x.QuantityChange,
                    ActionType = x.ActionType,
                    ReferenceCode = x.ReferenceCode,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        // =========================
        // ADJUST INVENTORY (UNIFIED)
        // =========================
        public async Task AdjustAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            InventoryActionType actionType,
            Guid? lotId = null,
            string? refCode = null,
            string? lotCode = null,
            DateTime? expiryDate = null,
            DateTime? manufacturingDate = null,
            string? note = null)
        {
            if (qty == 0 && actionType != InventoryActionType.StockTakeAdjustment) return;

            // 1. Idempotency Check
            if (!string.IsNullOrEmpty(refCode))
            {
                var isProcessed = await _db.InventoryTransactions.AnyAsync(t =>
                    t.ReferenceCode == refCode &&
                    t.ProductId == productId &&
                    t.LotId == (lotId ?? Guid.Empty) &&
                    t.LocationId == locationId &&
                    t.ActionType == actionType);

                if (isProcessed) return;
            }

            // 2. Resolve Lot
            Guid finalLotId;
            string finalLotCode = lotCode ?? "";
            if (lotId.HasValue)
            {
                finalLotId = lotId.Value;
                var lot = await _db.Lots.FindAsync(lotId.Value);
                finalLotCode = lot?.Code ?? "";
            }
            else
            {
                var lot = await _db.Lots.FirstOrDefaultAsync(x => x.productId == productId && x.Code == lotCode);
                if (lot == null)
                {
                    lot = new Lot
                    {
                        Id = Guid.NewGuid(),
                        productId = productId,
                        Code = lotCode,
                        ExpiryDate = expiryDate?.Date,
                        ManufacturingDate = manufacturingDate?.Date,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Lots.Add(lot);
                }
                finalLotId = lot.Id;
            }

            // 3. Calculate Signed Qty
            decimal signedQty = actionType switch
            {
                InventoryActionType.Receive or InventoryActionType.TransferIn or InventoryActionType.AdjustIncrease => Math.Abs(qty),
                InventoryActionType.TransferOut or InventoryActionType.AdjustDecrease or InventoryActionType.Issue or InventoryActionType.Lock => -Math.Abs(qty),
                _ => qty
            };

            // 4. Update Inventory
            var inv = await GetOrCreateInventoryAsync(warehouseId, locationId, productId, finalLotId);

            // Validation: Prevent negative inventory (OnHand - Locked)
            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < inv.LockedQuantity))
            {
                throw new BusinessException("INSUFFICIENT_STOCK", $"Không đủ tồn kho khả dụng (Có: {inv.AvailableQuantity}, Yêu cầu: {Math.Abs(signedQty)})");
            }

            inv.OnHandQuantity += signedQty;
            Console.WriteLine(
    $"Inventory Updated | Loc={locationId} | Product={productId} | Qty={inv.OnHandQuantity}"
);
            inv.UpdatedAt = DateTime.UtcNow;

            // 5. Record Transaction (for consistency/idempotency)
            if (!string.IsNullOrEmpty(refCode))
            {
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ReferenceCode = refCode,
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = finalLotId,
                    ActionType = actionType,
                    Quantity = signedQty,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 6. Record History (for audit)
            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                LotId = finalLotId,
                LotCode = finalLotCode,
                QuantityChange = signedQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task AdjustPickingAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, InventoryActionType actionType, string refCode, Guid lotId)
        {
            await AdjustAsync(warehouseId, locationId, productId, qty, actionType, lotId: lotId, refCode: refCode);
        }

        public async Task Adjust1Async(Guid warehouseId, Guid? locationId, int productId, decimal qtyChange, InventoryActionType actionType, string? refCode, string? note = null)
        {
            // Fallback for legacy calls
            var locId = locationId ?? Guid.Empty; // Should ideally be a real location
            await AdjustAsync(warehouseId, locId, productId, qtyChange, actionType, refCode: refCode, note: note);
        }

        public async Task<List<LocationStockDto>> GetAvailableLocationsByLot(
            int productId,
            Guid warehouseId,
            Guid lotId)
        {
            return await _db.Inventories
                .Where(x =>
                    x.ProductId == productId &&
                    x.WarehouseId == warehouseId &&
                    x.LotId == lotId &&
                    (x.OnHandQuantity - x.LockedQuantity) > 0
                    && x.LocationId != null)
                .Select(x => new LocationStockDto
                {
                    Id = x.LocationId!.Value,
                    OnHandQty = x.OnHandQuantity,
                    LockedQty = x.LockedQuantity,
                })
                .ToListAsync();
        }

        // =========================
        // LOCK / UNLOCK STOCK
        // =========================
        public async Task LockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, string? note = null)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId);
            if (inv == null) throw new BusinessException("INVENTORY_NOT_FOUND", "Không tìm thấy tồn kho");

            if (inv.AvailableQuantity < qty) throw new BusinessException("INSUFFICIENT_STOCK", "Không đủ tồn kho khả dụng để khóa");

            inv.LockedQuantity += qty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = qty,
                ActionType = InventoryActionType.Lock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task UnlockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, string? note = null)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId);
            if (inv == null) throw new BusinessException("INVENTORY_NOT_FOUND", "Không tìm thấy tồn kho");

            if (inv.LockedQuantity < qty) throw new BusinessException("INVALID_UNLOCK", "Không thể mở khóa nhiều hơn số lượng đã khóa");

            inv.LockedQuantity -= qty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = -qty,
                ActionType = InventoryActionType.Unlock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}