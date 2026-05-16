using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Services.Inventorys;
using Wms.Domain.Enums.Inventory;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    private readonly Wms.Infrastructure.Persistence.Context.AppDbContext _db;

    public InventoryController(IInventoryService service, Wms.Infrastructure.Persistence.Context.AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // =========================
    // GET INVENTORY BY ID
    // =========================
    [HttpPost("by-product-type")]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> GetByProductType([FromBody] ProductType1Dto dto)
    {
        if (dto == null)
            return BadRequest();

        var result = await _service.GetInventoryByProductType(dto);
        return Ok(result);
    }
    [HttpGet("{id:guid}")]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Get(Guid id)
    {
        var inv = await _service.GetAsync(id);
        if (inv == null) return NotFound();
        return Ok(inv);
    }
    [HttpPost("putaway")]
    [HasPermission("inventory.putaway")]
    public async Task<IActionResult> Putaway([FromBody] PutawayDto dto)
    {
        if (dto == null || dto.Qty <= 0)
            return BadRequest("Invalid input");

        try
        {
            await _service.PutAway(dto);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Putaway completed successfully" });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message;
            throw new Exception(inner ?? ex.Message);
        }

    }
    [HasPermission("inventory.view")]
    [HttpGet("available-locations")] // ⬅️ Đổi thành GET
    public async Task<ActionResult<List<LocationQtyDto>>> GetAvailableLocations(
    [FromQuery] int productId,      // ⬅️ FromQuery
    [FromQuery] Guid warehouseId)   // ⬅️ FromQuery
    {
        try
        {
            // Validate
            if (productId <= 0)
            {
                return BadRequest(new { message = "ProductId phải lớn hơn 0" });
            }

            if (warehouseId == Guid.Empty)
            {
                return BadRequest(new { message = "WarehouseId không hợp lệ" });
            }

            var result = await _service.GetAvailableLocations(productId, warehouseId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    // =========================
    // QUERY INVENTORY
    // =========================
    [HttpGet]
    [HasPermission("inventory.view")]
    public async Task<IActionResult> Query(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? locationId,
        [FromQuery] int? productId,
        [FromQuery] List<int>? productIds)
    {
        var dto = new InventoryQueryDto
        {
            WarehouseId = warehouseId,
            LocationId = locationId,
            ProductId = productId,
            ProductIds = productIds
        };
        var list = await _service.QueryAsync(dto);
        return Ok(list);
    }

    // =========================
    // INVENTORY HISTORY
    // =========================
    [HttpGet("product/{productId}/history")]
    [HasPermission("inventory.history")]
    public async Task<IActionResult> History(int productId)
    {
        var list = await _service.GetHistoryAsync(productId);
        return Ok(list);
    }

    // =========================
    // ADJUST INVENTORY (Receive / Issue / Transfer / Adjust)
    // =========================
    [HttpPost("adjust")]
    [HasPermission("inventory.adjust")]
    public async Task<IActionResult> Adjust([FromBody] InventoryAdjustRequest req)
    {
        await _service.Adjust1Async(
            req.WarehouseId,
            req.LocationId,
            req.ProductId,
            req.QtyChange,
            req.ActionType,
            req.RefCode,
            req.Note
        );
        await _db.SaveChangesAsync();
        return Ok();
    }

    // =========================
    // LOCK OR UNLOCK STOCK
    // =========================
    [HttpPost("lock-toggle")]
    [HasPermission("inventory.lock")]
    public async Task<IActionResult> LockToggle([FromBody] InventoryLockRequest req)
    {
        if (req.Lock)
            await _service.LockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity, note: null);
        else
            await _service.UnlockStockAsync(req.WarehouseId, req.LocationId, req.ProductId, req.Quantity, note: null);

        await _db.SaveChangesAsync();
        return Ok();
    }

    // =========================
    // HELPER ENDPOINTS (OPTIONAL)
    // =========================
    [HttpGet("warehouse/{warehouseId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByWarehouse(Guid warehouseId)
        => GetQueryResult(_service.GetByWarehouseAsync(warehouseId));

    [HttpGet("location/{locationId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByLocation(Guid locationId)
        => GetQueryResult(_service.GetByLocationAsync(locationId));

    [HttpGet("product/{productId}")]
    [HasPermission("inventory.view")]
    public Task<IActionResult> GetByProduct(int productId)
        => GetQueryResult(_service.GetByProductAsync(productId));

    // Helper private method
    private async Task<IActionResult> GetQueryResult(Task<List<InventoryDto>> task)
    {
        var list = await task;
        return Ok(list);
    }
}
