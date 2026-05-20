using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.MasterData.ProductUoms;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.MasterData;

public class ProductUomService : IProductUomService
{
    private readonly AppDbContext _context;

    public ProductUomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductUomDto>> GetProductUomsAsync(int productId)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) throw new NotFoundException($"Product with ID {productId} not found");

        var uoms = await _context.ProductUoms
            .Include(u => u.Unit)
            .Where(u => u.ProductId == productId)
            .ToListAsync();

        return uoms.Select(u => new ProductUomDto
        {
            Id = u.Id,
            ProductId = u.ProductId,
            UnitId = u.UnitId,
            UnitName = u.Unit.Name,
            Factor = u.Factor,
            IsBaseUnit = u.IsBaseUnit
        });
    }

    public async Task<ProductUomDto> AddProductUomAsync(CreateProductUomDto dto)
    {
        if (dto.Factor <= 0) throw new BusinessRuleException("Factor must be greater than 0");

        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists) throw new NotFoundException($"Product with ID {dto.ProductId} not found");

        var unitExists = await _context.Units.AnyAsync(u => u.Id == dto.UnitId);
        if (!unitExists) throw new NotFoundException($"Unit with ID {dto.UnitId} not found");

        var duplicateExists = await _context.ProductUoms.AnyAsync(u => u.ProductId == dto.ProductId && u.UnitId == dto.UnitId);
        if (duplicateExists) throw new BusinessRuleException("This Unit is already assigned to the Product");

        // Prevent multiple base units
        if (dto.IsBaseUnit)
        {
            var baseExists = await _context.ProductUoms.AnyAsync(u => u.ProductId == dto.ProductId && u.IsBaseUnit);
            if (baseExists) throw new BusinessRuleException("Product already has a base unit");
            
            if (dto.Factor != 1) throw new BusinessRuleException("Base unit must have Factor = 1");
        }

        var productUom = new ProductUom
        {
            ProductId = dto.ProductId,
            UnitId = dto.UnitId,
            Factor = dto.Factor,
            IsBaseUnit = dto.IsBaseUnit,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductUoms.Add(productUom);
        await _context.SaveChangesAsync();

        var unit = await _context.Units.FindAsync(dto.UnitId);
        return new ProductUomDto
        {
            Id = productUom.Id,
            ProductId = productUom.ProductId,
            UnitId = productUom.UnitId,
            UnitName = unit!.Name,
            Factor = productUom.Factor,
            IsBaseUnit = productUom.IsBaseUnit
        };
    }

    public async Task UpdateProductUomAsync(int id, UpdateProductUomDto dto)
    {
        if (dto.Factor <= 0) throw new BusinessRuleException("Factor must be greater than 0");

        var productUom = await _context.ProductUoms.FindAsync(id);
        if (productUom == null) throw new NotFoundException($"ProductUom with ID {id} not found");

        var unitExists = await _context.Units.AnyAsync(u => u.Id == dto.UnitId);
        if (!unitExists) throw new NotFoundException($"Unit with ID {dto.UnitId} not found");

        if (productUom.UnitId != dto.UnitId)
        {
            var duplicateExists = await _context.ProductUoms.AnyAsync(u => u.ProductId == productUom.ProductId && u.UnitId == dto.UnitId && u.Id != id);
            if (duplicateExists) throw new BusinessRuleException("This Unit is already assigned to the Product");
            
            // Check if transactions exist
            if (productUom.IsBaseUnit)
            {
                var transactionsExist = await _context.InventoryHistories.AnyAsync(h => h.ProductId == productUom.ProductId);
                if (transactionsExist) throw new BusinessRuleException("Cannot change the Base Unit because inventory transactions already exist for this product.");
            }
        }

        if (dto.IsBaseUnit && !productUom.IsBaseUnit)
        {
            var baseExists = await _context.ProductUoms.AnyAsync(u => u.ProductId == productUom.ProductId && u.IsBaseUnit && u.Id != id);
            if (baseExists) throw new BusinessRuleException("Product already has a base unit");
        }
        
        if (dto.IsBaseUnit && dto.Factor != 1) throw new BusinessRuleException("Base unit must have Factor = 1");

        // Check if transactions exist and IsBaseUnit is being changed
        if (productUom.IsBaseUnit != dto.IsBaseUnit)
        {
            var transactionsExist = await _context.InventoryHistories.AnyAsync(h => h.ProductId == productUom.ProductId);
            if (transactionsExist) throw new BusinessRuleException("Cannot change the Base Unit status because inventory transactions already exist for this product.");
        }

        productUom.UnitId = dto.UnitId;
        productUom.Factor = dto.Factor;
        productUom.IsBaseUnit = dto.IsBaseUnit;
        productUom.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteProductUomAsync(int id)
    {
        var productUom = await _context.ProductUoms.FindAsync(id);
        if (productUom == null) throw new NotFoundException($"ProductUom with ID {id} not found");

        if (productUom.IsBaseUnit) throw new BusinessRuleException("Cannot delete the base unit of a product");

        // Double check just in case, but base unit check above covers it mostly. For non-base units, can we delete them if they have transactions?
        // Actually, if we delete a non-base unit but transactions used that UnitId, it could cause issues displaying the original unit.
        // So let's prevent deletion if it's been used in any transaction.
        var hasTransactionsWithUnit = await _context.InventoryTransactions.AnyAsync(t => t.ProductId == productUom.ProductId && t.UnitId == productUom.UnitId);
        if (hasTransactionsWithUnit) throw new BusinessRuleException("Cannot delete this UOM because it has already been used in inventory transactions.");

        _context.ProductUoms.Remove(productUom);
        await _context.SaveChangesAsync();
    }

    public async Task<decimal> ConvertToBaseQuantityAsync(int productId, int unitId, decimal quantity)
    {
        if (unitId <= 0)
        {
            var prod = await _context.Products.FindAsync(productId);
            if (prod != null)
            {
                unitId = prod.UnitId;
            }
            else
            {
                throw new BusinessRuleException($"UnitId must be greater than 0. Invalid UnitId {unitId} for Product ID {productId}");
            }
        }

        var uom = await _context.ProductUoms
            .FirstOrDefaultAsync(u => u.ProductId == productId && u.UnitId == unitId);
        
        if (uom == null)
        {
            var prod = await _context.Products.FindAsync(productId);
            if (prod != null && prod.UnitId == unitId)
            {
                if (quantity <= 0) throw new BusinessRuleException($"Quantity must be greater than 0. Invalid Quantity: {quantity}");
                return quantity;
            }
            throw new BusinessRuleException($"UOM setup for Product {productId} and Unit {unitId} not found");
        }

        var baseQty = quantity * uom.Factor;
        if (baseQty <= 0 && quantity > 0)
        {
            throw new BusinessRuleException($"Calculated BaseQuantity must be greater than 0. Quantity: {quantity}, Factor: {uom.Factor}");
        }
        return baseQty;
    }

    public async Task<decimal> ConvertFromBaseQuantityAsync(int productId, int unitId, decimal baseQuantity)
    {
        if (unitId <= 0)
        {
            throw new BusinessRuleException($"UnitId must be greater than 0. Invalid UnitId {unitId} for Product ID {productId}");
        }

        var uom = await _context.ProductUoms
            .FirstOrDefaultAsync(u => u.ProductId == productId && u.UnitId == unitId);
        
        if (uom == null)
        {
            var prod = await _context.Products.FindAsync(productId);
            if (prod != null && prod.UnitId == unitId)
            {
                if (baseQuantity <= 0) throw new BusinessRuleException($"BaseQuantity must be greater than 0. Invalid BaseQuantity: {baseQuantity}");
                return baseQuantity;
            }
            throw new BusinessRuleException($"UOM setup for Product {productId} and Unit {unitId} not found");
        }

        var qty = baseQuantity / uom.Factor;
        if (qty <= 0 && baseQuantity > 0)
        {
            throw new BusinessRuleException($"Calculated Quantity must be greater than 0. BaseQuantity: {baseQuantity}, Factor: {uom.Factor}");
        }
        return qty;
    }
}
