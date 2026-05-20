using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wms.Application.DTOs.MasterData.Products;

namespace Wms.Application.Interfaces.Services.MasterData;

public interface IProductService
{
    Task<int> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetAllBySupplierAsync(int dto, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetAllByType(ProductTypeDto dto, CancellationToken cancellationToken = default);

    Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ProductDto>> FilterAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
}
