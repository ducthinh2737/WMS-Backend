using Wms.Domain.Entity.Outbound;
using System;
using System.Threading.Tasks;

namespace Wms.Application.Interfaces.Services.Outbound
{
    public interface IAllocationService
    {
        Task AllocateInventoryAsync(GoodsIssueItem item, Guid warehouseId, decimal? quantityOverride = null);
    }
}
