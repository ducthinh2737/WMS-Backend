using Wms.Application.DTOS.Outbound;
using Wms.Domain.Entity.Outbound;

namespace Wms.Application.Interfaces.Services.Outbound
{
    public interface IOutboundOrderService
    {
        Task<OutboundOrderDto> CreateOutboundOrderAsync(OutboundOrderDto dto);
        Task<OutboundOrderDto> ApproveOutboundOrderAsync(Guid orderId);
        Task<OutboundOrderDto> RejectOutboundOrderAsync(Guid orderId);
        Task UpdateGIStatusAsync(Guid giId, GIStatus status);
        Task<OutboundOrderDto> GetOutboundOrderAsync(Guid orderId);
        Task OutgoingStockCount(IssueGoodsDto dto);
        Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto);
        Task<List<OutboundOrderDto>> QueryOutboundOrdersAsync(OutboundOrderQueryDto dto);
        Task Picking(GoodsIssueItemDto dto);
        Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId);
        Task<GoodsIssueDto> CreateProductionGIAsync(
            ProductionGoodsIssueCreateDto dto);
        Task<GoodsIssueDto> ApproveGIAsync(Guid giId);
    }
}

