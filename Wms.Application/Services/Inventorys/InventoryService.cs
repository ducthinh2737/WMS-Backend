using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Wms.Application.Services.Inventorys
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        public static readonly Guid RECEIVING_LOCATION_GUID = new Guid("11111111-2222-3333-4444-555555555555");
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
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = lotId,
                    OnHandQuantity = 0,
                    LockedQuantity = 0,
                    InTransitQuantity = 0
                };

                _db.Inventories.Add(inventory);
            }

            return inventory;
        }

        public async Task<List<LocationQtyDto>> GetAvailableLocations(int productId, Guid warehouseId)
        {
            var result = await (
                from inv in _db.Inventories
                join loc in _db.Locations on inv.LocationId equals loc.Id
                join lot in _db.Lots on inv.LotId equals lot.Id
                where inv.WarehouseId == warehouseId
                    && inv.ProductId == productId
                    && (inv.OnHandQuantity - inv.LockedQuantity) > 0
                select new LocationQtyDto
                {
                    Id = loc.Id,
                    WarehouseId = inv.WarehouseId,
                    Type = loc.Type,
                    Code = loc.Code,
                    AvailableQty = inv.OnHandQuantity - inv.LockedQuantity,
                    Description = loc.Description,
                    IsActive = loc.IsActive,
                    CreatedAt = loc.CreatedAt,
                    UpdatedAt = loc.UpdatedAt,
                    LotId = inv.LotId,
                    LotCode = lot.Code,
                    ExpiryDate = lot.ExpiryDate,
                    ManufacturingDate = lot.ManufacturingDate
                }
            ).ToListAsync();

            return result;
        }

        public async Task PutAway(PutawayDto dto)
        {
            if (dto.Qty <= 0) throw new Exception("Số lượng putaway phải > 0");
            var lotcheck = await _db.Lots.FirstOrDefaultAsync(s => s.Id == dto.LotId);

            // 1️⃣ Trừ ở Receiving location
            await AdjustAsync(
                dto.WarehouseId,
                dto.FromLocationId,
                dto.ProductId,
                dto.Qty,
                InventoryActionType.TransferOut,
                refCode: "PUTAWAY",
                lotId: dto.LotId,
                lotCode: lotcheck.Code,
                note: "Putaway from Receiving"
            );

            await AdjustAsync(
                dto.WarehouseId,
                dto.ToLocationId,
                dto.ProductId,
                dto.Qty,
                InventoryActionType.TransferIn,
                refCode: "PUTAWAY",
                lotId: dto.LotId,
                note: "Putaway to Storage"
            );

            await _db.SaveChangesAsync();
        }

        public async Task<InventoryDto?> GetAsync(Guid id)
        {
            return await _db.Inventories
                .Join(_db.Locations,
                      inv => inv.LocationId,
                      loc => loc.Id,
                      (inv, loc) => new InventoryDto
                      {
                          Id = inv.Id,
                          WarehouseId = inv.WarehouseId,
                          LocationId = inv.LocationId,
                          ProductId = inv.ProductId,
                          OnHandQuantity = inv.OnHandQuantity,
                          LockedQuantity = inv.LockedQuantity,
                          InTransitQuantity = inv.InTransitQuantity,
                          LocationType = loc.Type
                      })
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<InventoryDto>> GetInventoryByProductType(ProductType1Dto dto)
        {
            var result = await _db.Inventories
                .Where(i => i.Product.Type == dto.ProductType)
                .GroupBy(i => new
                {
                    i.ProductId,
                    i.WarehouseId
                })
                .Select(g => new InventoryDto
                {
                    ProductId = g.Key.ProductId,
                    WarehouseId = g.Key.WarehouseId,

                    WarehouseName = _db.Warehouses
                        .Where(w => w.Id == g.Key.WarehouseId)
                        .Select(w => w.Name)
                        .FirstOrDefault(),

                    ProductName = _db.Products
                        .Where(p => p.Id == g.Key.ProductId)
                        .Select(p => p.Name)
                        .FirstOrDefault(),

                    ProductCode = _db.Products
                        .Where(p => p.Id == g.Key.ProductId)
                        .Select(p => p.Code)
                        .FirstOrDefault(),

                    OnHandQuantity = g.Sum(x => x.OnHandQuantity),
                    LockedQuantity = g.Sum(x => x.LockedQuantity),
                    InTransitQuantity = g.Sum(x => x.InTransitQuantity),
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            var query = _db.Inventories
                .Include(x => x.Product)
                .Include(x => x.Lot)
                .AsNoTracking()
                .AsQueryable();

            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId);

            if (dto.LocationId.HasValue)
                query = query.Where(x => x.LocationId == dto.LocationId);

            if (dto.ProductId.HasValue)
                query = query.Where(x => x.ProductId == dto.ProductId);

            if (dto.ProductIds != null && dto.ProductIds.Any())
                query = query.Where(x => dto.ProductIds.Contains(x.ProductId));

            if (!string.IsNullOrEmpty(dto.LotCode))
                query = query.Where(x => x.Lot.Code.Contains(dto.LotCode));

            return await query
                .Select(inv => new InventoryDto
                {
                    Id = inv.Id,
                    WarehouseId = inv.WarehouseId,
                    WarehouseName = _db.Warehouses
                        .Where(w => w.Id == inv.WarehouseId)
                        .Select(w => w.Name)
                        .FirstOrDefault(),

                    LocationId = inv.LocationId,
                    LocationCode = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Code)
                        .FirstOrDefault(),
                    LocationType = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Type)
                        .FirstOrDefault(),

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

        // =========================
        // INVENTORY HISTORY
        // =========================
        public async Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId)
            => await _db.InventoryHistories
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new InventoryHistoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    QuantityChange = x.QuantityChange,
                    ActionType = x.ActionType,
                    ReferenceCode = x.ReferenceCode,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

        // =========================
        // ADJUST INVENTORY
        // =========================
        // Normalize DateTime về đúng ngày (bỏ phần giờ)
        // Frontend gửi "YYYY-MM-DD" → ASP.NET parse thành Unspecified 00:00:00 → .Date là đúng
        private static DateTime? NormalizeDateOnly(DateTime? dt)
        {
            if (dt == null) return null;
            return dt.Value.Date; // luôn lấy phần ngày, bỏ giờ
        }

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
            if (qty == 0)
    return;

            // Normalize: chỉ giữ phần ngày, bỏ giờ để tránh lệch timezone
            expiryDate = NormalizeDateOnly(expiryDate);
            manufacturingDate = NormalizeDateOnly(manufacturingDate);

            // 1. XÁC ĐỊNH LOT ID
            Guid finalLotId;
            if (lotId.HasValue)
            {
                finalLotId = lotId.Value;
            }
            else
            {
                // Tìm theo LotCode, không có thì tạo mới
                var lot = await _db.Lots.FirstOrDefaultAsync(x => x.productId == productId && x.Code == lotCode);
                if (lot == null)
                {
                    lot = new Lot
                    {
                        Id = Guid.NewGuid(),
                        productId = productId,
                        Code = lotCode,
                        ExpiryDate = expiryDate,
                        ManufacturingDate = manufacturingDate,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Lots.Add(lot);
                }
                else
                {
                    // Cập nhật nếu lot đã tồn tại nhưng chưa có ngày
                    if (lot.ExpiryDate == null && expiryDate != null)
                        lot.ExpiryDate = expiryDate;
                    if (lot.ManufacturingDate == null && manufacturingDate != null)
                        lot.ManufacturingDate = manufacturingDate;
                }
                finalLotId = lot.Id;
            }

            // 2. TÍNH TOÁN DẤU
            decimal signedQty;

switch (actionType)
{
    case InventoryActionType.Receive:
    case InventoryActionType.TransferIn:
    case InventoryActionType.AdjustIncrease:
        signedQty = Math.Abs(qty);
        break;

    case InventoryActionType.TransferOut:
    case InventoryActionType.AdjustDecrease:
    case InventoryActionType.Issue:
        signedQty = -Math.Abs(qty);
        break;

    case InventoryActionType.StockTakeAdjustment:
        // KIỂM KÊ CHO PHÉP ÂM/DƯƠNG
        signedQty = qty;
        break;

    default:
        signedQty = qty;
        break;
}

            // 3. CẬP NHẬT BẢNG INVENTORY
            var inv = await _db.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId && x.LocationId == locationId &&
                x.ProductId == productId && x.LotId == finalLotId);

            if (inv == null)
            {
                if (signedQty < 0) throw new Exception("Không tìm thấy tồn kho để trừ.");
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = finalLotId,
                    OnHandQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Inventories.Add(inv);
            }

            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < 0))
                throw new Exception($"Lô hàng này tại vị trí này không đủ tồn kho (Còn: {inv.OnHandQuantity})");

            inv.OnHandQuantity += signedQty;
            inv.UpdatedAt = DateTime.UtcNow;
            _db.Inventories.Update(inv);

            // 4. GHI LỊCH SỬ
            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = signedQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task AdjustPickingAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            InventoryActionType actionType,
            string refCode,
            Guid lotId)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId &&
                    x.LotId == lotId);

            if (inventory == null)
                throw new Exception("INVENTORY_NOT_FOUND");

            if (actionType == InventoryActionType.AdjustDecrease)
            {
                if (inventory.AvailableQuantity < qty)
                    throw new Exception("NOT_ENOUGH_STOCK");

                inventory.OnHandQuantity -= qty;
            }
            else
            {
                inventory.OnHandQuantity += qty;
            }

            inventory.UpdatedAt = DateTime.UtcNow;
            _db.Inventories.Update(inventory);
        }

        public async Task AdjustAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            InventoryActionType actionType,
            Guid lotId)
        {
            var inventory = await GetOrCreateInventoryAsync(
                warehouseId,
                locationId,
                productId,
                lotId);

            if (actionType == InventoryActionType.AdjustDecrease)
            {
                if (inventory.AvailableQuantity < qty)
                    throw new Exception("NOT_ENOUGH_STOCK");

                inventory.OnHandQuantity -= qty;
            }
            else
            {
                inventory.OnHandQuantity += qty;
            }

            inventory.UpdatedAt = DateTime.UtcNow;
            _db.Inventories.Update(inventory);
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

        public async Task Adjust1Async(
            Guid warehouseId,
            Guid? locationId,
            int productId,
            decimal qtyChange,
            InventoryActionType actionType,
            string? refCode,
            string? note = null)
        {
            var effectiveLocationId = locationId ?? RECEIVING_LOCATION_GUID;

            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == effectiveLocationId &&
                    x.ProductId == productId);

            if (inv == null)
            {
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = effectiveLocationId,
                    ProductId = productId,
                    OnHandQuantity = 0,
                    LockedQuantity = 0,
                    InTransitQuantity = 0,
                };
                _db.Inventories.Add(inv);
            }

            if (inv.OnHandQuantity + qtyChange < 0)
                throw new Exception("Not enough stock");

            inv.OnHandQuantity += qtyChange;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = effectiveLocationId,
                ProductId = productId,
                QuantityChange = qtyChange,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        // =========================
        // LOCK STOCK
        // =========================
        public async Task LockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null)
        {
            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId);

            if (inv == null)
                throw new Exception("Inventory not found");

            if (inv.OnHandQuantity - inv.LockedQuantity < qty)
                throw new Exception("Not enough available stock");

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

            await _db.SaveChangesAsync();
        }

        // =========================
        // UNLOCK STOCK
        // =========================
        public async Task UnlockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null)
        {
            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId);

            if (inv == null)
                throw new Exception("Inventory not found");

            if (inv.LockedQuantity < qty)
                throw new Exception("Cannot unlock more than locked quantity");

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

            await _db.SaveChangesAsync();
        }
    }
}