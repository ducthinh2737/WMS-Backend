using Wms.Domain.Enums.Inbound;

namespace Wms.Domain.Entity.Inbound;

public class GoodsReceiptItem
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public Guid InboundOrderItemId { get; set; }
    public decimal Received_Qty { get; set; }
    public int UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
    public GRIStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; }
}