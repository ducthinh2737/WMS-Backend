namespace Wms.Application.DTOS.StockTake;

public class StockTakeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<StockTakeItemDto> Items { get; set; } = new();
}

public class StockTakeItemDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationCode { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SKU { get; set; }
    public Guid? LotId { get; set; }
    public string? LotCode { get; set; }

    public decimal SystemQty { get; set; } // Số lượng sổ sách
    public decimal CountedQty { get; set; } // Số lượng thực tế
    public decimal Difference { get; set; } // Chênh lệch (Counted - System)
    public string? Note { get; set; }
}