using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Domain.Entity.StockTakes;

public class StockTakeItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StockTakeId { get; set; }
    public StockTake StockTake { get; set; }

    [Required]
    public Guid? LocationId { get; set; }
    public Location Location { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product Product { get; set; }

    public Guid? LotId { get; set; }

    // Số lượng trên hệ thống lúc bắt đầu kiểm kê
    public decimal SystemQty { get; set; }

    // Số lượng thực tế nhân viên đếm được
    public decimal CountedQty { get; set; }

    // Chênh lệch (Counted - System)
    public decimal Difference => CountedQty - SystemQty;

    [MaxLength(255)]
    public string? Note { get; set; } // Lý do lệch (Hàng hỏng, mất mã...)
}