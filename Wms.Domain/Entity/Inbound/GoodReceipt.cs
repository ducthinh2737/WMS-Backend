namespace Wms.Domain.Entity.Inbound;

public class GoodsReceipt
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? InboundOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public ReceiptType ReceiptType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public InboundStatus Status { get; set; } = InboundStatus.Pending;
    public List<GoodsReceiptItem> Items { get; set; } = new();
    public List<ProductionReceiptItem> Productions { get; set; } = new();
    public InboundOrder? InboundOrder { get; set; }
}

public enum ReceiptType
{
    Inbound,
    Production
}

public enum InboundStatus
{
    Pending = 0,
    Approve = 1,
    Partially_Received = 2,
    Complete = 3,
    Rejected = 4,

}