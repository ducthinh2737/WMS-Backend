using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOS.Inbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services.Inbound;
using Wms.Domain.Entity.Inbound;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/inbound")]
public class InboundController : ControllerBase
{
    private readonly IInboundService _inboundService;
    private readonly AppDbContext _db;

    public InboundController(IInboundService inboundService, AppDbContext dbContext)
    {
        _inboundService = inboundService;
        _db = dbContext;
    }

    // ========================
    // INBOUND ORDER
    // ========================
    [HttpGet("scan/{orderCode}")]
    public async Task<IActionResult> ScanInboundOrderInfo(string orderCode)
    {
        try
        {
            var result = await _inboundService.ScanInboundOrderInfoAsync(orderCode);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("scan-and-process")]
    public async Task<IActionResult> ScanAndProcess([FromBody] ScanQRPayloadDto payload)
    {
        try
        {
            var result = await _inboundService.ScanAndProcessAsync(payload);
            return Ok(result);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("scan/{orderCode}/confirm")]
    public async Task<IActionResult> ConfirmScanReceive(string orderCode)
    {
        try
        {
            var result = await _inboundService.ConfirmAndReceiveAsync(orderCode);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("order")]
    [HasPermission("inbound.order.create")]
    public async Task<ActionResult<InboundOrderDto>> CreateInboundOrder([FromBody] InboundOrderDto dto)
    {
        var order = await _inboundService.CreateInboundOrderAsync(dto);
        return CreatedAtAction(nameof(GetInboundOrderById), new { orderId = order.Id }, order);
    }

    [HttpGet("order/{orderId}")]
    [HasPermission("inbound.order.view")]
    public async Task<ActionResult<InboundOrderDto>> GetInboundOrderById(Guid orderId)
    {
        var order = await _inboundService.GetInboundOrderAsync(orderId);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet("order")]
    [HasPermission("inbound.order.view")]
    public async Task<ActionResult<List<InboundOrderDto>>> GetInboundOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var orders = await _inboundService.GetInboundOrdersAsync(page, pageSize, status);
        return Ok(orders);
    }

    [HttpPost("order/{orderId}/approve")]
    [HasPermission("inbound.order.approve")]
    public async Task<ActionResult<InboundOrderDto>> ApproveInboundOrder(Guid orderId)
    {
        var order = await _inboundService.ApproveInboundOrderAsync(orderId);
        return Ok(order);
    }

    [HttpPost("order/{orderId}/reject")]
    [HasPermission("inbound.order.reject")]
    public async Task<ActionResult<InboundOrderDto>> RejectInboundOrder(Guid orderId)
    {
        var order = await _inboundService.RejectInboundOrderAsync(orderId);
        return Ok(order);
    }

    // ========================
    // GOODS RECEIPT
    // ========================
    [HttpPost("gr-production-approve")]
    [HasPermission("inbound.gr.approve")]
    public async Task<ActionResult<GoodsReceiptDto>> ApproveGRProduction([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _inboundService.ApproveProductionReceipt(dto);
        return gr;
    }

    [HttpPost("gr-production-counting")]
    [HasPermission("inbound.gr.counting")]
    public async Task<ActionResult<GoodsReceiptDto>> CountingGRProduction([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _inboundService.CountingReceiptProduction(dto);
        return gr;
    }

    [HttpPost("receive-item")]
    [HasPermission("inbound.gr.receive")]
    public async Task<IActionResult> ReceiveItem([FromBody] GoodsReceiptItem1Dto dto)
    {
        if (dto == null || dto.Received_Qty <= 0)
        {
            return BadRequest("Số lượng nhập kho phải lớn hơn 0.");
        }

        try
        {
            await _inboundService.IncomingStockCount(dto);
            return Ok(new { message = "Cập nhật số lượng nhập kho thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("gr/{grId}/status")]
    public async Task<IActionResult> UpdateGRStatus(Guid grId, [FromBody] UpdateGRStatusDto dto)
    {
        await _inboundService.UpdateGRStatusAsync(grId, dto.Status);
        return Ok();
    }

    [HttpPost("gr")]
    [HasPermission("inbound.gr.create")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateGR([FromBody] GoodsReceiptDto dto)
    {
        var gr = await _inboundService.CreateGRAsync(dto);
        return CreatedAtAction(nameof(GetGRById), new { grId = gr.Id }, gr);
    }

    [HttpGet("gr/{grId}")]
    [HasPermission("inbound.gr.view")]
    public async Task<ActionResult<GoodsReceiptDto>> GetGRById(Guid grId)
    {
        var grs = await _inboundService.GetGRsAsync();
        var gr = grs.FirstOrDefault(x => x.Id == grId);
        if (gr == null) return NotFound();
        return Ok(gr);
    }

    [HttpGet("gr")]
    [HasPermission("inbound.gr.view")]
    public async Task<ActionResult<List<GoodsReceiptDto>>> GetGRs(
        [FromQuery] Guid? orderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var grs = await _inboundService.GetGRsAsync(orderId, page, pageSize);
        return Ok(grs);
    }

    [HttpDelete("gr/{grId}")]
    [HasPermission("inbound.gr.cancel")]
    public async Task<IActionResult> CancelGR(Guid grId)
    {
        await _inboundService.CancelGRAsync(grId);
        return NoContent();
    }

    [HttpGet("grbytype")]
    [HasPermission("inbound.order.view")]
    public async Task<ActionResult<List<GoodsReceiptDto>>> GetGRsByType([FromQuery] GRByTypeDto dto)
    {
        var entities = await _inboundService.getGRbytype(dto);
        var result = entities.Select(gr => new GoodsReceiptDto
        {
            Id = gr.Id,
            Code = gr.Code,
            InboundOrderId = gr.InboundOrderId,
            WarehouseId = gr.WarehouseId,
            ReceiptType = gr.ReceiptType,
            Status = gr.Status,
            CreatedAt = gr.CreatedAt,
            UpdatedAt = gr.UpdatedAt,
            Items = gr.Items.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Received_Qty = i.Received_Qty,
                Status = i.Status,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList(),
            ProductionReceiptItems = gr.Productions.Select(p => new ProductionReceiptItemDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                Quantity = p.Quantity,
                Receipt_Qty = p.Receipt_Qty,
                Status = p.Status,
                UnitId = p.UnitId,
                ExpiryDate = p.ExpiryDate,
                ManufacturingDate = p.ManufacturingDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    public class UpdateGRStatusDto
    {
        public InboundStatus Status { get; set; }
    }
}
