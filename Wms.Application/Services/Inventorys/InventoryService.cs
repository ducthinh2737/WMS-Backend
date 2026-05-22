using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(AppDbContext db, IWarehouseService warehouse, ILogger<InventoryService>? logger = null)
        {
            _db = db;
            warehouseService = warehouse;
            _logger = logger ?? NullLogger<InventoryService>.Instance;
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
            // First check local tracker in case it was added in the same transaction
            var inventory = _db.Inventories.Local
                .FirstOrDefault(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && x.LotId == lotId);

            if (inventory == null)
            {
                inventory = await _db.Inventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && x.LotId == lotId);
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

        public async Task<List<LocationQtyDto>> GetAvailableLocations(
            int productId, Guid warehouseId)
        {
            var pendingByLocation = await _db.GoodsIssueAllocates
                .Where(a => a.GoodsIssueItem.ProductId == productId
                         && a.LocationId != null
                         && a.Status != Wms.Domain.Entity.Outbound.GIAStatus.Picked
                         && a.Status != Wms.Domain.Entity.Outbound.GIAStatus.Cancelled)
                .GroupBy(a => a.LocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    ReservedQty = g.Sum(a => a.AllocatedQty - a.PickedQty)
                })
                .ToListAsync();

            var raw = await (from inv in _db.Inventories
                              join loc in _db.Locations on inv.LocationId equals loc.Id into locJoin
                              from loc in locJoin.DefaultIfEmpty()
                              join lot in _db.Lots on inv.LotId equals lot.Id into lotJoin
                              from lot in lotJoin.DefaultIfEmpty()
                              where inv.WarehouseId == warehouseId
                                 && inv.ProductId == productId
                                 && (inv.OnHandQuantity - inv.LockedQuantity) > 0
                              select new LocationQtyDto
                              {
                                  Id = inv.LocationId ?? Guid.Empty,
                                  WarehouseId = inv.WarehouseId,
                                  Type = loc != null ? loc.Type : default,
                                  Code = loc != null ? loc.Code : string.Empty,
                                  AvailableQty = inv.OnHandQuantity - inv.LockedQuantity,
                                  LotId = inv.LotId,
                                  LotCode = lot != null ? lot.Code : string.Empty,
                                  ExpiryDate = lot != null ? lot.ExpiryDate : null,
                                  ManufacturingDate = lot != null ? lot.ManufacturingDate : null
                              }).ToListAsync();

            foreach (var loc in raw)
            {
                var reserved = pendingByLocation
                    .FirstOrDefault(p => p.LocationId == loc.Id)?.ReservedQty ?? 0;
                loc.AvailableQty = Math.Max(0, loc.AvailableQty - reserved);
            }

            return raw.Where(x => x.AvailableQty > 0).ToList();
        }

        public async Task PutAway(PutawayDto dto)
        {
            if (dto.Qty <= 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng putaway phải > 0");
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                int retryCount = 0;
                int maxRetries = 3;
                while (retryCount < maxRetries)
                {
                    IDbContextTransaction? transaction = null;
                    try
                    {
                        transaction = await _db.Database.BeginTransactionAsync();
                    }
                    catch (InvalidOperationException)
                    {
                        // Provider does not support transactions (e.g. EF Core InMemory)
                    }

                    try
                    {
                        var fromLoc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == dto.FromLocationId);
                        var toLoc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == dto.ToLocationId);

                        if (fromLoc == null || toLoc == null) throw new BusinessException("LOCATION_NOT_FOUND", "Vị trí không tồn tại");

                        // Validate: Receiving -> Storage
                        if (fromLoc.Type != LocationType.Receiving || toLoc.Type != LocationType.Storage)
                        {
                            throw new BusinessException("INVALID_PUTAWAY_FLOW", "Putaway chỉ được phép từ Receiving sang Storage");
                        }

                        var lot = await _db.Lots.FirstOrDefaultAsync(s => s.Id == dto.LotId);
                        if (lot == null && dto.LotId == Guid.Empty)
                        {
                            lot = await _db.Lots.FirstOrDefaultAsync(x => x.productId == dto.ProductId);
                            if (lot == null)
                            {
                                lot = new Lot
                                {
                                    Id = Guid.NewGuid(),
                                    productId = dto.ProductId,
                                    Code = "NOSERIAL",
                                    CreatedAt = DateTime.UtcNow
                                };
                                _db.Lots.Add(lot);
                            }
                            dto.LotId = lot.Id;
                        }
                        else if (lot == null)
                        {
                            throw new BusinessException("LOT_NOT_FOUND", "Lô hàng không tồn tại");
                        }

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

                        await _db.SaveChangesAsync();

                        if (transaction != null)
                        {
                            await transaction.CommitAsync();
                        }
                        break; // Success
                    }
                    catch (DbUpdateConcurrencyException) when (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        if (transaction != null)
                        {
                            await transaction.RollbackAsync();
                        }
                        _db.ChangeTracker.Clear();
                    }
                    catch (DbUpdateException ex) when (retryCount < maxRetries - 1 && (ex.InnerException?.Message.Contains("Duplicate") == true || ex.InnerException?.Message.Contains("unique") == true || ex.InnerException?.Message.Contains("key") == true))
                    {
                        retryCount++;
                        if (transaction != null)
                        {
                            await transaction.RollbackAsync();
                        }
                        _db.ChangeTracker.Clear();
                    }
                    catch
                    {
                        if (transaction != null)
                        {
                            await transaction.RollbackAsync();
                        }
                        throw;
                    }
                }
            });
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
            var grouped = await _db.Inventories
                .Where(i => i.Product.Type == dto.ProductType)
                .GroupBy(i => new { i.ProductId, i.WarehouseId })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    WarehouseId = g.Key.WarehouseId,
                    OnHandQuantity = g.Sum(x => x.OnHandQuantity),
                    LockedQuantity = g.Sum(x => x.LockedQuantity),
                    InTransitQuantity = g.Sum(x => x.InTransitQuantity),
                })
                .AsNoTracking()
                .ToListAsync();

            if (!grouped.Any()) return new List<InventoryDto>();

            var productIds = grouped.Select(x => x.ProductId).Distinct().ToList();
            var warehouseIds = grouped.Select(x => x.WarehouseId).Distinct().ToList();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Name, p.Code });

            var warehouses = await _db.Warehouses
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            return grouped.Select(g => new InventoryDto
            {
                ProductId = g.ProductId,
                WarehouseId = g.WarehouseId,
                WarehouseName = warehouses.TryGetValue(g.WarehouseId, out var wName) ? wName : string.Empty,
                ProductName = products.TryGetValue(g.ProductId, out var pInfo) ? pInfo.Name : string.Empty,
                ProductCode = products.TryGetValue(g.ProductId, out var pInfo2) ? pInfo2.Code : string.Empty,
                OnHandQuantity = g.OnHandQuantity,
                LockedQuantity = g.LockedQuantity,
                InTransitQuantity = g.InTransitQuantity
            }).ToList();
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

            var result = await (from inv in query
                                join w in _db.Warehouses on inv.WarehouseId equals w.Id
                                join l in _db.Locations on inv.LocationId equals l.Id into locJoin
                                from l in locJoin.DefaultIfEmpty()
                                select new InventoryDto
                                {
                                    Id = inv.Id,
                                    WarehouseId = inv.WarehouseId,
                                    WarehouseName = w.Name,
                                    LocationId = inv.LocationId,
                                    LocationCode = l != null ? l.Code : string.Empty,
                                    LocationType = l != null ? l.Type : default,
                                    ProductId = inv.ProductId,
                                    ProductName = inv.Product.Name,
                                    ProductCode = inv.Product.Code,
                                    LotId = inv.LotId,
                                    LotCode = inv.Lot.Code,
                                    ExpiryDate = inv.Lot.ExpiryDate,
                                    OnHandQuantity = inv.OnHandQuantity,
                                    LockedQuantity = inv.LockedQuantity,
                                    InTransitQuantity = inv.InTransitQuantity,
                                }).ToListAsync();

            return result;
        }

        public async Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId)
        {
            return await (from h in _db.InventoryHistories
                          join w in _db.Warehouses on h.WarehouseId equals w.Id into wJoin
                          from w in wJoin.DefaultIfEmpty()
                          join l in _db.Locations on h.LocationId equals l.Id into lJoin
                          from l in lJoin.DefaultIfEmpty()
                          join p in _db.Products on h.ProductId equals p.Id into pJoin
                          from p in pJoin.DefaultIfEmpty()
                          where h.ProductId == productId
                          orderby h.CreatedAt descending
                          select new InventoryHistoryDto
                          {
                              Id = h.Id,
                              WarehouseId = h.WarehouseId,
                              WarehouseName = w != null ? w.Name : string.Empty,
                              LocationId = h.LocationId,
                              LocationCode = l != null ? l.Code : string.Empty,
                              ProductId = h.ProductId,
                              ProductName = p != null ? p.Name : string.Empty,
                              ProductCode = p != null ? p.Code : string.Empty,
                              LotId = h.LotId,
                              LotCode = h.LotCode,
                              QuantityChange = h.QuantityChange,
                              BeforeQty = h.BeforeQty,
                              AfterQty = h.AfterQty,
                              ActionType = h.ActionType,
                              ReferenceCode = h.ReferenceCode,
                              Note = h.Note,
                              CreatedAt = h.CreatedAt
                          }).ToListAsync();
        }

        public async Task<List<InventoryHistoryDto>> GetRecentGlobalHistoryAsync(int limit = 50)
        {
            return await (from h in _db.InventoryHistories
                          join w in _db.Warehouses on h.WarehouseId equals w.Id into wJoin
                          from w in wJoin.DefaultIfEmpty()
                          join l in _db.Locations on h.LocationId equals l.Id into lJoin
                          from l in lJoin.DefaultIfEmpty()
                          join p in _db.Products on h.ProductId equals p.Id into pJoin
                          from p in pJoin.DefaultIfEmpty()
                          orderby h.CreatedAt descending
                          select new InventoryHistoryDto
                          {
                              Id = h.Id,
                              WarehouseId = h.WarehouseId,
                              WarehouseName = w != null ? w.Name : string.Empty,
                              LocationId = h.LocationId,
                              LocationCode = l != null ? l.Code : string.Empty,
                              ProductId = h.ProductId,
                              ProductName = p != null ? p.Name : string.Empty,
                              ProductCode = p != null ? p.Code : string.Empty,
                              LotId = h.LotId,
                              LotCode = h.LotCode,
                              QuantityChange = h.QuantityChange,
                              BeforeQty = h.BeforeQty,
                              AfterQty = h.AfterQty,
                              ActionType = h.ActionType,
                              ReferenceCode = h.ReferenceCode,
                              Note = h.Note,
                              CreatedAt = h.CreatedAt
                          }).Take(limit).ToListAsync();
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
            string? note = null,
            int? unitId = null,
            decimal? originalQty = null)
        {
            if (qty == 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng điều chỉnh không được bằng 0");
            }

            if (actionType != InventoryActionType.StockTakeAdjustment && qty < 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng phải lớn hơn 0");
            }

            if (locationId == Guid.Empty)
            {
                throw new BusinessException("LOCATION_REQUIRED", "Vị trí kho không được để trống hoặc là Guid.Empty");
            }

            if (lotId.HasValue && lotId.Value == Guid.Empty)
            {
                throw new BusinessException("INVALID_LOT_ID", "LotId không được là Guid.Empty");
            }

            // Product Existence Validation
            var productExists = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!productExists)
            {
                throw new BusinessException("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm với ID {productId}");
            }

            // Transaction checking in production for relational databases
            if (_db.Database.IsRelational() && _db.Database.CurrentTransaction == null)
            {
                throw new BusinessException("TRANSACTION_REQUIRED", $"Thao tác {actionType} yêu cầu phải nằm trong một transaction.");
            }

            int resolvedUnitId = unitId ?? 0;
            if (resolvedUnitId <= 0)
            {
                var product = await _db.Products.FindAsync(productId);
                if (product != null)
                {
                    resolvedUnitId = product.UnitId;
                }
            }

            if (resolvedUnitId <= 0)
            {
                throw new BusinessException("INVALID_UNIT", "Đơn vị tính không hợp lệ hoặc không tìm thấy đơn vị mặc định cho sản phẩm.");
            }

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
            if (lotId.HasValue && lotId.Value != Guid.Empty)
            {
                finalLotId = lotId.Value;
                var lot = await _db.Lots.FindAsync(lotId.Value);
                finalLotCode = lot?.Code ?? "";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(lotCode))
                {
                    throw new BusinessException("LOT_CODE_REQUIRED", "Mã lô hàng không được để trống khi tạo mới lô");
                }
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
                finalLotCode = lotCode;
            }

            decimal signedQty = actionType switch
            {
                InventoryActionType.Receive or InventoryActionType.TransferIn or InventoryActionType.AdjustIncrease or InventoryActionType.Stage => Math.Abs(qty),
                InventoryActionType.TransferOut or InventoryActionType.AdjustDecrease or InventoryActionType.Issue or InventoryActionType.Lock or InventoryActionType.Pick => -Math.Abs(qty),
                _ => qty
            };

            // 4. Update Inventory
            var inv = await GetOrCreateInventoryAsync(warehouseId, locationId, productId, finalLotId);

            // Validation: Prevent negative inventory (OnHand - Locked)
            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < inv.LockedQuantity))
            {
                throw new BusinessException("INSUFFICIENT_STOCK", $"không đủ tồn kho (Có: {inv.AvailableQuantity}, Yêu cầu: {Math.Abs(signedQty)})");
            }

            decimal beforeQty = inv.OnHandQuantity;
            decimal afterQty = beforeQty + signedQty;

            inv.OnHandQuantity = afterQty;
            inv.UpdatedAt = DateTime.UtcNow;

            decimal finalOriginalQty = originalQty ?? qty;
            decimal signedOriginalQty = signedQty < 0 ? -Math.Abs(finalOriginalQty) : Math.Abs(finalOriginalQty);

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
                    Quantity = signedOriginalQty,
                    BaseQuantity = signedQty,
                    UnitId = resolvedUnitId,
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
                QuantityChange = signedOriginalQty,
                BaseQuantityChange = signedQty,
                UnitId = resolvedUnitId,
                BeforeQty = beforeQty,
                AfterQty = afterQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Inventory updated. Product={ProductId}, Location={LocationId}, QtyChange={QtyChange}, OnHandQuantity={OnHandQuantity}",
                productId,
                locationId,
                signedQty,
                inv.OnHandQuantity);
        }

        public async Task AdjustPickingAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, InventoryActionType actionType, string refCode, Guid lotId, int? unitId = null, decimal? originalQty = null)
        {
            if (qty <= 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng picking phải lớn hơn 0");
            }
            if (locationId == Guid.Empty)
            {
                throw new BusinessException("LOCATION_REQUIRED", "Vị trí kho không được để trống hoặc là Guid.Empty");
            }

            // Product Existence Validation
            var productExists = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!productExists)
            {
                throw new BusinessException("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm với ID {productId}");
            }

            await AdjustAsync(warehouseId, locationId, productId, qty, actionType, lotId: lotId, refCode: refCode, unitId: unitId, originalQty: originalQty);
        }

        public async Task Adjust1Async(Guid warehouseId, Guid? locationId, int productId, decimal qtyChange, InventoryActionType actionType, string? refCode, string? note = null, int? unitId = null, decimal? originalQty = null)
        {
            if (locationId == null || locationId == Guid.Empty)
            {
                throw new BusinessException("LOCATION_REQUIRED", "Vị trí kho không được để trống");
            }

            await AdjustAsync(warehouseId, locationId.Value, productId, qtyChange, actionType, refCode: refCode, note: note, unitId: unitId, originalQty: originalQty);
        }

        public async Task<List<LocationStockDto>> GetAvailableLocationsByLot(
            int productId, Guid warehouseId, Guid lotId)
        {
            var pendingByLocation = await _db.GoodsIssueAllocates
                .Where(a => a.GoodsIssueItem.ProductId == productId
                         && a.LocationId != null
                         && a.LotId == lotId
                         && a.Status != Wms.Domain.Entity.Outbound.GIAStatus.Picked
                         && a.Status != Wms.Domain.Entity.Outbound.GIAStatus.Cancelled)
                .GroupBy(a => a.LocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    ReservedQty = g.Sum(a => a.AllocatedQty - a.PickedQty)
                })
                .ToListAsync();

            var raw = await (from inv in _db.Inventories
                              join loc in _db.Locations on inv.LocationId equals loc.Id into locJoin
                              from loc in locJoin.DefaultIfEmpty()
                              where inv.ProductId == productId
                                 && inv.WarehouseId == warehouseId
                                 && inv.LotId == lotId
                                 && (inv.OnHandQuantity - inv.LockedQuantity) > 0
                              select new LocationStockDto
                              {
                                  Id = loc != null ? loc.Id : (inv.LocationId ?? Guid.Empty),
                                  LocationCode = loc != null ? loc.Code : string.Empty,
                                  Type = loc != null ? loc.Type : default,
                                  OnHandQty = inv.OnHandQuantity,
                                  LockedQty = inv.LockedQuantity,
                                  AvailableQty = inv.OnHandQuantity - inv.LockedQuantity
                              }).ToListAsync();

            foreach (var loc in raw)
            {
                var reserved = pendingByLocation
                    .FirstOrDefault(p => p.LocationId == loc.Id)?.ReservedQty ?? 0;
                loc.AvailableQty = Math.Max(0, loc.OnHandQty - loc.LockedQty - reserved);
            }

            return raw.Where(x => x.AvailableQty > 0).ToList();
        }

        // =========================
        // LOCK / UNLOCK STOCK
        // =========================
        public async Task LockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, string? note = null, Guid? lotId = null)
        {
            if (qty <= 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng khóa phải lớn hơn 0");
            }

            // Product Existence Validation
            var productExists = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!productExists)
            {
                throw new BusinessException("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm với ID {productId}");
            }

            // Transaction checking in production for relational databases
            if (_db.Database.IsRelational() && _db.Database.CurrentTransaction == null)
            {
                throw new BusinessException("TRANSACTION_REQUIRED", "LockStockAsync yêu cầu phải nằm trong một transaction.");
            }

            int resolvedUnitId = 0;
            var product = await _db.Products.FindAsync(productId);
            if (product != null)
            {
                resolvedUnitId = product.UnitId;
            }

            if (resolvedUnitId <= 0)
            {
                throw new BusinessException("INVALID_UNIT", "Đơn vị tính không hợp lệ");
            }

            var inv = _db.Inventories.Local
                .FirstOrDefault(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && (lotId == null || lotId == Guid.Empty || x.LotId == lotId));

            if (inv == null)
            {
                inv = await _db.Inventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && (lotId == null || lotId == Guid.Empty || x.LotId == lotId));
            }

            if (inv == null)
            {
                throw new BusinessException("INSUFFICIENT_STOCK", $"không đủ tồn kho (Có: {0:0.0000}, Yêu cầu: {qty})");
            }

            if (inv.AvailableQuantity < qty)
            {
                throw new BusinessException("INSUFFICIENT_STOCK", $"không đủ tồn kho (Có: {inv.AvailableQuantity:0.0000}, Yêu cầu: {qty})");
            }

            decimal beforeQty = inv.LockedQuantity;
            decimal afterQty = beforeQty + qty;

            inv.LockedQuantity = afterQty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                LotId = inv.LotId,
                QuantityChange = qty,
                BaseQuantityChange = qty,
                UnitId = resolvedUnitId,
                BeforeQty = beforeQty,
                AfterQty = afterQty,
                ActionType = InventoryActionType.Lock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Inventory locked. Product={ProductId}, Location={LocationId}, Qty={Qty}, LockedQuantity={LockedQuantity}",
                productId,
                locationId,
                qty,
                inv.LockedQuantity);
        }

        public async Task UnlockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty, string? note = null, Guid? lotId = null)
        {
            if (qty <= 0)
            {
                throw new BusinessException("INVALID_QUANTITY", "Số lượng mở khóa phải lớn hơn 0");
            }

            // Product Existence Validation
            var productExists = await _db.Products.AnyAsync(x => x.Id == productId);
            if (!productExists)
            {
                throw new BusinessException("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm với ID {productId}");
            }

            // Transaction checking in production for relational databases
            if (_db.Database.IsRelational() && _db.Database.CurrentTransaction == null)
            {
                throw new BusinessException("TRANSACTION_REQUIRED", "UnlockStockAsync yêu cầu phải nằm trong một transaction.");
            }

            int resolvedUnitId = 0;
            var product = await _db.Products.FindAsync(productId);
            if (product != null)
            {
                resolvedUnitId = product.UnitId;
            }

            if (resolvedUnitId <= 0)
            {
                throw new BusinessException("INVALID_UNIT", "Đơn vị tính không hợp lệ");
            }

            var inv = _db.Inventories.Local
                .FirstOrDefault(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && (lotId == null || lotId == Guid.Empty || x.LotId == lotId));

            if (inv == null)
            {
                inv = await _db.Inventories
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.ProductId == productId && (lotId == null || lotId == Guid.Empty || x.LotId == lotId));
            }

            if (inv == null)
            {
                throw new BusinessException("INVENTORY_NOT_FOUND", "Inventory not found");
            }

            if (inv.LockedQuantity < qty)
            {
                throw new BusinessException("INVALID_UNLOCK", "Cannot unlock more than locked quantity");
            }

            decimal beforeQty = inv.LockedQuantity;
            decimal afterQty = beforeQty - qty;

            inv.LockedQuantity = afterQty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                LotId = inv.LotId,
                QuantityChange = -qty,
                BaseQuantityChange = -qty,
                UnitId = resolvedUnitId,
                BeforeQty = beforeQty,
                AfterQty = afterQty,
                ActionType = InventoryActionType.Unlock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Inventory unlocked. Product={ProductId}, Location={LocationId}, Qty={Qty}, LockedQuantity={LockedQuantity}",
                productId,
                locationId,
                qty,
                inv.LockedQuantity);
        }
    }
}