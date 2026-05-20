namespace Wms.Application.DTOS.MasterData.ProductUoms;

public class ProductUomDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public bool IsBaseUnit { get; set; }
}

public class CreateProductUomDto
{
    public int ProductId { get; set; }
    public int UnitId { get; set; }
    public decimal Factor { get; set; }
    public bool IsBaseUnit { get; set; }
}

public class UpdateProductUomDto
{
    public int UnitId { get; set; }
    public decimal Factor { get; set; }
    public bool IsBaseUnit { get; set; }
}
