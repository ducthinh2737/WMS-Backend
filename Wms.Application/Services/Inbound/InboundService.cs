using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Inbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Inbound;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Inbound;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.Inbound;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Inbound;

public class InboundService : IInboundService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IJwtService _jwt;
    private readonly IWarehouseService _locationService;

    public InboundService(AppDbContext db, IInventoryService inventoryService, IJwtService jwt, IWarehouseService warehouseService)
    {
        _db = db;
        _locationService = warehouseService;
        _jwt = jwt;
        _inventoryService = inventoryService;
    }


    // ========================
    // PRIVATE HELPERS
    // ========================

    /// <summary>
    /// Tự động sinh mã đơn nhập theo format: IN-YYYYMMDD-0001
    /// </summary>
    private string GenerateInboundOrderCode()
    {
        var today = DateTime.UtcNow.Date;

        var countToday = _db.InboundOrders
            .Count(order => order.CreatedAt >= today && order.CreatedAt < today.AddDays(1));

        var seq = countToday + 1;

        return $"IN-{today:yyyyMMdd}-{seq:0000}";
    }

    /// <summary>
    /// Tự động sinh mã GR theo format: GR-YYYYMMDD-0001
    /// </summary>
    private string GenerateGRCode()
    {
        var today = DateTime.UtcNow.Date;

        var countToday = _db.GoodsReceipts
            .Count(gr => gr.CreatedAt >= today && gr.CreatedAt < today.AddDays(1));

        var seq = countToday + 1;

        return $"GR-{today:yyyyMMdd}-{seq:0000}";
    }


    /// <summary>
    /// Tự động sinh LotCode từ ProductId và ngày + giờ nhập.
    /// Format: LOT-{ProductId 6 chữ số}-{YYYYMMDD}-{HHmmss}
    /// Ví dụ: LOT-000042-20260308-143022
    /// Timestamp giây đảm bảo unique cho mỗi lần nhập trong ngày.
    /// </summary>
    private static string GenerateLotCode(int productId, DateTime date)
    {
        var productShort = productId.ToString("D6");
        var timestamp = date.ToString("HHmmss");
        return $"LOT-{productShort}-{date:yyyyMMdd}-{timestamp}";
    }

    // ========================
    // INBOUND ORDER
    // ========================

    public async Task<InboundOrderDto> CreateInboundOrderAsync(InboundOrderDto dto)
    {
        foreach (var item in dto.Items)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(s => s.Id == item.WarehouseId);

            if (warehouse == null)
                throw new BusinessException(
                    "WAREHOUSE_NOT_FOUND",
                    "Kho nhận không tồn tại"
                );

            if (warehouse.WarehouseType != WarehouseType.RawMaterial)
                throw new BusinessException(
                    "INVALID_WAREHOUSE_TYPE",
                    $"Kho \"{warehouse.Name}\" không phải kho nguyên vật liệu, không thể nhập hàng"
                );
        }

        var order = new InboundOrder
        {
            Id = Guid.NewGuid(),
            Code = GenerateInboundOrderCode(),   // ← tự sinh
            SupplierId = dto.SupplierId,
            CreateBy = _jwt.GetUserId(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new InboundOrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity,
                Status = InboundItemStatus.Pending,
                Received_qty = 0,
                Price = i.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList()
        };

        _db.Set<InboundOrder>().Add(order);
        await _db.SaveChangesAsync();

        return MapInboundOrderToDto(order);
    }


    public async Task<InboundOrderDto> ApproveInboundOrderAsync(Guid orderId)
    {
        var order = await _db.InboundOrders
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null) throw new NotFoundException("Order not found");

        if (order.Status != "Pending")
            throw new InvalidOperationException("Only Pending orders can be approved");

        order.Status = "Approve";
        order.ApprovedAt = DateTime.UtcNow;
        order.ApprovedBy = _jwt.GetUserId();
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (item.Status == InboundItemStatus.Pending)
            {
                item.Status = InboundItemStatus.Approve;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        var groupedItems = order.Items
                           .Where(i => i.Status == InboundItemStatus.Approve)
                           .GroupBy(i => i.WarehouseId);

        foreach (var group in groupedItems)
        {
            var warehouseId = group.Key;

            var gr = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                InboundOrderId = order.Id,
                WarehouseId = warehouseId,
                Code = GenerateGRCode(),
                Status = InboundStatus.Pending,
                ReceiptType = ReceiptType.Inbound,
                CreatedAt = DateTime.UtcNow,
                Items = new List<GoodsReceiptItem>()
            };

            foreach (var item in group)
            {
                var grItem = new GoodsReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    GoodsReceiptId = gr.Id,
                    Quantity = item.Quantity,
                    InboundOrderItemId = item.Id,
                    Received_Qty = 0,
                    CreatedAt = DateTime.UtcNow
                };
                gr.Items.Add(grItem);
            }

            _db.GoodsReceipts.Add(gr);
        }

        await _db.SaveChangesAsync();

        return MapInboundOrderToDto(order);
    }


    public async Task<InboundOrderDto> RejectInboundOrderAsync(Guid orderId)
    {
        var order = await _db.Set<InboundOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null) throw new NotFoundException("Order not found");

        if (order.Status != "Pending")
            throw new InvalidOperationException("Only Pending orders can be rejected");

        order.Status = "Rejected";
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (item.Status == InboundItemStatus.Pending)
            {
                item.Status = InboundItemStatus.Rejected;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return MapInboundOrderToDto(order);
    }


    public async Task<InboundOrderDto> GetInboundOrderAsync(Guid orderId)
    {
        var order = await _db.InboundOrders
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null) throw new NotFoundException("Order not found");

        return MapInboundOrderToDto(order);
    }

    // Không paging
    public async Task<List<InboundOrderDto>> GetInboundOrdersAsync()
    {
        return await GetInboundOrdersAsync(1, int.MaxValue);
    }


    // Có paging + optional status filter
    public async Task<List<InboundOrderDto>> GetInboundOrdersAsync(int page = 1, int pageSize = 20, string? status = null)
    {
        var query = _db.Set<InboundOrder>().Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var orders = await query.OrderByDescending(x => x.CreatedAt)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return orders.Select(MapInboundOrderToDto).ToList();
    }


    // ========================
    // GOODS RECEIPT
    // ========================

    public async Task<GoodsReceiptDto> CreateGRAsync(GoodsReceiptDto dto)
    {
        if (dto.WarehouseId == Guid.Empty)
            throw new BusinessRuleException("WarehouseId is required");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var gr = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                Code = GenerateGRCode(),
                InboundOrderId = dto.InboundOrderId,
                ReceiptType = ReceiptType.Production,
                WarehouseId = dto.WarehouseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Productions = dto.ProductionReceiptItems.Select(i => new ProductionReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Receipt_Qty = i.Receipt_Qty,
                    Status = GRIStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };

            _db.GoodsReceipts.Add(gr);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }


    public async Task<GoodsReceiptDto> ApproveProductionReceipt(GoodsReceiptDto dto)
    {
        var gr = await _db.GoodsReceipts.FirstOrDefaultAsync(s => s.Id == dto.Id);
        if (gr == null)
            throw new Exception("Đơn nhập không tồn tại");
        if (gr.Status == InboundStatus.Approve)
            throw new Exception("Chỉ có thể chấp thuận (Approve) đơn nhập có trạng thái là đang xử lý (Pending)");

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            gr.Status = InboundStatus.Approve;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }


    public async Task<List<GoodsReceipt>> getGRbytype(GRByTypeDto dto)
    {
        var GRlist = _db.GoodsReceipts
            .Include(s => s.Items)
            .Include(s => s.Productions)
            .Where(s => s.ReceiptType == dto.ReceiptType)
            .ToList();
        return GRlist;
    }

    public async Task<GoodsReceiptDto> CountingReceiptProduction(GoodsReceiptDto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var gr = await _db.GoodsReceipts
                .Include(s => s.Productions)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (gr == null)
                throw new Exception("Mã đơn nhập không tồn tại");

            if (gr.Status != InboundStatus.Approve && gr.Status != InboundStatus.Partially_Received)
                throw new Exception("Đơn nhập không hợp lệ để kiểm đếm");

            var location = await _locationService.GetReceivingLocationId(dto.WarehouseId);
            var receivedAt = DateTime.UtcNow;

            foreach (var item in dto.ProductionReceiptItems)
            {
                if (item.Receipt_Qty <= 0)
                    continue;

                var production = gr.Productions.FirstOrDefault(s => s.Id == item.Id);
                if (production == null)
                    throw new Exception("Chi tiết sản phẩm nhập không tồn tại");

                if (production.Receipt_Qty + item.Receipt_Qty > production.Quantity)
                    throw new Exception("Số lượng nhận vượt quá số lượng yêu cầu");

                production.Receipt_Qty += item.Receipt_Qty;

                production.Status = production.Receipt_Qty == production.Quantity
                    ? GRIStatus.Complete
                    : GRIStatus.Partial;

                await _inventoryService.AdjustAsync(
                    dto.WarehouseId,
                    location,
                    item.ProductId,
                    item.Receipt_Qty,
                    InventoryActionType.Receive,
                    refCode: gr.Code,
                    lotCode: GenerateLotCode(item.ProductId, receivedAt),
                    expiryDate: item.ExpiryDate,
                    manufacturingDate: item.ManufacturingDate
                );
            }

            if (gr.Productions.All(s => s.Status == GRIStatus.Complete))
                gr.Status = InboundStatus.Complete;
            else if (gr.Productions.Any(s => s.Status != GRIStatus.Pending))
                gr.Status = InboundStatus.Partially_Received;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }


    // Không paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null)
    {
        return await GetGRsAsync(poId, page: 1, pageSize: int.MaxValue);
    }

    public async Task IncomingStockCount(GoodsReceiptItem1Dto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var item = await _db.GoodsReceiptItems
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                var product = await _db.Products.FirstOrDefaultAsync(s => s.Id == dto.ProductId);
                if (product == null)
                    throw new Exception("Sản phẩm không tồn tại");

                if (item == null)
                    throw new Exception("Dòng hàng nhập kho không tồn tại (ID null hoặc sai)");

                // 1️⃣ Update GR Item
                item.Received_Qty += dto.Received_Qty;

                if (item.Received_Qty >= item.Quantity)
                    item.Status = GRIStatus.Complete;
                else if (item.Received_Qty > 0)
                    item.Status = GRIStatus.Partial;

                // 2️⃣ Update Inbound Item + Inventory
                var orderItem = await _db.InboundOrderItems
                    .FirstOrDefaultAsync(p => p.Id == item.InboundOrderItemId);
                var gr = await _db.GoodsReceipts
                            .Include(p => p.Items)
                            .FirstOrDefaultAsync(s => s.Id == item.GoodsReceiptId);

                if (orderItem != null)
                {
                    orderItem.Received_qty += dto.Received_Qty;

                    if (orderItem.Received_qty >= item.Quantity)
                        orderItem.Status = InboundItemStatus.Complete;
                    else
                        orderItem.Status = InboundItemStatus.Partially_Received;

                    var locationId = await _locationService
                        .GetReceivingLocationId(orderItem.WarehouseId);

                    await _inventoryService.AdjustAsync(
                        orderItem.WarehouseId,
                        locationId,
                        item.ProductId,
                        dto.Received_Qty,
                        InventoryActionType.Receive,
                        refCode: gr.Code,
                        lotCode: GenerateLotCode(item.ProductId, DateTime.UtcNow),
                        expiryDate: dto.ExpiryDate,
                        manufacturingDate: dto.ManufacturingDate
                    );
                }

                // 3️⃣ Update GR status
                if (gr != null)
                {
                    gr.Status = gr.Items.All(i => i.Status == GRIStatus.Complete)
                        ? InboundStatus.Complete
                        : InboundStatus.Partially_Received;

                    // 4️⃣ Update Order status
                    var order = await _db.InboundOrders
                        .Include(s => s.Items)
                        .FirstOrDefaultAsync(s => s.Id == gr.InboundOrderId);

                    if (order != null)
                    {
                        if (order.Items.All(x => x.Received_qty >= x.Quantity))
                            order.Status = InboundStatus.Complete.ToString();
                        else if (order.Items.Any(x => x.Received_qty > 0))
                            order.Status = InboundStatus.Partially_Received.ToString();
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }


    // Có paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(
        Guid? orderId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.Set<GoodsReceipt>()
            .Include(x => x.Items)
            .Include(x => x.Productions)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(x => x.InboundOrderId == orderId.Value);

        var grs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return grs.Select(MapGRToDto).ToList();
    }





    public async Task CancelGRAsync(Guid grId)
    {
        var gr = await _db.Set<GoodsReceipt>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == grId);
        if (gr == null) throw new NotFoundException("GR not found");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in gr.Items)
            {
                await _inventoryService.Adjust1Async(
                    warehouseId: gr.WarehouseId,
                    locationId: null,
                    productId: item.ProductId,
                    qtyChange: -item.Quantity,
                    actionType: InventoryActionType.AdjustDecrease,
                    refCode: gr.Code
                );
                item.UpdatedAt = DateTime.UtcNow;
            }

            gr.UpdatedAt = DateTime.UtcNow;
            _db.Set<GoodsReceipt>().Remove(gr);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ========================
    // Private mapping helpers
    // ========================

    private static InboundOrderDto MapInboundOrderToDto(InboundOrder order) => new()
    {
        Id = order.Id,
        Code = order.Code,
        SupplierId = order.SupplierId,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        ApprovedAt = order.ApprovedAt,
        Items = order.Items.Select(i => new InboundOrderItemDto
        {
            ProductId = i.ProductId,
            ReceivedQuantity = i.Received_qty,
            Status = i.Status,
            Quantity = i.Quantity,
            Price = i.Price,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList()
    };


    private static GoodsReceiptDto MapGRToDto(GoodsReceipt gr)
    {
        return gr.ReceiptType switch
        {
            ReceiptType.Inbound => MapInboundGRToDto(gr),
            ReceiptType.Production => MapProductionGRToDto(gr),
            _ => throw new Exception($"ReceiptType không hợp lệ: {gr.ReceiptType}")
        };
    }


    private static GoodsReceiptDto MapProductionGRToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        Code = gr.Code,
        InboundOrderId = gr.InboundOrderId,
        WarehouseId = gr.WarehouseId,
        ReceiptType = gr.ReceiptType,
        Status = gr.Status,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,

        ProductionReceiptItems = gr.Productions.Select(p => new ProductionReceiptItemDto
        {
            Id = p.Id,
            GoodsReceiptId = p.GoodsReceiptId,
            ProductId = p.ProductId,
            Quantity = p.Quantity,
            Receipt_Qty = p.Receipt_Qty,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList()
    };

    public async Task<ScanReceiveResultDto> ScanInboundOrderInfoAsync(string orderCode)
    {
        var order = await _db.InboundOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Code == orderCode.Trim().ToUpper());

        if (order == null)
            throw new NotFoundException($"Không tìm thấy đơn hàng: {orderCode}");

        if (order.Status == "Rejected")
            throw new BusinessException("ORDER_REJECTED", "Đơn hàng đã bị từ chối");

        if (order.Status == "Complete")
            throw new BusinessException("ORDER_COMPLETED", "Đơn hàng đã hoàn thành");

        // Lấy GR hiện có (nếu đã approve trước đó)
        var existingGRs = await _db.GoodsReceipts
            .Include(x => x.Items)
            .Where(x => x.InboundOrderId == order.Id
                     && x.ReceiptType == ReceiptType.Inbound
                     && x.Status != InboundStatus.Complete)
            .ToListAsync();

        return new ScanReceiveResultDto
        {
            InboundOrder = MapInboundOrderToDto(order),
            GoodsReceipts = existingGRs.Select(MapInboundGRToDto).ToList(),
            NeedsApproval = order.Status == "Pending"
        };
    }


    /// <summary>
    /// BƯỚC 2 — Confirm: User đã xác nhận → tự động Approve đơn + tạo GR nếu cần.
    /// POST /api/inbound/scan/{orderCode}/confirm
    /// </summary>
    public async Task<ScanReceiveResultDto> ConfirmAndReceiveAsync(string orderCode)
    {
        var order = await _db.InboundOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Code == orderCode.Trim().ToUpper());

        if (order == null)
            throw new NotFoundException($"Không tìm thấy đơn hàng: {orderCode}");

        if (order.Status == "Rejected")
            throw new BusinessException("ORDER_REJECTED", "Đơn hàng đã bị từ chối");

        if (order.Status == "Complete")
            throw new BusinessException("ORDER_COMPLETED", "Đơn hàng đã hoàn thành");

        // Chỉ approve nếu còn Pending
        if (order.Status == "Pending")
        {
            order.Status = "Approve";
            order.ApprovedAt = DateTime.UtcNow;
            order.ApprovedBy = _jwt.GetUserId();
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items.Where(i => i.Status == InboundItemStatus.Pending))
            {
                item.Status = InboundItemStatus.Approve;
                item.UpdatedAt = DateTime.UtcNow;
            }

            // Tạo GR theo từng kho (giống ApproveInboundOrderAsync)
            var grouped = order.Items
                .Where(i => i.Status == InboundItemStatus.Approve)
                .GroupBy(i => i.WarehouseId);

            foreach (var group in grouped)
            {
                var gr = new GoodsReceipt
                {
                    Id = Guid.NewGuid(),
                    InboundOrderId = order.Id,
                    WarehouseId = group.Key,
                    Code = GenerateGRCode(),
                    Status = InboundStatus.Pending,
                    ReceiptType = ReceiptType.Inbound,
                    CreatedAt = DateTime.UtcNow,
                    Items = group.Select(item => new GoodsReceiptItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        InboundOrderItemId = item.Id,
                        Received_Qty = 0,
                        CreatedAt = DateTime.UtcNow
                    }).ToList()
                };
                _db.GoodsReceipts.Add(gr);
            }

            await _db.SaveChangesAsync();
        }

        // Lấy tất cả GR chưa hoàn thành
        var activeGRs = await _db.GoodsReceipts
            .Include(x => x.Items)
            .Where(x => x.InboundOrderId == order.Id
                     && x.ReceiptType == ReceiptType.Inbound
                     && x.Status != InboundStatus.Complete)
            .ToListAsync();

        if (!activeGRs.Any())
            throw new BusinessException("ALL_GR_COMPLETED", "Tất cả phiếu nhập đã hoàn thành");

        return new ScanReceiveResultDto
        {
            InboundOrder = MapInboundOrderToDto(order),
            GoodsReceipts = activeGRs.Select(MapInboundGRToDto).ToList(),
            NeedsApproval = false
        };
    }


    public async Task UpdateGRStatusAsync(Guid grId, InboundStatus status)
    {
        var gr = await _db.GoodsReceipts
            .FirstOrDefaultAsync(x => x.Id == grId);

        if (gr == null)
            throw new NotFoundException("GoodsReceipt không tồn tại");

        gr.Status = status;
        gr.UpdatedAt = DateTime.UtcNow;

        // Đồng bộ các item con
        var items = await _db.GoodsReceiptItems
            .Where(x => x.GoodsReceiptId == grId)
            .ToListAsync();

        foreach (var item in items)
        {
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private static GoodsReceiptDto MapInboundGRToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        Code = gr.Code,
        InboundOrderId = gr.InboundOrderId,
        WarehouseId = gr.WarehouseId,
        ReceiptType = gr.ReceiptType,
        Status = gr.Status,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,

        Items = gr.Items == null
            ? new List<GoodsReceiptItemDto>()
            : gr.Items.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Received_Qty = i.Received_Qty,
                Status = i.Status,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList()
    };

    public async Task<ScanReceiveResultDto> ScanAndProcessAsync(ScanQRPayloadDto payload)
    {
        // ── Validate warehouse ──────────────────────────────────────────
        foreach (var item in payload.Items)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Id == item.WarehouseId);

            if (warehouse == null)
                throw new BusinessException("WAREHOUSE_NOT_FOUND", "Kho nhận không tồn tại");

            if (warehouse.WarehouseType != WarehouseType.RawMaterial)
                throw new BusinessException(
                    "INVALID_WAREHOUSE_TYPE",
                    $"Kho \"{warehouse.Name}\" không phải kho nguyên vật liệu"
                );
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // ── BƯỚC 1: Tạo đơn nhập ────────────────────────────────────────
                var order = new InboundOrder
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateInboundOrderCode(),
                    SupplierId = payload.SupplierId,
                    CreateBy = _jwt.GetUserId(),
                    Status = "Approve",                  // Tạo thẳng Approve
                    ApprovedAt = DateTime.UtcNow,
                    ApprovedBy = _jwt.GetUserId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = payload.Items.Select(i => new InboundOrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = i.ProductId,
                        WarehouseId = i.WarehouseId,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Status = InboundItemStatus.Approve,          // Approve luôn
                        Received_qty = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList()
                };

                _db.InboundOrders.Add(order);

                // ── BƯỚC 2: Tạo GR theo từng kho ─────────────────────────
                var groupedItems = order.Items.GroupBy(i => i.WarehouseId);
                var createdGRs = new List<GoodsReceipt>();

                foreach (var group in groupedItems)
                {
                    var gr = new GoodsReceipt
                    {
                        Id = Guid.NewGuid(),
                        InboundOrderId = order.Id,
                        WarehouseId = group.Key,
                        Code = GenerateGRCode(),
                        Status = InboundStatus.Pending,
                        ReceiptType = ReceiptType.Inbound,
                        CreatedAt = DateTime.UtcNow,
                        Items = group.Select(item => new GoodsReceiptItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            GoodsReceiptId = Guid.Empty,  // EF set
                            Quantity = item.Quantity,
                            InboundOrderItemId = item.Id,
                            Received_Qty = 0,
                            CreatedAt = DateTime.UtcNow
                        }).ToList()
                    };

                    _db.GoodsReceipts.Add(gr);
                    createdGRs.Add(gr);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ScanReceiveResultDto
                {
                    InboundOrder = MapInboundOrderToDto(order),
                    GoodsReceipts = createdGRs.Select(MapInboundGRToDto).ToList(),
                    NeedsApproval = false
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }


}