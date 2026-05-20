using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wms.Domain.Entity.MasterData;

[Table("ProductUoms")]
public class ProductUom
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal Factor { get; set; }

    public bool IsBaseUnit { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
