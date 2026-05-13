using Wms.Domain.Enums.Inbound;

namespace Wms.Domain.Entity.Inbound;

public class GoodsReceiptItem
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid InboundOrderItemId { get; set; }
    public int Received_Qty { get; set; }
    public GRIStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; }
}