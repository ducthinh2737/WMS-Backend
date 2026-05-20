using Wms.Domain.Enums.Inbound;

namespace Wms.Application.DTOS.Inbound;

public class GoodsReceiptItemDto
{
    public Guid Id {  get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Received_Qty { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public GRIStatus Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GoodsReceiptItem1Dto
{
    public Guid Id { get; set; }
    public DateTime? ExpiryDate { get; set; }           // ← nullable
    public string? LotCode { get; set; }
    public DateTime? ManufacturingDate { get; set; }    // ← nullable
    public int ProductId { get; set; }
    public decimal Received_Qty { get; set; }
    public int UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
    public GRIStatus Status { get; set; }
}