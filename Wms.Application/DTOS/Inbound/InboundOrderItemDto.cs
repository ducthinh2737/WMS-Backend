using Wms.Domain.Entity.Inbound;

namespace Wms.Application.DTOS.Inbound;

public class InboundOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public Guid WarehouseId {  get; set; }
    public decimal Price { get; set; }
    public InboundItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}