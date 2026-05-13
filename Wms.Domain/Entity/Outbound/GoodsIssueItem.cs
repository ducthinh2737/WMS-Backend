using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Outbound
{
    public class GoodsIssueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GoodsIssueId { get; set; }
        public GoodsIssue GoodsIssue { get; set; } = null!;

        public Guid? OutboundOrderItemId { get; set; }

        public OutboundOrderItem? OutboundOrderItem { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public GIStatus Status { get; set; }
        public Guid? LocationId { get; set; }
        public Location Location { get; set; }

        public int Quantity { get; set; }
        public int Issued_Qty { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<GoodsIssueAllocate> Allocations { get; set; } = new List<GoodsIssueAllocate>();

    }
}

    