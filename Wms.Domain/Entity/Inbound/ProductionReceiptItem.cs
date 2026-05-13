using Wms.Domain.Enums.Inbound;

namespace Wms.Domain.Entity.Inbound
{
    public class ProductionReceiptItem
    {
        public Guid Id {  get; set; }
        public Guid GoodsReceiptId { get; set; }
        public int ProductId {  get; set; }
        public int Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public int Receipt_Qty { get; set; }
        public GRIStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set;}
        public GoodsReceipt GoodsReceipt { get; set; }

    }
}

