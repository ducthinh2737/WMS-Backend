using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Outbound
{
    public class GoodsIssue : IVersionedEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? OutboundOrderId { get; set; }
        public OutboundOrder? OutboundOrder { get; set; } = null!;
        public GIType Type { get;set; }
        public string Code { get; set; } = null!;
        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!; 

        public int? CustomerId { get; set; }
        public Wms.Domain.Entity.MasterData.Customer? Customer { get; set; }
        public string? Address { get; set; }

        public GIStatus Status { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow; // EF sẽ tự điền giá trị
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public long Version { get; set; } = 1;

        public ICollection<GoodsIssueItem> Items { get; set; } = new List<GoodsIssueItem>();
    }
    public enum GIStatus
    {
        Pending = 0,
        Approve = 1,
        Partically_Issued = 2,
        Complete = 3,
        Rejected = 4,
        Picking = 5,

        OutOfStock = 6,        // Hết hàng
        InsufficientStock = 7, // Không đủ hàng
        Picked = 8
    }
    public enum GIType
    {
        Outbound,
        Production
    }
}

