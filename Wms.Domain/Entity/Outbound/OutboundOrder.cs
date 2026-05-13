using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Outbound
{
    public class OutboundOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = null!;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int CreatedBy { get; set; }
        public OutboundStatus Status { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? ApproveBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<OutboundOrderItem> Items { get; set; } = new List<OutboundOrderItem>();
        public ICollection<GoodsIssue> GoodsIssues { get; set; } = new List<GoodsIssue>();
    }

    public enum OutboundStatus
    {
        Pending = 0,
        Approve = 1,
        Partically_Issued = 2,
        Complete = 3,
        Rejected = 4,
    }
}

