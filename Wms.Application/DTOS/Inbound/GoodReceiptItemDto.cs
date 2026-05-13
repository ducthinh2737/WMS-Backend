using Wms.Domain.Enums.Inbound;

namespace Wms.Application.DTOS.Inbound;

public class GoodsReceiptItemDto
{
    public Guid Id {  get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Received_Qty { get; set; }
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
    public int Received_Qty { get; set; }
    public GRIStatus Status { get; set; }
}