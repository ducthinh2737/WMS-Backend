using Wms.Domain.Entity.Auth;
using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Inbound;

public class InboundOrder 
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreateBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public List<GoodsReceipt> GoodsReceipts { get; set; } = new();

    public List<InboundOrderItem> Items { get; set; } = new();
}