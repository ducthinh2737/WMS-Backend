using Wms.Application.DTOS.Outbound;
using Wms.Domain.Entity.Outbound;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wms.Application.Interfaces.Services.Outbound
{
    public interface IGoodsIssueService
    {
        Task<GoodsIssueDto> CreateProductionGIAsync(ProductionGoodsIssueCreateDto dto);
        Task<GoodsIssue> CreateGIAsync(GoodsIssueDto dto);
        Task<GoodsIssueDto> ApproveGIAsync(Guid giId);
        Task UpdateGIStatusAsync(Guid giId, GIStatus status);
        Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId);
        Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto);
    }
}
