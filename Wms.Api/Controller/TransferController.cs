using Microsoft.AspNetCore.Mvc;
using Wms.Application.DTOS.Transfer;
using Wms.Application.Interfaces.Services.Transfer;
using Wms.Api.Middlewares; // <-- import HasPermission

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransferController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Lấy danh sách phiếu chuyển kho (có phân trang và lọc)
    /// </summary>
    [HttpGet]
    [HasPermission("transfer.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var results = await _transferService.GetTransfersAsync(page, pageSize, status);
        return Ok(results);
    }

    /// <summary>
    /// Lấy chi tiết một phiếu chuyển kho kèm danh sách sản phẩm
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("transfer.view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _transferService.GetTransferByIdAsync(id);
        if (result == null) return NotFound("Không tìm thấy phiếu chuyển kho.");
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một phiếu chuyển kho (Trạng thái Draft)
    /// </summary>
    [HttpPost]
    // [HasPermission("transfer.create")]
    public async Task<IActionResult> Create([FromBody] TransferOrderDto dto)
    {
        try
        {
            var result = await _transferService.CreateTransferAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }

    /// <summary>
    /// Duyệt phiếu chuyển kho (Thực hiện trừ kho nguồn, cộng kho đích)
    /// </summary>
    [HttpPost("{id}/approve")]
    [HasPermission("transfer.approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var result = await _transferService.ApproveTransferAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Hủy phiếu chuyển kho
    /// </summary>
    [HttpPost("{id}/cancel")]
    [HasPermission("transfer.cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var result = await _transferService.CancelTransferAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
