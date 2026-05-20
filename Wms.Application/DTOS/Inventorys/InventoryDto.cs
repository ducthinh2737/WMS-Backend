using System;
using System.Text.Json.Serialization;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;

namespace Wms.Application.DTOs.Inventorys
{
    public class InventoryDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } // Thêm mới
        public Guid? LocationId { get; set; }
        public Guid LotId { get; set; }
        public string LotCode { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string LocationCode { get; set; }  // Thêm mới
        public int ProductId { get; set; }
        public string ProductName { get; set; }   // Thêm mới
        public string ProductCode { get; set; }   // Thêm mới (SKU)
        public decimal OnHandQuantity { get; set; }
        public decimal LockedQuantity { get; set; }
        public decimal AvailableQuantity => OnHandQuantity - LockedQuantity;
        public decimal InTransitQuantity { get; set; }
        public LocationType? LocationType { get; set; }
    }

    public class ProductType1Dto
    {
        [JsonPropertyName("productType")]
        public ProductType ProductType { get; set; }
    }
    public class GetAvailableLocationsRequest
    {
        public int ProductId { get; set; }
        public Guid WarehouseId { get; set; }
    }
    public class PutawayDto
    {
        public int ProductId { get; set; }      // Sản phẩm cần putaway
        public Guid LotId { get; set; }

        public Guid FromLocationId { get; set; } // Thường là Receiving location
        public Guid ToLocationId { get; set; }   // Storage location
        public Guid WarehouseId { get; set; }
        public decimal Qty { get; set; }         // Số lượng putaway
    }
    public class LocationStockDto
    {
        public Guid Id { get; set; }
        public string LocationCode { get; set; }
        public LocationType Type { get; set; }

        public decimal OnHandQty { get; set; }
        public decimal LockedQty { get; set; }
        public decimal AvailableQty { get; set; }
    }


    public class InventoryHistoryDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public Guid LotId { get; set; }
        public string? LotCode { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        public InventoryActionType ActionType { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InventoryAdjustRequest
    {
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal QtyChange { get; set; }
        public InventoryActionType ActionType { get; set; } // enum, không string
        public string? RefCode { get; set; }
        public string? Note { get; set; }
    }

    public class InventoryLockRequest
    {
        public Guid WarehouseId { get; set; }
        public Guid LocationId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public bool Lock { get; set; } = true; // true: lock, false: release
    }

    public class InventoryQueryDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid? LocationId { get; set; }
        public string? LotCode { get; set; } // THÊM DÒNG NÀY
        public int? ProductId { get; set; }
        public List<int>? ProductIds { get; set; } // mở rộng query nhiều sản phẩm
    }

}
