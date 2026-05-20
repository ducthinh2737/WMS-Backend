using Wms.Application.DTOS.MasterData.ProductUoms;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface IProductUomService
{
    Task<IEnumerable<ProductUomDto>> GetProductUomsAsync(int productId);
    Task<ProductUomDto> AddProductUomAsync(CreateProductUomDto dto);
    Task UpdateProductUomAsync(int id, UpdateProductUomDto dto);
    Task DeleteProductUomAsync(int id);
    Task<decimal> ConvertToBaseQuantityAsync(int productId, int unitId, decimal quantity);
    Task<decimal> ConvertFromBaseQuantityAsync(int productId, int unitId, decimal baseQuantity);
}
