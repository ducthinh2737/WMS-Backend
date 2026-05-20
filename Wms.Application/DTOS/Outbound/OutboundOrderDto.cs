using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.Outbound;

namespace Wms.Application.DTOS.Outbound;

public class OutboundOrderLDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; } = null!;

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;

    public OutboundStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ApproveBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public List<OutboundOrderItemDto> Items { get; set; } = new();
    public List<GoodsIssueDto> GoodsIssues { get; set; } = new();
}

public class UpdateGIStatusDto
{
    public GIStatus Status { get; set; }
}

public class OutboundOrderDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; } = null!;

    public int CustomerId { get; set; }

    public OutboundStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ApproveBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public List<OutboundOrderItemDto> Items { get; set; } = new();
    public List<GoodsIssueDto> GoodsIssues { get; set; } = new();
}

public class OutboundOrderItemDto
{
    public Guid Id { get; set; }

    public int ProductId { get; set; }

    public decimal OrderQty { get; set; }
    public decimal IssuedQty { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public Guid WarehouseId { get; set; }

    public decimal Price { get; set; }

    public OutboundStatus Status { get; set; }
}

public class OutboundOrderCreateDto
{
    [Required]
    public int CustomerId { get; set; }

    public string? Code { get; set; }

    [Required]
    public List<OutboundOrderItemCreateDto> Items { get; set; } = new();
}

public class OutboundOrderItemCreateDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    public int UnitId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public Guid InventoryId { get; set; }
}

public class OutboundOrderUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public List<OutboundOrderItemUpdateDto> Items { get; set; } = new();
}

public class OutboundOrderItemUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal OrderQty { get; set; }
    public int UnitId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class OutboundOrderQueryDto
{
    public string? Code { get; set; }
    public int? CustomerId { get; set; }
    public OutboundStatus? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GoodsIssue1Dto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public Guid OutboundOrderId { get; set; }
    public string? OutboundOrderCode { get; set; }     
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }      
    public GIStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime? UpdateAt { get; set; }
    public List<GoodsIssueItem1Dto> Items { get; set; } = new();
}

public class GoodsIssueItem1Dto
{
    public Guid Id { get; set; }
    public Guid OutboundOrderItemId { get; set; }                
    public int ProductId { get; set; }
    public string? ProductName { get; set; }        
    public decimal Quantity { get; set; }
    public decimal Issued_Qty { get; set; }
    public int UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
}

public class GoodsIssueQuery1Dto
{
    public string? Code { get; set; }             
    public Guid? OutboundOrderId { get; set; }         
    public Guid? WarehouseId { get; set; }          
    public GIStatus? Status { get; set; }           
    public DateTime? IssuedFrom { get; set; }     
    public DateTime? IssuedTo { get; set; }        
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GoodsIssueDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string OutboundOrderCode { get; set; } = null!;
    public GIType Type { get; set; }
    public string WarehouseName { get; set; } = null!;
    public int Status { get; set; } // GIStatus enum as int

    public string? CustomerName { get; set; }
    public string? Address { get; set; }

    public List<GoodsIssueItemDtoForFrontend> Items { get; set; } = new();
}

public class GoodsIssueItemDtoForFrontend
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal PickedQty { get; set; }
    public decimal IssuedQty { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public int Status { get; set; }
    public List<GoodsIssueAllocate1Dto> Allocations { get; set; } = new();
}

public class GoodsIssueAllocate1Dto
{
    public Guid Id { get; set; }

    public Guid? LocationId { get; set; }

    public string LocationCode { get; set; } = null!;
    public decimal AllocatedQty { get; set; }
    public decimal PickedQty { get; set; }
    public decimal IssuedQty { get; set; }
    public int Status { get; set; } // GIAStatus
}