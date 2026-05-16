using Wms.Domain.Enums.Inventory;
namespace Wms.Domain.Entity.Inventorys;

public class InventoryHistory
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public int ProductId { get; set; }
    public Guid LotId { get; set; }
    public string? LotCode { get; set; }
    public decimal QuantityChange { get; set; }
    public string Note { get; set; }
    public InventoryActionType ActionType { get; set; } // Nhập/xuất/chuyển/kiểm kê
    public string? ReferenceCode { get; set; }          // PO/SO/Transfer code
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}