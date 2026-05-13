using Wms.Domain.Entity.Inbound;

namespace Wms.Application.DTOS.Inbound;

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? InboundOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public ReceiptType ReceiptType { get; set; }
    public InboundStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
    public List<ProductionReceiptItemDto> ProductionReceiptItems { get; set; } = new();

}

public class GRByTypeDto
{
    public ReceiptType ReceiptType { get; set; }
}