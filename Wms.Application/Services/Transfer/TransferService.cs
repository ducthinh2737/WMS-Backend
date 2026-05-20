using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Transfer;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Transfer;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Transfer;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.Transfer;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Transfer;

public class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IJwtService _jwt;

    public TransferService(AppDbContext db, IInventoryService inventoryService, IJwtService jwt)
    {
        _db = db;
        _inventoryService = inventoryService;
        _jwt = jwt;
    }

    public async Task<TransferOrderDto> CreateTransferAsync(TransferOrderDto dto)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId && dto.Items.Any(x => x.FromLocationId == x.ToLocationId))
            throw new Exception("Vị trí nguồn và đích không được trùng nhau.");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra tồn kho & khóa
                foreach (var item in dto.Items)
                {
                    await _inventoryService.LockStockAsync(
                        warehouseId: dto.FromWarehouseId,
                        locationId: item.FromLocationId,
                        productId: item.ProductId,
                        qty: item.Quantity,
                        note: "LOCK_FOR_TRANSFER"
                    );
                }

                var transfer = new TransferOrder
                {
                    Id = Guid.NewGuid(),
                    Code = $"TRF-{DateTime.UtcNow:yyyyMMdd-HHmm}",
                    FromWarehouseId = dto.FromWarehouseId,
                    ToWarehouseId = dto.ToWarehouseId,
                    Status = TransferStatus.Draft,
                    Note = dto.Note ?? "",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _jwt.GetUserId() ?? 1,
                    Items = dto.Items.Select(i => new TransferOrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = i.ProductId,
                        FromLocationId = i.FromLocationId,
                        ToLocationId = i.ToLocationId,
                        Quantity = i.Quantity,
                        Note = i.Note ?? ""
                    }).ToList()
                };

                _db.TransferOrders.Add(transfer);
                await _db.SaveChangesAsync();
                
                await transaction.CommitAsync();
                return await GetTransferByIdAsync(transfer.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }


    public async Task<TransferOrderDto> ApproveTransferAsync(Guid transferId)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _db.Set<TransferOrder>()
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == transferId);

                if (transfer == null)
                    throw new Exception("Không tìm thấy phiếu chuyển kho.");

                

                foreach (var item in transfer.Items)
                {
                    var stock = await _db.Set<Inventory>()
                        .Include(x => x.Lot)
                        .Include(x => x.Product)
                        .FirstOrDefaultAsync(x =>
                            x.LocationId == item.FromLocationId &&
                            x.ProductId == item.ProductId);

                    if (stock == null || stock.OnHandQuantity < item.Quantity)
                        throw new Exception(
                            $"Sản phẩm ID {item.ProductId} không đủ tồn kho tại vị trí nguồn."
                        );

                    decimal beforeQty = stock.OnHandQuantity;

                    // 1️⃣ Trừ kho nguồn
                    stock.OnHandQuantity -= item.Quantity;

                    // 2️⃣ Mở khóa
                    stock.LockedQuantity -= item.Quantity;

                    decimal afterQty = stock.OnHandQuantity;

                    // 3️⃣ Cộng kho đích
                    await _inventoryService.AdjustAsync(
                        warehouseId: transfer.ToWarehouseId,
                        locationId: item.ToLocationId,
                        productId: item.ProductId,
                        qty: item.Quantity,
                        actionType: InventoryActionType.TransferIn,
                        refCode: transfer.Code,
                        lotId: stock.LotId
                    );

                    // 4️⃣ History kho nguồn
                    _db.InventoryHistories.Add(new InventoryHistory
                    {
                        Id = Guid.NewGuid(),
                        WarehouseId = transfer.FromWarehouseId,
                        LocationId = item.FromLocationId,
                        ProductId = item.ProductId,
                        LotId = stock.LotId,
                        LotCode = stock.Lot?.Code,
                        QuantityChange = -item.Quantity,
                        BaseQuantityChange = -item.Quantity,
                        UnitId = stock.Product?.UnitId ?? 0,
                        BeforeQty = beforeQty,
                        AfterQty = afterQty,
                        ActionType = InventoryActionType.TransferOut,
                        ReferenceCode = transfer.Code,
                        Note = $"TransferOut for transfer {transfer.Code}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                transfer.Status = TransferStatus.Approved;
                transfer.ApprovedAt = DateTime.UtcNow;
                transfer.ApprovedBy = _jwt.GetUserId();
                transfer.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetTransferByIdAsync(transfer.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<TransferOrderDto> CancelTransferAsync(Guid transferId)
    {
        var transfer = await _db.Set<TransferOrder>().FindAsync(transferId);
        if (transfer == null) throw new Exception("Không tìm thấy phiếu.");
        if (transfer.Status == TransferStatus.Approved) throw new Exception("Phiếu đã duyệt không thể hủy.");

        transfer.Status = TransferStatus.Cancelled;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedBy = _jwt.GetUserId();

        await _db.SaveChangesAsync();
        return await GetTransferByIdAsync(transferId);
    }

    public async Task<TransferOrderDto> GetTransferByIdAsync(Guid id)
    {
        var transfer = await _db.Set<TransferOrder>()
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Items).ThenInclude(i => i.FromLocation)
            .Include(x => x.Items).ThenInclude(i => i.ToLocation)
            .FirstOrDefaultAsync(x => x.Id == id);

        return transfer == null ? null! : MapToDto(transfer);
    }

    public async Task<List<TransferOrderDto>> GetTransfersAsync(int page = 1, int pageSize = 20, string? status = null)
    {
        var query = _db.Set<TransferOrder>()
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<TransferStatus>(status, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);
        }

        var list = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    // Helper Mapping
    private TransferOrderDto MapToDto(TransferOrder t) => new()
    {
        Id = t.Id, // Frontend cần cái này để render Table
        Code = t.Code,
        FromWarehouseId = t.FromWarehouseId,
        FromWarehouseName = t.FromWarehouse?.Name, // Cần để hiện tên thay vì GUID
        ToWarehouseId = t.ToWarehouseId,
        ToWarehouseName = t.ToWarehouse?.Name,
        Status = t.Status.ToString(), // Chuyển Enum sang String cho FE dễ đọc
        Note = t.Note,
        CreatedAt = t.CreatedAt,
        Items = t.Items?.Select(i => new TransferOrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name,
            FromLocationId = i.FromLocationId,
            FromLocationCode = i.FromLocation?.Code,
            ToLocationId = i.ToLocationId,
            ToLocationCode = i.ToLocation?.Code,
            Quantity = i.Quantity,
            Note = i.Note
        }).ToList() ?? new()
    };
}