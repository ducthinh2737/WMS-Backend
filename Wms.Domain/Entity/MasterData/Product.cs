using Wms.Domain.Entity.MasterData;

namespace Wms.Domain.Entity.MasterData;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = null!; // SKU
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public ProductType Type { get; set; }

    // Foreign Keys
    public int CategoryId { get; set; }
    public int UnitId { get; set; }
    public int BrandId { get; set; }
    public int SupplierId { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;

    public ICollection<ProductUom> ProductUoms { get; set; } = new List<ProductUom>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
public enum ProductType
{
    Material = 0,
    Production = 1,
}
