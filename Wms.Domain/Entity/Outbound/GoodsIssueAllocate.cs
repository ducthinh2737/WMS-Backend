using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Outbound
{
    public class GoodsIssueAllocate
    {
        public Guid Id { get; set; }  
        public Guid GoodsIssueItemId { get; set; }
        public Guid? LocationId { get; set; }
        public virtual Location Location { get; set; }
        public Guid LotId {  get; set; }
        public decimal AllocatedQty { get; set; }  
        public decimal PickedQty { get; set; } = 0;  
        public GIAStatus Status { get; set; } = GIAStatus.Planned;

        // Navigation
        public GoodsIssueItem GoodsIssueItem { get; set; }
    }

    public enum GIAStatus
    {
        Planned,
        Picking,
        Picked,
        Cancelled
    }
}

