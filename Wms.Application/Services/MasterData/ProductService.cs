using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.DTOs.MasterData.Products;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wms.Application.Services.MasterData;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService>? logger = null)
    {
        _db = db;
        _logger = logger ?? NullLogger<ProductService>.Instance;
    }

    public async Task<int> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (await _db.Products.AnyAsync(x => x.Code == dto.Code, cancellationToken))
            throw new Exception("Code already exists");

        var product = new Product
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Type = dto.Type,
            UnitId = dto.UnitId,
            BrandId = dto.BrandId,
            SupplierId = dto.SupplierId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);

        // Automatically create a base ProductUom for the new product
        var productUom = new ProductUom
        {
            Product = product, // Use navigation property instead of ProductId since product isn't saved yet
            UnitId = dto.UnitId,
            Factor = 1,
            IsBaseUnit = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ProductUoms.Add(productUom);

        await _db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}. Payload: {@Payload}", id, dto);

        // Load existing entity with tracking
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new Exception("Product not found");

        // Manually map only editable fields
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.CategoryId = dto.CategoryId;
        product.UnitId = dto.UnitId;
        product.BrandId = dto.BrandId;
        product.SupplierId = dto.SupplierId;

        // Auditing update timestamp
        product.UpdatedAt = DateTime.UtcNow;

        // Prevent IsActive from being reset accidentally
        if (dto.IsActive.HasValue)
        {
            product.IsActive = dto.IsActive.Value;
        }
        else
        {
            _logger.LogWarning("IsActive was omitted in update payload for product ID: {ProductId}. Keeping existing state: {IsActive}", id, product.IsActive);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product with ID: {ProductId} successfully saved. Saved Entity: {@Product}", id, product);

        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Type = product.Type,
            Description = product.Description,
            CategoryId = product.CategoryId,
            UnitId = product.UnitId,
            BrandId = product.BrandId,
            SupplierId = product.SupplierId,
            IsActive = product.IsActive,
            CreateAt = product.CreatedAt
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new Exception("Product not found");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        // Use AsNoTracking carefully for read-only retrieval to prevent tracking conflicts
        var p = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new Exception("Product not found");

        return new ProductDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Description = p.Description,
            CategoryId = p.CategoryId,
            UnitId = p.UnitId,
            BrandId = p.BrandId,
            SupplierId = p.SupplierId,
            IsActive = p.IsActive,
            CreateAt = p.CreatedAt
        };
    }

    public async Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Type = p.Type,
                UnitId = p.UnitId,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive,
                CreateAt = p.CreatedAt
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<ProductDto>> GetAllBySupplierAsync(int dto, CancellationToken cancellationToken = default)
    {
        var prodList = await _db.Products
            .AsNoTracking()
            .Where(p => p.SupplierId == dto)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                UnitId = p.UnitId,
                Type = p.Type,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive,
                CreateAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (prodList.Count == 0)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm của nhà cung cấp ID: {dto}");
        }

        return prodList;
    }

    public async Task<List<ProductDto>> GetAllByType(ProductTypeDto dto, CancellationToken cancellationToken = default)
    {
        var prodList = await _db.Products
            .AsNoTracking()
            .Where(p => p.Type == dto.Type)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                UnitId = p.UnitId,
                Type = p.Type,
                BrandId = p.BrandId,
                SupplierId = p.SupplierId,
                IsActive = p.IsActive,
                CreateAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (prodList.Count == 0)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm thuộc loại: {dto.Type}");
        }

        return prodList;
    }

    public async Task<List<ProductDto>> FilterAsync(ProductFilterDto f, CancellationToken cancellationToken = default)
    {
        var q = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var kw = f.Keyword.ToLower();
            q = q.Where(x =>
                x.Name.ToLower().Contains(kw) ||
                x.Code.ToLower().Contains(kw));
        }

        if (f.CategoryId.HasValue)
            q = q.Where(x => x.CategoryId == f.CategoryId);

        if (f.BrandId.HasValue)
            q = q.Where(x => x.BrandId == f.BrandId);

        if (f.SupplierId.HasValue)
            q = q.Where(x => x.SupplierId == f.SupplierId);

        return await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                CategoryId = x.CategoryId,
                UnitId = x.UnitId,
                BrandId = x.BrandId,
                Type = x.Type,
                SupplierId = x.SupplierId,
                IsActive = x.IsActive,
                CreateAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
