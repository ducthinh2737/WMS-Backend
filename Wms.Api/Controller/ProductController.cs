using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.MasterData.Products;
using Wms.Application.Interfaces.Services.MasterData;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IProductUomService _productUomService;

    public ProductController(IProductService service, IProductUomService productUomService)
    {
        _service = service;
        _productUomService = productUomService;
    }

    // CREATE
    [HttpPost]
    [HasPermission("product.create")]
    public async Task<IActionResult> Create(CreateProductDto dto, CancellationToken cancellationToken)
        => Ok(await _service.CreateAsync(dto, cancellationToken));

    // UPDATE
    [HttpPut("{id}")]
    [HasPermission("product.update")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var updatedProduct = await _service.UpdateAsync(id, dto, cancellationToken);
        return Ok(updatedProduct);
    }

    // DELETE
    [HttpDelete("{id}")]
    [HasPermission("product.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok();
    }

    // GET BY ID
    [HttpGet("{id}")]
    [HasPermission("product.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    // GET ALL
    [HttpGet]
    [HasPermission("product.view")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HasPermission("product.view")]
    [HttpPost("by-type")]
    public async Task<IActionResult> GetByType([FromBody] ProductTypeDto dto, CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest("DTO không được null");

        try
        {
            var products = await _service.GetAllByType(dto, cancellationToken);
            return Ok(products); 
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpGet("By-Supplier/{supplierId}")]
    [HasPermission("product.view")]
    public async Task<IActionResult> GetAllBySup(int supplierId, CancellationToken cancellationToken)
        => Ok(await _service.GetAllBySupplierAsync(supplierId, cancellationToken));

    // FILTER
    [HttpPost("filter")]
    [HasPermission("product.view")]
    public async Task<IActionResult> Filter(ProductFilterDto dto, CancellationToken cancellationToken)
        => Ok(await _service.FilterAsync(dto, cancellationToken));

    // GET UOMS
    [HttpGet("{id}/uoms")]
    public async Task<IActionResult> GetProductUoms(int id)
    {
        var uoms = await _productUomService.GetProductUomsAsync(id);
        return Ok(uoms);
    }

    [HttpPost("{id}/uoms")]
    public async Task<IActionResult> AddProductUom(int id, [FromBody] Wms.Application.DTOS.MasterData.ProductUoms.CreateProductUomDto dto)
    {
        dto.ProductId = id;
        var uom = await _productUomService.AddProductUomAsync(dto);
        return Ok(uom);
    }

    [HttpDelete("{id}/uoms/{uomId}")]
    public async Task<IActionResult> DeleteProductUom(int id, int uomId)
    {
        await _productUomService.DeleteProductUomAsync(uomId);
        return Ok();
    }
}
