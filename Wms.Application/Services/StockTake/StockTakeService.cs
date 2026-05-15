using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Application.DTOS.StockTake;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.StockTake;
using Wms.Domain.Entity.StockTakes;
using Wms.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;
using Wms.Domain.Enums.StockTakes;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.StockTake
{
    public class StockTakeService : IStockTakeService
    {
        private readonly AppDbContext _db;
        private readonly IInventoryService _inventoryService;

        public StockTakeService(AppDbContext db, IInventoryService inventoryService)
        {
            _db = db;
            _inventoryService = inventoryService;
        }

        // 1. Tạo phiếu nháp
        public async Task<StockTakeDto> CreateAsync(CreateStockTakeDto dto)
        {
            var stockTake = new Domain.Entity.StockTakes.StockTake
            {
                Id = Guid.NewGuid(),
                Code = $"ST-{DateTime.UtcNow:yyyyMMdd-HHmm}",
                WarehouseId = dto.WarehouseId,
                Description = dto.Description,
                Status = StockTakeStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockTakes.Add(stockTake);
            await _db.SaveChangesAsync();
            return await GetByIdAsync(stockTake.Id);
        }

        // 2. Chốt số liệu (Snapshot)
        public async Task<StockTakeDto> StartAsync(Guid id)
        {
            var st = await _db.StockTakes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (st == null || st.Status != StockTakeStatus.Draft) throw new Exception("Phiếu không hợp lệ.");

            // Lấy tất cả tồn kho của kho này
            var currentStocks = await _db.Inventories
                .Where(x => x.WarehouseId == st.WarehouseId && x.OnHandQuantity > 0)
                .ToListAsync();

            var newItems = currentStocks.Select(s => new StockTakeItem
            {
                Id = Guid.NewGuid(),
                StockTakeId = st.Id,
                LocationId = s.LocationId,
                ProductId = s.ProductId,
                LotId = s.LotId,
                SystemQty = s.OnHandQuantity,
                CountedQty = s.OnHandQuantity
            }).ToList();

            _db.StockTakeItems.AddRange(newItems);

            try
            {
                st.Status = StockTakeStatus.InProgress;
                _db.StockTakes.Update(st);
                await _db.SaveChangesAsync();
                return await GetByIdAsync(id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
                    throw new Exception($"Lỗi: Phiếu kiểm kê {id} đã bị xóa khỏi Database.");
                }
                else
                {
                    throw new Exception($"Lỗi xung đột dữ liệu trên phiếu {id}. Dữ liệu trong DB đã bị thay đổi bởi tiến trình khác.");
                }
            }
        }

        // 3. Cập nhật số lượng đếm được (Dùng cho nhân viên kho nhập liệu)
        public async Task<StockTakeDto> UpdateCountsAsync(SubmitCountDto dto)
{
    var stockTake = await _db.StockTakes
        .FirstOrDefaultAsync(x => x.Id == dto.StockTakeId);

    if (stockTake == null)
        throw new Exception("Phiếu kiểm kê không tồn tại.");

    if (stockTake.Status != StockTakeStatus.InProgress)
        throw new Exception("Phiếu kiểm kê không ở trạng thái kiểm kê.");

    var items = await _db.StockTakeItems
        .Where(x => x.StockTakeId == dto.StockTakeId)
        .ToListAsync();

    foreach (var count in dto.Counts)
    {
        // CHỈ CẤM ÂM
        if (count.CountedQty < 0)
            throw new Exception("Số lượng không được âm.");

        var item = items.FirstOrDefault(x =>
            x.ProductId == count.ProductId &&
            x.LocationId == count.LocationId &&
            x.LotId == count.LotId);

        if (item == null)
            continue;

        item.CountedQty = count.CountedQty;
        item.Note = count.Note;
    }

    await _db.SaveChangesAsync();

    return await GetByIdAsync(dto.StockTakeId);
}

        // 4. Hoàn tất và tự động điều chỉnh kho
        public async Task<StockTakeDto> CompleteAsync(Guid id)
{
    var strategy = _db.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        using var trans = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakes
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (st == null)
                throw new Exception("Phiếu kiểm kê không tồn tại.");

            if (st.Status != StockTakeStatus.InProgress)
                throw new Exception("Phiếu kiểm kê không hợp lệ.");

            foreach (var item in st.Items)
            {
                // VALIDATE
                if (item.CountedQty < 0)
                    throw new Exception("Số lượng kiểm kê không hợp lệ.");

                // variance
                decimal diff = item.CountedQty - item.SystemQty;

                // chỉ adjust khi có lệch
                if (diff != 0)
                {
                    await _inventoryService.AdjustAsync(
                        warehouseId: st.WarehouseId,
                        locationId: item.LocationId ?? Guid.Empty,
                        productId: item.ProductId,
                        qty: diff,
                        actionType: InventoryActionType.StockTakeAdjustment,
                        lotId: item.LotId,
                        refCode: st.Code
                    );
                }
            }

            st.Status = StockTakeStatus.Completed;
            st.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await trans.CommitAsync();

            return await GetByIdAsync(id);
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    });
}

        // 5. Get Detail & List (Mapping)
        public async Task<StockTakeDto> GetByIdAsync(Guid id)
        {
            var t = await _db.StockTakes
                .Include(x => x.Warehouse)
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t == null) return null!;

            return new StockTakeDto
            {
                Id = t.Id,
                Code = t.Code,
                WarehouseName = t.Warehouse?.Name,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                Items = t.Items.Select(i => new StockTakeItemDto
                {
                    Id = i.Id,
                    LocationId = i.LocationId ?? Guid.Empty,
                    LocationCode = i.Location?.Code,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    LotId = i.LotId,
                    SystemQty = i.SystemQty,
                    CountedQty = i.CountedQty,
                    Difference = i.Difference,
                    Note = i.Note
                }).ToList()
            };
        }

        public async Task<List<StockTakeDto>> GetListAsync(int page = 1, int pageSize = 20)
        {
            return await _db.StockTakes
                .Include(x => x.Warehouse)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => new StockTakeDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    WarehouseName = t.Warehouse.Name,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }
    }
}
