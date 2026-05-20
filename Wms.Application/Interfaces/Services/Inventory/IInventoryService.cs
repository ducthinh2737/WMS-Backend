using Wms.Application.DTOs.Inventorys;
using Wms.Application.DTOS.Warehouse;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;

namespace Wms.Application.Interfaces.Services.Inventory
{
    public interface IInventoryService
    {
        // =========================
        // GET INVENTORY
        // =========================
        public Task<List<InventoryDto>> GetByProductAsync(int productId)
    => QueryAsync(new InventoryQueryDto { ProductId = productId });

        public Task<List<InventoryDto>> GetByWarehouseAsync(Guid warehouseId)
            => QueryAsync(new InventoryQueryDto { WarehouseId = warehouseId });
        Task<List<LocationStockDto>> GetAvailableLocationsByLot(
    int productId,
    Guid warehouseId,
    Guid lotId);
 Task AdjustPickingAsync(
    Guid warehouseId,
    Guid locationId,
    int productId,
    decimal qty,
    InventoryActionType actionType,
    string refCode,
    Guid lotId,
    int? unitId = null,
    decimal? originalQty = null);
        Task<List<InventoryDto>> GetInventoryByProductType(ProductType1Dto dto);
        public Task<List<InventoryDto>> GetByLocationAsync(Guid locationId)
            => QueryAsync(new InventoryQueryDto { LocationId = locationId });

        Task<InventoryDto?> GetAsync(Guid id);
        Task PutAway(PutawayDto dto);
        Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto);
        Task<List<LocationQtyDto>> GetAvailableLocations(int productId, Guid WarehouseId);

        // =========================
        // INVENTORY HISTORY
        // =========================
        Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId);
        Task<List<InventoryHistoryDto>> GetRecentGlobalHistoryAsync(int limit = 50);

        // =========================
        // ADJUST INVENTORY
        // =========================
        Task Adjust1Async(
            Guid warehouseId,
            Guid? locationId,
            int productId,
            decimal qtyChange,
            InventoryActionType actionType,
            string? refCode = null,
            string? note = null,
            int? unitId = null,
            decimal? originalQty = null
        );
        Task AdjustAsync(
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
            decimal? originalQty = null);

        // =========================
        // LOCK / UNLOCK STOCK
        // =========================
        Task LockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null,
            Guid? lotId = null
        );

        Task UnlockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null,
            Guid? lotId = null
        );
    }
}
