using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Outbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Outbound;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Outbound;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Outbound
{
    public class OutboundOrderService : IOutboundOrderService

    {
        private readonly AppDbContext _dbContext;
        private readonly IInventoryService _inventoryService;
        private readonly IJwtService jwtService;
        private readonly IMapper _mapper;
        private readonly IWarehouseService _warehouse;

        public OutboundOrderService(AppDbContext dbContext, IMapper mapper, IInventoryService _inventory, IJwtService jwt, IWarehouseService warehouse)
        {
            _inventoryService = _inventory;
            _dbContext = dbContext;
            _warehouse = warehouse;
            jwtService = jwt;
            _mapper = mapper;
        }


        #region Create / Update / Get


       

        public async Task<OutboundOrderDto> CreateOutboundOrderAsync(OutboundOrderDto dto)
        {
            foreach (var item in dto.Items)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    throw new BusinessException(
                        "PRODUCT_NOT_FOUND",
                        $"Sản phẩm \"{item.ProductId}\" không tồn tại"
                    );

                if (product.Type != ProductType.Production)
                    throw new BusinessException(
                        "INVALID_PRODUCT_TYPE",
                        $"Sản phẩm \"{product.Name}\" không phải là thành phẩm nên không thể bán"
                    );
            }

            var order = new OutboundOrder
            {
                Id = Guid.NewGuid(),
                Code = GenerateOutboundOrderCode(),
                CustomerId = dto.CustomerId,
                Status = OutboundStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new OutboundOrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    OutboundOrderId = dto.Id,
                    Status = OutboundStatus.Pending,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.OrderQty,
                    Issued_Qty = 0,
                    Price = i.Price,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            if (jwtService.GetUserId().HasValue)
                order.CreatedBy = jwtService.GetUserId().Value;

            _dbContext.Set<OutboundOrder>().Add(order);
            await _dbContext.SaveChangesAsync();

            // Reload để đảm bảo Items được load đầy đủ
            var savedOrder = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == order.Id);

            return _mapper.Map<OutboundOrderDto>(savedOrder ?? order);
        }




        public async Task<OutboundOrderDto> GetOutboundOrderAsync(Guid orderId)
        {
            var entity = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.GoodsIssues)
                    .ThenInclude(gi => gi.Items)
                        .ThenInclude(giItem => giItem.Product)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (entity == null)
                throw new Exception("OutboundOrder not found");

            return _mapper.Map<OutboundOrderDto>(entity);
        }


        public async Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId)
        {
            var gi = await _dbContext.Set<GoodsIssue>()
                .Where(g => g.Id == goodsIssueId)
                .Include(g => g.OutboundOrder)
                .Include(g => g.Warehouse)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Product)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.Location)
                .Select(g => new GoodsIssueDetailDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    OutboundOrderCode = g.OutboundOrder.Code,

                    Type = g.Type,
                    WarehouseName = g.Warehouse.Name,
                    Status = (int)g.Status,
                    Items = g.Items.Select(i => new GoodsIssueItemDtoForFrontend
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductCode = i.Product.Code,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        PickedQty = i.Allocations.Sum(a => a.PickedQty),
                        IssuedQty = i.Issued_Qty,
                        Status = (int)i.Status,
                        Allocations = i.Allocations.Select(a => new GoodsIssueAllocate1Dto
                        {
                            Id = a.Id,
                            // ✅ QUAN TRỌNG: Bạn phải gán LocationId ở đây!
                            LocationId = a.LocationId,

                            // ✅ Lấy Code trực tiếp từ Object Location đã Include
                            LocationCode = a.Location.Code ?? "Chưa xác định",

                            AllocatedQty = a.AllocatedQty,
                            PickedQty = a.PickedQty,
                            Status = (int)a.Status
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return gi;
        }

        public async Task<List<OutboundOrderDto>> QueryOutboundOrdersAsync(OutboundOrderQueryDto dto)
        {
            var query = _dbContext.OutboundOrders
                .Include(x => x.Items)
                .Include(x => x.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.CustomerId.HasValue)
                query = query.Where(x => x.CustomerId == dto.CustomerId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == (OutboundStatus)dto.Status);

            if (dto.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= dto.CreatedFrom.Value);

            if (dto.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedAt <= dto.CreatedTo.Value);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            return _mapper.Map<List<OutboundOrderDto>>(list);
        }

        public async Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto)
        {
            var query = _dbContext.GoodsIssues
                .Include(x => x.OutboundOrder)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Allocations).ThenInclude(s => s.Location)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.OutboundOrderId.HasValue)
                query = query.Where(x => x.OutboundOrderId == dto.OutboundOrderId.Value);

            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == (GIStatus)dto.Status.Value);

            if (dto.IssuedFrom.HasValue)
                query = query.Where(x => x.IssuedAt >= dto.IssuedFrom.Value);

            if (dto.IssuedTo.HasValue)
                query = query.Where(x => x.IssuedAt <= dto.IssuedTo.Value);

            query = query.OrderByDescending(x => x.IssuedAt);

            var entities = await query
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var resultList = _mapper.Map<List<GoodsIssueDto>>(entities);

            for (int i = 0; i < entities.Count; i++)
            {
                resultList[i].IssuedAt = entities[i].IssuedAt;
            }

            return resultList;
        }

        #endregion

        #region Approve / Reject

        public async Task<GoodsIssueDto> CreateProductionGIAsync(
    ProductionGoodsIssueCreateDto dto)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);

            if (warehouse == null)
                throw new Exception("Kho không tồn tại");


            var gi = new GoodsIssue
            {
                Id = Guid.NewGuid(),
                Code = GenerateGICode(),
                Type = GIType.Production,
                WarehouseId = dto.WarehouseId,
                Status = GIStatus.Pending,
                CreateAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Issued_Qty = 0,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            _dbContext.GoodsIssues.Add(gi);
            await _dbContext.SaveChangesAsync();

            // ✅ MAP SANG DTO
            return _mapper.Map<GoodsIssueDto>(gi);
        }


        public async Task<GoodsIssueDto> ApproveGIAsync(Guid giId)
        {
            // 1️⃣ Atomic approve
            var affected = await _dbContext.GoodsIssues
                .Where(x => x.Id == giId && x.Status == GIStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, GIStatus.Approve)
                    .SetProperty(x => x.UpdateAt, DateTime.UtcNow)
                );

            if (affected == 0)
                throw new Exception("GoodsIssue đã được approve hoặc không tồn tại");

            // 2️⃣ Load lại GI
            var gi = await _dbContext.GoodsIssues
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Warehouse)
                .FirstAsync(x => x.Id == giId);

            // 3️⃣ Validate nghiệp vụ
   

            // 4️⃣ Allocate với xử lý hạn sử dụng
            foreach (var item in gi.Items)
            {
                var existed = await _dbContext.GoodsIssueAllocates
                    .AnyAsync(a => a.GoodsIssueItemId == item.Id);

                if (existed)
                    continue;

                decimal remainingQty = item.Quantity;

                // ✅ Lấy locations
                var locations = await _inventoryService.GetAvailableLocations(
                    item.ProductId,
                    gi.WarehouseId
                );

                // ✅ LỌC BỎ CÁC LÔ ĐÃ HẾT HẠN
                var validLocations = locations
                    .Where(loc => !loc.ExpiryDate.HasValue || loc.ExpiryDate.Value > DateTime.UtcNow)
                    .ToList();

                // ✅ KIỂM TRA: CÓ LÔ CÒN HẠN KHÔNG?
                if (!validLocations.Any())
                {
                    var product = await _dbContext.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    var expiredCount = locations.Count(loc =>
                        loc.ExpiryDate.HasValue && loc.ExpiryDate.Value <= DateTime.UtcNow);

                    throw new Exception(
                        $"Không thể phân bổ sản phẩm '{product?.Name ?? item.ProductId.ToString()}'. " +
                        $"Tất cả {locations.Count} lô hàng trong kho đều đã hết hạn sử dụng. " +
                        $"Vui lòng kiểm tra lại tồn kho."
                    );
                }

                // ✅ TÍNH TỔNG SỐ LƯỢNG KHẢ DỤNG (chỉ lô còn hạn)
                decimal totalValidQty = validLocations.Sum(loc => loc.AvailableQty);

                // ✅ CẢNH BÁO NẾU KHÔNG ĐỦ HÀNG CÒN HẠN
                if (totalValidQty < item.Quantity)
                {
                    var product = await _dbContext.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    var expiredQty = locations
                        .Where(loc => loc.ExpiryDate.HasValue && loc.ExpiryDate.Value <= DateTime.UtcNow)
                        .Sum(loc => loc.AvailableQty);

                    Console.WriteLine($"⚠️ WARNING: Sản phẩm '{product?.Name}' không đủ hàng còn hạn!");
                    Console.WriteLine($"   - Yêu cầu: {item.Quantity}");
                    Console.WriteLine($"   - Có sẵn (còn hạn): {totalValidQty}");
                    Console.WriteLine($"   - Thiếu: {item.Quantity - totalValidQty}");
                    Console.WriteLine($"   - Hàng đã hết hạn: {expiredQty}");

                    // Uncomment nếu muốn chặn approve khi không đủ hàng
                    // throw new Exception(
                    //     $"Sản phẩm '{product?.Name}' chỉ còn {totalValidQty} đơn vị hợp lệ, " +
                    //     $"không đủ cho {item.Quantity} đơn vị yêu cầu."
                    // );
                }

                // ✅ FEFO: Sắp xếp theo ExpiryDate (null cuối cùng)
                var sortedLocations = validLocations
                    .OrderBy(loc => loc.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(loc => loc.LotCode)
                    .ToList();

                Console.WriteLine($"\n=== GI {gi.Code}: Allocating {item.Quantity} x Product {item.ProductId} ===");

                foreach (var loc in sortedLocations)
                {
                    if (remainingQty <= 0)
                        break;

                    var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                    Console.WriteLine($"✅ Allocate {allocQty} from Location {loc.Code}, " +
                                    $"Lot {loc.LotCode}, " +
                                    $"Expiry: {loc.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A"}");

                    // ⚠️ CẢNH BÁO nếu lô gần hết hạn (< 30 ngày)
                    if (loc.ExpiryDate.HasValue)
                    {
                        var daysUntilExpiry = (loc.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
                        if (daysUntilExpiry < 30)
                        {
                            Console.WriteLine($"  ⚠️ WARNING: Lot expires in {Math.Round(daysUntilExpiry)} days!");
                        }
                    }

                    _dbContext.GoodsIssueAllocates.Add(new GoodsIssueAllocate
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueItemId = item.Id,
                        LocationId = loc.Id,
                        LotId = loc.LotId,
                        AllocatedQty = allocQty,
                        PickedQty = 0,
                        Status = GIAStatus.Planned
                    });

                    remainingQty -= allocQty;
                }

                // Nếu thiếu hàng (sau khi allocate hết lô còn hạn)
                if (remainingQty > 0)
                {
                    Console.WriteLine($"⚠️ Backorder: {remainingQty} units");

                    _dbContext.GoodsIssueAllocates.Add(new GoodsIssueAllocate
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueItemId = item.Id,
                        LocationId = null,
                        LotId = Guid.Empty,
                        AllocatedQty = remainingQty,
                        PickedQty = 0,
                        Status = GIAStatus.Planned
                    });
                }
            }

            // 5️⃣ SaveChanges
            await _dbContext.SaveChangesAsync();
            return MapToDto(gi);
        }

        private static GoodsIssueDto MapToDto(GoodsIssue gi)
        {
            return new GoodsIssueDto
            {
                Id = gi.Id,
                Code = gi.Code,
                OutboundOrderId = gi.OutboundOrderId,
                Type = gi.Type,
                WarehouseId = gi.WarehouseId,
                Status = gi.Status,
                CreatedAt = gi.CreateAt,
                UpdatedAt = gi.UpdateAt,
                IssuedAt = gi.IssuedAt,

                Items = gi.Items.Select(item => new GoodsIssueItemDto
                {
                    Id = item.Id,
                    GoodsIssueId = item.GoodsIssueId,
                    ProductId = item.ProductId,
                    OutboundOrderItemId = item.OutboundOrderItemId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity,
                    IssuedQty = item.Issued_Qty,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,

                    Allocations = item.Allocations.Select(a => new GoodsIssueAllocateDto
                    {
                        Id = a.Id,
                        GoodsIssueItemId = a.GoodsIssueItemId,
                        LocationId = a.LocationId ?? Guid.Empty,
                        AllocatedQty = a.AllocatedQty,
                        PickedQty = a.PickedQty,
                        Status = a.Status
                    }).ToList()

                }).ToList()
            };
        }

        public async Task<OutboundOrderDto> ApproveOutboundOrderAsync(Guid orderId)
        {
            var entity = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (entity == null)
                throw new Exception("OutboundOrder not found");
            if (entity.Status != OutboundStatus.Pending)
                throw new Exception("Only Pending orders can be approved");

            entity.Status = OutboundStatus.Approve;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ApproveBy = jwtService.GetUserId();
            entity.ApprovedAt = DateTime.UtcNow;

            foreach (var item in entity.Items.Where(x => x.Status == OutboundStatus.Pending))
            {
                item.Status = OutboundStatus.Approve;
                item.UpdatedAt = DateTime.UtcNow;
            }

            var GroupWarehouse = entity.Items
                .Where(x => x.Status == OutboundStatus.Approve)
                .GroupBy(x => x.WarehouseId);

            foreach (var group in GroupWarehouse)
            {
                var warehouseId = group.Key;

                var gi = new GoodsIssue
                {
                    Id = Guid.NewGuid(),
                    OutboundOrderId = entity.Id,
                    Code = GenerateGICode(),
                    Type = GIType.Outbound,
                    WarehouseId = warehouseId,
                    Status = GIStatus.Pending,
                    CreateAt = DateTime.UtcNow,
                    Items = new List<GoodsIssueItem>()
                };

                foreach (var item in group)
                {
                    var gii = new GoodsIssueItem
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueId = gi.Id,
                        OutboundOrderItemId = item.Id,
                        ProductId = item.ProductId,
                        Status = GIStatus.Pending,
                        Quantity = item.Quantity,
                        Issued_Qty = item.Issued_Qty,
                        CreatedAt = DateTime.UtcNow,
                        Allocations = new List<GoodsIssueAllocate>()
                    };

                    decimal remainingQty = item.Quantity;

                    // ✅ Lấy locations
                    var locations = await _inventoryService.GetAvailableLocations(
                        item.ProductId,
                        warehouseId
                    );

                    // ✅ LỌC BỎ CÁC LÔ ĐÃ HẾT HẠN
                    var validLocations = locations
                        .Where(loc => !loc.ExpiryDate.HasValue || loc.ExpiryDate.Value > DateTime.UtcNow)
                        .ToList();

                    // ✅ KIỂM TRA: CÓ LÔ CÒN HẠN KHÔNG?
                    if (!validLocations.Any())
                    {
                        // Lấy thông tin sản phẩm để hiển thị
                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        var expiredCount = locations.Count(loc =>
                            loc.ExpiryDate.HasValue && loc.ExpiryDate.Value <= DateTime.UtcNow);

                        throw new Exception(
                            $"Không thể phân bổ sản phẩm '{product?.Name ?? item.ProductId.ToString()}'. " +
                            $"Tất cả {locations.Count} lô hàng trong kho đều đã hết hạn sử dụng. " +
                            $"Vui lòng kiểm tra lại tồn kho."
                        );
                    }

                    // ✅ TÍNH TỔNG SỐ LƯỢNG KHẢ DỤNG (chỉ lô còn hạn)
                    decimal totalValidQty = validLocations.Sum(loc => loc.AvailableQty);

                    // ✅ CẢNH BÁO NẾU KHÔNG ĐỦ HÀNG CÒN HẠN
                    if (totalValidQty < item.Quantity)
                    {
                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        var expiredQty = locations
                            .Where(loc => loc.ExpiryDate.HasValue && loc.ExpiryDate.Value <= DateTime.UtcNow)
                            .Sum(loc => loc.AvailableQty);

                        Console.WriteLine($"⚠️ WARNING: Sản phẩm '{product?.Name}' không đủ hàng còn hạn!");
                        Console.WriteLine($"   - Yêu cầu: {item.Quantity}");
                        Console.WriteLine($"   - Có sẵn (còn hạn): {totalValidQty}");
                        Console.WriteLine($"   - Thiếu: {item.Quantity - totalValidQty}");
                        Console.WriteLine($"   - Hàng đã hết hạn: {expiredQty}");
                    }

                    // ✅ FEFO: Sắp xếp theo ExpiryDate
                    var sortedLocations = validLocations
                        .OrderBy(loc => loc.ExpiryDate ?? DateTime.MaxValue)
                        .ThenBy(loc => loc.LotCode)
                        .ToList();

                    Console.WriteLine($"\n=== Allocating {item.Quantity} x Product {item.ProductId} ===");

                    foreach (var loc in sortedLocations)
                    {
                        if (remainingQty <= 0) break;

                        var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                        Console.WriteLine($"✅ Allocate {allocQty} from Location {loc.Code}, " +
                                        $"Lot {loc.LotCode}, " +
                                        $"Expiry: {loc.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A"}");

                        // ⚠️ CẢNH BÁO nếu lô gần hết hạn (< 30 ngày)
                        if (loc.ExpiryDate.HasValue)
                        {
                            var daysUntilExpiry = (loc.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
                            if (daysUntilExpiry < 30)
                            {
                                Console.WriteLine($"  ⚠️ WARNING: Lot expires in {Math.Round(daysUntilExpiry)} days!");
                            }
                        }

                        gii.Allocations.Add(new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = loc.Id,
                            LotId = loc.LotId,
                            AllocatedQty = allocQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        });

                        remainingQty -= allocQty;
                    }

                    // Nếu thiếu hàng (sau khi allocate hết lô còn hạn)
                    if (remainingQty > 0)
                    {
                        Console.WriteLine($"⚠️ Backorder: {remainingQty} units");

                        gii.Allocations.Add(new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = null,
                            LotId = Guid.Empty,
                            AllocatedQty = remainingQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        });
                    }

                    gi.Items.Add(gii);
                }

                _dbContext.Set<GoodsIssue>().Add(gi);
            }

            await _dbContext.SaveChangesAsync();
            return _mapper.Map<OutboundOrderDto>(entity);
        }

        public async Task Picking(GoodsIssueItemDto dto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    Console.WriteLine($"=== START PICKING - DTO ProductId: {dto.ProductId} ===");

                    var gii = await _dbContext.GoodsIssueItems
                        .Include(x => x.Allocations)
                        .FirstOrDefaultAsync(s => s.Id == dto.Id);
                    if (gii == null)
                        throw new Exception("gii null");

                    var gi = await _dbContext.GoodsIssues
                        .Include(s => s.Warehouse)
                        .FirstOrDefaultAsync(s => s.Id == dto.GoodsIssueId);
                    if (gi == null)
                        throw new Exception("gi null");

                    var issuedLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                    if (issuedLocation == null)
                        throw new Exception("location null");

                    Console.WriteLine($"IssuedLocation: {issuedLocation.Id}");

                    var allocateIds = dto.Allocations.Select(x => x.Id).ToList();
                    Console.WriteLine($"Looking for allocations: {string.Join(", ", allocateIds)}");

                    var allAllocates = await _dbContext.GoodsIssueAllocates
                        .Where(a => allocateIds.Contains(a.Id))
                        .ToListAsync();

                    Console.WriteLine($"Found {allAllocates.Count} allocations");

                    foreach (var itemDto in dto.Allocations)
                    {
                        Console.WriteLine($"\n--- Processing allocation {itemDto.Id} ---");

                        var gia = allAllocates.FirstOrDefault(a => a.Id == itemDto.Id);
                        if (gia == null)
                        {
                            Console.WriteLine($"❌ Allocation {itemDto.Id} not found!");
                            continue;
                        }

                        Console.WriteLine($"Current: PickedQty={gia.PickedQty}, Status={gia.Status}, LotId={gia.LotId}");
                        Console.WriteLine($"New: PickedQty={itemDto.PickedQty}");

                        decimal actualPicked = itemDto.PickedQty;

                        // Check before update
                        var stateBefore = _dbContext.Entry(gia).State;
                        Console.WriteLine($"Entity state BEFORE update: {stateBefore}");

                        // ✅ Update allocation
                        gia.PickedQty = actualPicked;
                        gia.Status = GIAStatus.Picked;

                        var stateAfter = _dbContext.Entry(gia).State;
                        Console.WriteLine($"Entity state AFTER update: {stateAfter}");

                        // Check if LotId is valid
                        if (gia.LotId == Guid.Empty || gia.LotId == null)
                        {
                            Console.WriteLine($"⚠️ WARNING: LotId is empty/null!");
                        }

                        Console.WriteLine($"Calling AdjustPickingAsync - LocationId: {gia.LocationId}, LotId: {gia.LotId}");

                        // ✅ Adjust inventory (chỉ modify entities, chưa save)
                        await _inventoryService.AdjustPickingAsync(
                            gi.WarehouseId,
                            gia.LocationId.Value,
                            dto.ProductId,
                            actualPicked,
                            InventoryActionType.AdjustDecrease,
                            gi.Code,
                            gia.LotId
                        );

                        Console.WriteLine($"Calling AdjustAsync - IssuedLocationId: {issuedLocation.Id}, LotId: {gia.LotId}");

                        await _inventoryService.AdjustAsync(
                            gi.WarehouseId,
                            issuedLocation.Id,
                            dto.ProductId,
                            actualPicked,
                            InventoryActionType.AdjustIncrease,
                            gia.LotId
                        );

                        // Handle shortage
                        if (actualPicked < gia.AllocatedQty)
                        {
                            Console.WriteLine($"Short pick: {actualPicked} < {gia.AllocatedQty}");
                            decimal remainingQty = gia.AllocatedQty - actualPicked;

                            var availableLocs = await _inventoryService
                                .GetAvailableLocationsByLot(
                                    dto.ProductId,
                                    gi.WarehouseId,
                                    gia.LotId);

                            Console.WriteLine($"Found {availableLocs.Count()} available locations for reallocation");

                            foreach (var loc in availableLocs.Where(l => l.Id != gia.LocationId))
                            {
                                if (remainingQty <= 0) break;

                                decimal allocQty = Math.Min(remainingQty, loc.AvailableQty);

                                await _dbContext.GoodsIssueAllocates.AddAsync(new GoodsIssueAllocate
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsIssueItemId = gii.Id,
                                    LocationId = loc.Id,
                                    LotId = gia.LotId,
                                    AllocatedQty = allocQty,
                                    Status = GIAStatus.Planned
                                });

                                Console.WriteLine($"Added reallocation: {allocQty} from {loc.Id}");
                                remainingQty -= allocQty;
                            }

                            if (remainingQty > 0)
                            {
                                await _dbContext.GoodsIssueAllocates.AddAsync(new GoodsIssueAllocate
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsIssueItemId = gii.Id,
                                    LocationId = null,
                                    LotId = gia.LotId,
                                    AllocatedQty = remainingQty,
                                    Status = GIAStatus.Planned
                                });
                                Console.WriteLine($"Added backorder: {remainingQty}");
                            }
                        }
                    }

                    // ✅ Debug ChangeTracker
                    Console.WriteLine("\n=== CHANGE TRACKER BEFORE SAVE ===");
                    var trackedChanges = _dbContext.ChangeTracker.Entries()
                        .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
                        .ToList();

                    Console.WriteLine($"Total tracked changes: {trackedChanges.Count}");

                    foreach (var entry in trackedChanges)
                    {
                        Console.WriteLine($"- {entry.Entity.GetType().Name}: {entry.State}");

                        if (entry.State == EntityState.Modified)
                        {
                            var props = entry.Properties
                                .Where(p => p.IsModified)
                                .Select(p => $"{p.Metadata.Name}: {p.OriginalValue} → {p.CurrentValue}");
                            Console.WriteLine($"  Modified: {string.Join(", ", props)}");
                        }
                    }

                    // ✅ SAVE TẤT CẢ: allocations + inventories
                    var result = await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"\n✅ SaveChanges result: {result} rows affected");

                    if (result == 0)
                    {
                        Console.WriteLine("⚠️ WARNING: No rows were saved!");
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine("✅ Transaction committed");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.WriteLine($"Stack: {ex.StackTrace}");
                    throw;
                }
            });
        }
        public async Task OutgoingStockCount(IssueGoodsDto dto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 1. Lấy dữ liệu Item và kiểm tra cơ bản
                    var gii = await _dbContext.GoodsIssueItems
                        .FirstOrDefaultAsync(x => x.Id == dto.GoodsIssueItemId);

                    if (gii == null) throw new Exception("Không tìm thấy dòng hàng xuất kho.");
                    if (dto.IssuedQty <= 0) throw new Exception("Số lượng xuất phải lớn hơn 0.");

                    // ✅ CAST RÕ RÀNG
                    int issuedQtyInt = dto.IssuedQty;

                    // Kiểm tra tổng số lượng đã xuất so với yêu cầu
                    if (gii.Issued_Qty + issuedQtyInt > gii.Quantity)
                        throw new Exception("Tổng số lượng xuất vượt quá số lượng yêu cầu trên phiếu.");

                    // 2. Lấy danh sách các lô hàng đã được Pick (đang nằm ở cổng xuất)
                    var pickedAllocates = await _dbContext.GoodsIssueAllocates
                        .Where(x => x.GoodsIssueItemId == gii.Id && x.Status == GIAStatus.Picked)
                        .OrderBy(x => x.LotId)
                        .ToListAsync();

                    // Tính số lượng thực tế đang nằm chờ ở cổng xuất
                    decimal totalCurrentlyAtGate = pickedAllocates.Sum(x => x.PickedQty) - gii.Issued_Qty;

                    if (dto.IssuedQty > totalCurrentlyAtGate)
                        throw new Exception("Số lượng xuất vượt quá số lượng hàng đang có sẵn tại cổng xuất.");

                    // 3. Lấy thông tin phiếu xuất và vị trí xuất
                    var gi = await _dbContext.GoodsIssues
                        .FirstOrDefaultAsync(x => x.Id == gii.GoodsIssueId);

                    var issueLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                    if (issueLocation == null)
                        throw new Exception("Kho chưa cấu hình vị trí xuất hàng (Issue Location).");

                    // 4. TRỪ TỒN KHO TẠI CỔNG XUẤT THEO TỪNG LÔ
                    decimal qtyRemainingToIssue = issuedQtyInt;

                    foreach (var alloc in pickedAllocates)
                    {
                        if (qtyRemainingToIssue <= 0) break;

                        decimal availableInThisAlloc = alloc.PickedQty;
                        decimal takeFromThisLot = Math.Min(qtyRemainingToIssue, availableInThisAlloc);

                        if (takeFromThisLot <= 0) continue;

                        await _inventoryService.AdjustAsync(
                            gi.WarehouseId,
                            issueLocation.Id,
                            gii.ProductId,
                            takeFromThisLot,
                            InventoryActionType.Issue,
                            alloc.LotId,
                            gi.Code
                        );

                        qtyRemainingToIssue -= takeFromThisLot;
                    }

                    // 5. ✅ CẬP NHẬT SỐ LƯỢNG VÀ CHECK STATUS
                    gii.Issued_Qty += issuedQtyInt;

                    // ✅ LOG ĐỂ DEBUG
                    Console.WriteLine($"GoodsIssueItem {gii.Id}:");
                    Console.WriteLine($"  Issued_Qty: {gii.Issued_Qty}");
                    Console.WriteLine($"  Quantity: {gii.Quantity}");
                    Console.WriteLine($"  Is Complete: {gii.Issued_Qty >= gii.Quantity}");

                    gii.Status = (gii.Issued_Qty >= gii.Quantity)
                        ? GIStatus.Complete
                        : GIStatus.Partically_Issued;

                    Console.WriteLine($"  New Status: {gii.Status}");

                    // 6. Cập nhật trạng thái của GoodsIssue
                    var allGiiOfThisGi = await _dbContext.GoodsIssueItems
                        .Where(x => x.GoodsIssueId == gi.Id)
                        .ToListAsync();

                    // ✅ CHECK TẤT CẢ ITEMS
                    var isGiComplete = allGiiOfThisGi.All(x => x.Status == GIStatus.Complete);

                    Console.WriteLine($"GoodsIssue {gi.Id}:");
                    Console.WriteLine($"  Total Items: {allGiiOfThisGi.Count}");
                    Console.WriteLine($"  Complete Items: {allGiiOfThisGi.Count(x => x.Status == GIStatus.Complete)}");
                    Console.WriteLine($"  Is Complete: {isGiComplete}");

                    gi.Status = isGiComplete ? GIStatus.Complete : GIStatus.Partically_Issued;
                    gi.IssuedAt = DateTime.UtcNow;

                    Console.WriteLine($"  New Status: {gi.Status}");

                    // 7. CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (Nếu là đơn xuất kho)
                    if (gi.Type == GIType.Outbound && gii.OutboundOrderItemId.HasValue)
                    {
                        var item = await _dbContext.OutboundOrderItems
                            .FirstOrDefaultAsync(s => s.Id == gii.OutboundOrderItemId);

                        if (item != null)
                        {
                            item.Issued_Qty += issuedQtyInt;

                            item.Status = (item.Issued_Qty >= item.Quantity)
                                ? OutboundStatus.Complete
                                : OutboundStatus.Partically_Issued;

                            // ✅ CẬP NHẬT OUTBOUND ORDER
                            var order = await _dbContext.OutboundOrders
                                .FirstOrDefaultAsync(s => s.Id == item.OutboundOrderId);

                            if (order != null)
                            {
                                var allItemsOfThisOrder = await _dbContext.OutboundOrderItems
                                    .Where(s => s.OutboundOrderId == order.Id)
                                    .ToListAsync();

                                var isOrderComplete = allItemsOfThisOrder.All(s => s.Status == OutboundStatus.Complete);

                                order.Status = isOrderComplete ? OutboundStatus.Complete : OutboundStatus.Partically_Issued;
                            }
                        }
                    }


                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync();

                    Console.WriteLine("✅ Transaction committed");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    throw;
                }
            });
        }
        public async Task<GoodsIssue> CreateGIAsync(GoodsIssueDto dto)
        {
            var warehousecheck = await _dbContext.Warehouses.FirstOrDefaultAsync(s => s.Id == dto.WarehouseId);
            if (warehousecheck.WarehouseType != WarehouseType.RawMaterial)
                throw new Exception("Không thể xuất kho không thuộc loại vật liệu");
            var GI = new GoodsIssue
            {
                Id = dto.Id,
                Code = GenerateGICode(),
                OutboundOrderId = dto.OutboundOrderId,
                Type = GIType.Production,
                Status = (GIStatus)dto.Status,
                CreateAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Issued_Qty = 0,
                    GoodsIssueId= dto.Id,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                }).ToList()
            };
            _dbContext.GoodsIssues.Add(GI);
            await _dbContext.SaveChangesAsync();

            return GI;
        }

       

        private string GenerateGICode()
        {
            // Lấy ngày hôm nay
            var today = DateTime.UtcNow.Date; // chỉ YYYY-MM-DD

            // Đếm số GR đã tạo trong ngày hôm nay
            var countToday = _dbContext.GoodsIssues
                                .Count(gr => gr.CreateAt >= today && gr.CreateAt < today.AddDays(1));

            // Tăng số thứ tự 1
            var seq = countToday + 1;

            // Format code: GR-YYYYMMDD-XXXX
            var code = $"GI-{today:yyyyMMdd}-{seq:0000}";

            return code;
        }
        private string GenerateOutboundOrderCode()
        {
            var today = DateTime.UtcNow.Date;
            var countToday = _dbContext.OutboundOrders
                                .Count(gr => gr.CreatedAt >= today && gr.CreatedAt < today.AddDays(1));
            var seq = countToday + 1;
            var code = $"ORD-{today:yyyyMMdd}-{seq:0000}";
            return code;
        }

        public async Task UpdateGIStatusAsync(Guid giId, GIStatus status)
        {
            var gi = await _dbContext.GoodsIssues
                .FirstOrDefaultAsync(x => x.Id == giId);

            if (gi == null)
                throw new Exception("GoodsIssue không tồn tại");

            gi.Status = status;
            gi.UpdateAt = DateTime.UtcNow;

            // update item luôn cho đồng bộ
            var items = await _dbContext.GoodsIssueItems
                .Where(x => x.GoodsIssueId == giId)
                .ToListAsync();

            foreach (var item in items)
            {
                item.Status = status;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task<OutboundOrderDto> RejectOutboundOrderAsync(Guid orderId)
        {
            var entity = await _dbContext.OutboundOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (entity == null)
                throw new Exception("OutboundOrder not found");

            if (entity.Status != OutboundStatus.Pending)
                throw new Exception("Only DRAFT orders can be rejected");

            entity.Status = OutboundStatus.Rejected;
            entity.UpdatedAt = DateTime.UtcNow;
            foreach (var item in entity.Items)
            {
                item.Status = OutboundStatus.Rejected;
            }

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<OutboundOrderDto>(entity);
        }


        #endregion
    }
}
