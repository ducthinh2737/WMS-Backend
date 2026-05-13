namespace Wms.Application.DTOS.Inbound;

public class InboundOrderDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; }          // Mã PO
    public int SupplierId { get; set; }
    public string? Status { get; set; }        // Pending, Approved, Rejected
    public DateTime? CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; } // Nullable vì chưa approve
    public List<InboundOrderItemDto> Items { get; set; } = new();
}

public class ScanQRPayloadDto
{
    /// <summary>
    /// Nhà cung cấp
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Danh sách sản phẩm trong đơn hàng
    /// </summary>
    public List<ScanQRItemDto> Items { get; set; } = new();
}

public class ScanQRItemDto
{
    public int ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class ScanReceiveResultDto
{
    public InboundOrderDto InboundOrder { get; set; } = null!;
    public List<GoodsReceiptDto> GoodsReceipts { get; set; } = new();

    /// <summary>true = Đơn hàng còn Pending, cần user confirm để approve</summary>
    public bool NeedsApproval { get; set; }
}