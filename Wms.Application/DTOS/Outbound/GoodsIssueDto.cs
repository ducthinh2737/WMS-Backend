using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.Outbound;

namespace Wms.Application.DTOS.Outbound
{
    // DTO trả về chi tiết GI
    public class GoodsIssueDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public Guid? OutboundOrderId { get; set; }   // ✅ nullable
        public GIType Type { get; set; }
        public Guid WarehouseId { get; set; }
        public GIStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime IssuedAt { get; set; }

        public List<GoodsIssueItemDto> Items { get; set; } = new();
    }


    // DTO chi tiết item GI
    public class GoodsIssueItemDto
    {
        public Guid Id { get; set; }
        public Guid GoodsIssueId { get; set; }

        public int ProductId { get; set; }

        public Guid? OutboundOrderItemId { get; set; } // ✅ đúng nghĩa

        public Guid? LocationId { get; set; }

        public int Quantity { get; set; }
        public int IssuedQty { get; set; }
        public Guid LotId { get; set; }

        public GIStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<GoodsIssueAllocateDto> Allocations { get; set; } = new();
    }

    public class IssueGoodsDto
    {
        public Guid GoodsIssueItemId { get; set; }
        public int IssuedQty { get; set; }
    }

    public class GoodsIssueAllocateDto
    {
        public Guid Id { get; set; }
        public Guid GoodsIssueItemId { get; set; }
        public Guid LocationId { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal PickedQty { get; set; }
        public GIAStatus Status { get; set; }
    }


    // DTO dùng để tạo GI từ đơn đã approve
    public class GoodsIssueCreateDto
    {
        [Required]
        public Guid OutboundOrderId { get; set; }
        [Required]
        public Guid WarehouseId { get; set; }
        [Required]
        public List<GoodsIssueItemCreateDto> Items { get; set; } = new();
    }

    public class ProductionGoodsIssueCreateDto
    {
        [Required]
        public Guid WarehouseId { get; set; }

        [Required]
        public List<ProductionGoodsIssueItemCreateDto> Items { get; set; } = new();
    }

    public class ProductionGoodsIssueItemCreateDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }


    public class GoodsIssueItemCreateDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public Guid LocationId { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    // DTO query/filter GI
    public class GoodsIssueQueryDto
    {
        public string? Code { get; set; }
        public Guid? OutboundOrderId { get; set; }
        public GIStatus? Status { get; set; }
        public DateTime? IssuedFrom { get; set; }
        public DateTime? IssuedTo { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

