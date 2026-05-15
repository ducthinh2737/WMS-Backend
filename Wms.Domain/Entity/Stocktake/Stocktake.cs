using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.StockTakes;

namespace Wms.Domain.Entity.StockTakes;

public class StockTake
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } // ST-20251222-001

    [Required]
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }

    public StockTakeStatus Status { get; set; } = StockTakeStatus.Draft;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedBy { get; set; }

    // Navigation
    public ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();
}