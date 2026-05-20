using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.Outbound
{
    public class OutboundOrderItem : IVersionedEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OutboundOrderId { get; set; }
        public OutboundOrder OutboundOrder { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public OutboundStatus Status { get; set; } = OutboundStatus.Pending;

        public decimal Quantity { get; set; }
        public decimal Issued_Qty { get; set; }
        public int UnitId { get; set; }
        public decimal BaseQuantity { get; set; }
        public Guid WarehouseId {  get; set; }
        public Warehouse warehouse { get; set; } 
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long Version { get; set; } = 1;
    }
}

