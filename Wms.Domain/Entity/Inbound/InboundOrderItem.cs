using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Inbound;

public class InboundOrderItem
{
    public Guid Id { get; set; }
    public Guid InboundOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Received_qty { get; set; }
    public int UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
    public InboundItemStatus Status { get; set; } = InboundItemStatus.Pending;
    public decimal Price { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public InboundOrder InboundOrder { get; set; }
}

public enum InboundItemStatus
{
    Pending = 0,
    Approve = 1,
    Partially_Received = 2,
    Complete = 3,
    Rejected = 4,

}