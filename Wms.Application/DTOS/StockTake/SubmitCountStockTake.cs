namespace Wms.Application.DTOS.StockTake;

public class SubmitCountDto
{
    public Guid StockTakeId { get; set; }
    public List<ItemCountDto> Counts { get; set; } = new();
}

public class ItemCountDto
{
    public Guid LocationId { get; set; }
    public int ProductId { get; set; }
    public Guid? LotId { get; set; }
    public decimal CountedQty { get; set; }
    public string? Note { get; set; }
}