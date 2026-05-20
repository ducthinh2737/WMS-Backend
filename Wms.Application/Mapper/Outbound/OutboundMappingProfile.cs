using AutoMapper;
using Wms.Application.DTOS.Outbound;
using Wms.Domain.Entity.Outbound;

namespace Wms.Application.Mapper.Outbound
{
    public class OutboundMappingProfile : Profile
    {
        public OutboundMappingProfile()
        {
            // OutboundOrder ↔ OutboundOrderDto
            CreateMap<OutboundOrder, OutboundOrderDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.GoodsIssues, opt => opt.MapFrom(src => src.GoodsIssues));

            // OutboundOrderItem → OutboundOrderItemDto
            CreateMap<OutboundOrderItem, OutboundOrderItemDto>()
                .ForMember(dest => dest.OrderQty, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.IssuedQty, opt => opt.MapFrom(src => src.Issued_Qty));

            // Create/Update DTO → Entity
            CreateMap<OutboundOrderCreateDto, OutboundOrder>();
            CreateMap<OutboundOrderItemCreateDto, OutboundOrderItem>();
            CreateMap<OutboundOrderUpdateDto, OutboundOrder>();
            CreateMap<OutboundOrderItemUpdateDto, OutboundOrderItem>();

            // GoodsIssue ↔ GoodsIssueDto
            CreateMap<GoodsIssue, GoodsIssueDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdateAt))
                .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => src.IssuedAt))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<GoodsIssueItem, GoodsIssueItemDto>();

            CreateMap<GoodsIssueCreateDto, GoodsIssue>();
            CreateMap<GoodsIssueItemCreateDto, GoodsIssueItem>();

            CreateMap<GoodsIssueAllocate, GoodsIssueAllocateDto>();

            CreateMap<GoodsIssueAllocate, GoodsIssueAllocate1Dto>()
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId))
                .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code));
        }
    }
}
