using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Outbound
{
    public class OutboundOrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OutboundOrderId { get; set; }
        public OutboundOrder OutboundOrder { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public OutboundStatus Status { get; set; } = OutboundStatus.Pending;

        public int Quantity { get; set; }
        public int Issued_Qty { get; set; }
        public Guid WarehouseId {  get; set; }
        public Warehouse warehouse { get; set; } 
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

