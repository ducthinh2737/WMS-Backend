using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.Inventorys;

[Table("Inventories")]
public class Inventory : IVersionedEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid LotId { get; set; }
    public Lot Lot { get; set; }
    public int ProductId { get; set; }
    public Product Product {get;set;}

    // ===== Quantities =====
    public decimal OnHandQuantity { get; set; }       // tồn thực tế
    public decimal LockedQuantity { get; set; }       // đã reserve
    public decimal InTransitQuantity { get; set; }    // đang chuyển

    [NotMapped]
    public decimal AvailableQuantity
        => OnHandQuantity - LockedQuantity;

    public long Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
