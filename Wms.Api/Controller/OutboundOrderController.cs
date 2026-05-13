using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wms.Api.Middlewares;
using Wms.Application.DTOS.Outbound;
using Wms.Application.Interfaces.Services.Outbound;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Api.Controllers
{
    [ApiController]
    [Route("api/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private readonly IOutboundOrderService _outboundOrderService;
        private readonly AppDbContext _dbContext;

        public OutboundOrderController(
            IOutboundOrderService outboundOrderService,
            AppDbContext dbContext)
        {
            _outboundOrderService = outboundOrderService;
            _dbContext = dbContext;
        }

        [HttpPut("goods-issue/{id}/status")]
        public async Task<IActionResult> UpdateGIStatus(Guid id, [FromBody] UpdateGIStatusDto dto)
        {
            await _outboundOrderService.UpdateGIStatusAsync(id, dto.Status);
            return Ok();
        }

        [HttpPost("order")]
        [HasPermission("outbound.order.create")]
        public async Task<ActionResult<OutboundOrderDto>> Create([FromBody] OutboundOrderDto dto)
        {
            var result = await _outboundOrderService.CreateOutboundOrderAsync(dto);
            return Ok(result);
        }

        [HttpGet("goods-issue/{id}")]
        [HasPermission("outbound.order.view")]
        public async Task<ActionResult<GoodsIssueDetailDto>> GetGoodsIssue(Guid id)
        {
            var result = await _outboundOrderService.GetGoodsIssueDetailAsync(id);
            if (result == null)
                return NotFound("Goods Issue not found");

            return Ok(result);
        }

        [HttpGet("goods-issues")]
        [HasPermission("outbound.order.view")]
        public async Task<ActionResult<List<GoodsIssueDto>>> QueryGoodsIssues([FromQuery] GoodsIssueQuery1Dto dto)
        {
            var result = await _outboundOrderService.QueryGoodsIssuesAsync(dto);
            return Ok(result);
        }

        [HttpGet("order/{id}")]
        [HasPermission("outbound.order.view")]
        public async Task<ActionResult<OutboundOrderDto>> Get(Guid id)
        {
            var result = await _outboundOrderService.GetOutboundOrderAsync(id);
            return Ok(result);
        }

        [HttpGet("order")]
        [HasPermission("outbound.order.view")]
        public async Task<ActionResult<List<OutboundOrderDto>>> Query([FromQuery] OutboundOrderQueryDto dto)
        {
            var result = await _outboundOrderService.QueryOutboundOrdersAsync(dto);
            return Ok(result);
        }

        [HttpPost("order/{id}/approve")]
        [HasPermission("outbound.order.approve")]
        public async Task<ActionResult<OutboundOrderDto>> Approve(Guid id)
        {
            var result = await _outboundOrderService.ApproveOutboundOrderAsync(id);
            return Ok(result);
        }

        [HttpPost("issue")]
        [HasPermission("outbound.order.issue")]
        public async Task<IActionResult> Issue([FromBody] IssueGoodsDto dto)
        {
            await _outboundOrderService.OutgoingStockCount(dto);
            return Ok(new { Message = "Issued successfully" });
        }

        [HttpPost("production")]
        public async Task<IActionResult> CreateProductionGI([FromBody] ProductionGoodsIssueCreateDto dto)
        {
            var gi = await _outboundOrderService.CreateProductionGIAsync(dto);
            return Ok(gi);
        }

        [HttpPost("goods-issue/{giId}/approve")]
        public async Task<ActionResult<GoodsIssueDto>> ApproveGI(Guid giId)
        {
            var result = await _outboundOrderService.ApproveGIAsync(giId);
            return Ok(result);
        }

        [HttpPost("picking")]
        [HasPermission("outbound.order.picking")]
        public async Task<IActionResult> Picking([FromBody] GoodsIssueItemDto dto)
        {
            await _outboundOrderService.Picking(dto);
            return Ok();
        }

        [HttpPost("order/{id}/reject")]
        [HasPermission("outbound.order.reject")]
        public async Task<ActionResult<OutboundOrderDto>> Reject(Guid id)
        {
            var result = await _outboundOrderService.RejectOutboundOrderAsync(id);
            return Ok(result);
        }

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var data = await _dbContext.Warehouses
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.WarehouseType,
                    x.Status
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
