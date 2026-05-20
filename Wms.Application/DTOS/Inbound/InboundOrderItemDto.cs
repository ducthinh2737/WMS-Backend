using Wms.Domain.Entity.Inbound;

namespace Wms.Application.DTOS.Inbound;

public class InboundOrderItemDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public Guid WarehouseId {  get; set; }
    public decimal Price { get; set; }
    public InboundItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}