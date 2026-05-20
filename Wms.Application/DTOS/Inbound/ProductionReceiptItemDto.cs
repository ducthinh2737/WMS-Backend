using Wms.Domain.Enums.Inbound;

namespace Wms.Application.DTOS.Inbound
{
    public class ProductionReceiptItemDto
    {
        public Guid Id { get; set; }
        public Guid GoodsReceiptId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Receipt_Qty { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal BaseQuantity { get; set; }
        public string? LotCode { get; set; }
        public DateTime? ExpiryDate { get; set; }           // ← nullable
        public DateTime? ManufacturingDate { get; set; }    // ← thêm + nullable
        public GRIStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

