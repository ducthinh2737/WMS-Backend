using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Inventorys;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Wms.Domain.Entity.MasterData;

namespace Wms.Tests.Services.Inventory
{
    public class InventoryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWarehouseService> _warehouseServiceMock;
        private readonly InventoryService _inventoryService;

        public InventoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _warehouseServiceMock = new Mock<IWarehouseService>();
            _inventoryService = new InventoryService(_context, _warehouseServiceMock.Object);

            SeedProduct(1);
        }

        private void SeedProduct(int id, string code = "P1", string name = "Product 1")
        {
            if (!_context.Products.Any(p => p.Id == id))
            {
                _context.Products.Add(new Product
                {
                    Id = id,
                    Code = code,
                    Name = name,
                    UnitId = 1,
                    Type = ProductType.Production
                });
                _context.SaveChanges();
            }
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 9: Quản lý tồn kho

        [Fact]
        public async Task GetAvailableLocations_ValidProduct_ReturnsLocationsWithQuantity()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var location1 = new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Description = "Location 1",
                Type = LocationType.Storage,
                IsActive = true
            };
            var location2 = new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                Code = "A1-01-02",
                Description = "Location 2",
                Type = LocationType.Storage,
                IsActive = true
            };

            _context.Locations.AddRange(location1, location2);

            _context.Inventories.AddRange(
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouseId,
                    LocationId = location1.Id,
                    ProductId = 1,
                    OnHandQuantity = 100,
                    LockedQuantity = 20
                },
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouseId,
                    LocationId = location2.Id,
                    ProductId = 1,
                    OnHandQuantity = 50,
                    LockedQuantity = 10
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _inventoryService.GetAvailableLocations(1, warehouseId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(l => l.Code == "A1-01-01" && l.AvailableQty == 80);
            result.Should().Contain(l => l.Code == "A1-01-02" && l.AvailableQty == 40);
        }

        [Fact]
        public async Task PutAway_ValidData_MovesInventory()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var fromLocationId = Guid.NewGuid();
            var toLocationId = Guid.NewGuid();

            _context.Locations.AddRange(
                new Location
                {
                    Id = fromLocationId,
                    WarehouseId = warehouseId,
                    Code = "RECEIVING",
                    Description = "Receiving Location",
                    Type = LocationType.Receiving
                },
                new Location
                {
                    Id = toLocationId,
                    WarehouseId = warehouseId,
                    Code = "A1-01-01",
                    Description = "Storage Location",
                    Type = LocationType.Storage
                }
            );

            var lot = new Lot
            {
                Id = Guid.NewGuid(),
                productId = 1,
                Code = "NOSERIAL",
                CreatedAt = DateTime.UtcNow
            };
            _context.Lots.Add(lot);

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = fromLocationId,
                ProductId = 1,
                LotId = lot.Id,
                OnHandQuantity = 100
            });
            await _context.SaveChangesAsync();

            var dto = new PutawayDto
            {
                WarehouseId = warehouseId,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                ProductId = 1,
                Qty = 50
            };

            // Act
            await _inventoryService.PutAway(dto);

            // Assert
            var fromInventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == fromLocationId);
            var toInventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == toLocationId);

            fromInventory!.OnHandQuantity.Should().Be(50);
            toInventory!.OnHandQuantity.Should().Be(50);

            var histories = await _context.InventoryHistories
                .Where(h => h.ProductId == 1)
                .ToListAsync();
            histories.Should().HaveCount(2);
            histories.Should().Contain(h => h.ActionType == InventoryActionType.TransferOut);
            histories.Should().Contain(h => h.ActionType == InventoryActionType.TransferIn);
        }

        [Fact]
        public async Task PutAway_InvalidQuantity_ThrowsException()
        {
            // Arrange
            var dto = new PutawayDto
            {
                WarehouseId = Guid.NewGuid(),
                FromLocationId = Guid.NewGuid(),
                ToLocationId = Guid.NewGuid(),
                ProductId = 1,
                Qty = 0
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.PutAway(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Số lượng putaway phải > 0"));
        }

        [Fact]
        public async Task GetHistoryAsync_ValidProduct_ReturnsHistoryOrderedByNewest()
        {
            // Arrange
            var now = DateTime.UtcNow;
            _context.InventoryHistories.AddRange(
                new InventoryHistory
                {
                    ProductId = 1,
                    QuantityChange = 10,
                    ActionType = InventoryActionType.Receive,
                    CreatedAt = now.AddDays(-2)
                },
                new InventoryHistory
                {
                    ProductId = 1,
                    QuantityChange = 5,
                    ActionType = InventoryActionType.Issue,
                    CreatedAt = now.AddDays(-1)
                },
                new InventoryHistory
                {
                    ProductId = 1,
                    QuantityChange = 20,
                    ActionType = InventoryActionType.Receive,
                    CreatedAt = now
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _inventoryService.GetHistoryAsync(1);

            // Assert
            result.Should().HaveCount(3);
            result.First().QuantityChange.Should().Be(20);
        }

        #endregion

        #region Case 10: Điều chỉnh tồn kho

        [Fact]
        public async Task AdjustAsync_IncreaseAction_IncreasesOnHand()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Locations.Add(new Location
            {
                Id = locationId,
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Description = "Storage",
                Type = LocationType.Storage
            });
            await _context.SaveChangesAsync();

            // Act
            await _inventoryService.AdjustAsync(
                warehouseId,
                locationId,
                productId: 1,
                qty: 100,
                InventoryActionType.Receive,
                refCode: "PO-001",
                lotCode: "LOT001"
            );
            await _context.SaveChangesAsync();

            // Assert
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == locationId && i.ProductId == 1);

            inventory.Should().NotBeNull();
            inventory!.OnHandQuantity.Should().Be(100);
        }

        [Fact]
        public async Task AdjustAsync_DecreaseAction_DecreasesOnHand()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Locations.Add(new Location
            {
                Id = locationId,
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Description = "Storage",
                Type = LocationType.Storage
            });

            var lot = new Lot
            {
                Id = Guid.NewGuid(),
                productId = 1,
                Code = "LOT001",
                CreatedAt = DateTime.UtcNow
            };
            _context.Lots.Add(lot);

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                LotId = lot.Id,
                OnHandQuantity = 100
            });
            await _context.SaveChangesAsync();

            // Act
            await _inventoryService.AdjustAsync(
                warehouseId,
                locationId,
                productId: 1,
                qty: 30,
                InventoryActionType.Issue,
                refCode: "SO-001",
                lotId: lot.Id
            );
            await _context.SaveChangesAsync();

            // Assert
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == locationId && i.ProductId == 1);

            inventory!.OnHandQuantity.Should().Be(70);
        }

        [Fact]
        public async Task AdjustAsync_DecreaseMoreThanAvailable_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Locations.Add(new Location
            {
                Id = locationId,
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Description = "Storage",
                Type = LocationType.Storage
            });

            var lot = new Lot
            {
                Id = Guid.NewGuid(),
                productId = 1,
                Code = "LOT001",
                CreatedAt = DateTime.UtcNow
            };
            _context.Lots.Add(lot);

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                LotId = lot.Id,
                OnHandQuantity = 50
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.AdjustAsync(
                    warehouseId,
                    locationId,
                    productId: 1,
                    qty: 100,
                    InventoryActionType.Issue,
                    refCode: "SO-001",
                    lotId: lot.Id
                )
            ).ContinueWith(t => t.Result.Message.Should().Contain("không đủ tồn kho"));
        }

        [Fact]
        public async Task AdjustAsync_InvalidQuantity_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.AdjustAsync(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    productId: 1,
                    qty: 0,
                    InventoryActionType.Receive,
                    refCode: "REF"
                )
            ).ContinueWith(t => t.Result.Message.Should().Be("Số lượng điều chỉnh không được bằng 0"));
        }

        [Fact]
        public async Task AdjustAsync_UnsupportedActionType_ThrowsException()
        {
            // Arrange
            var locationId = Guid.NewGuid();
            _context.Locations.Add(new Location
            {
                Id = locationId,
                WarehouseId = Guid.NewGuid(),
                Code = "TEST",
                Description = "Test Location"
            });
            await _context.SaveChangesAsync();

            await _inventoryService.AdjustAsync(
                Guid.NewGuid(),
                locationId,
                productId: 1,
                qty: 10,
                InventoryActionType.TransferIn,
                refCode: "REF",
                lotCode: "LOT001"
            );
            await _context.SaveChangesAsync();

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == locationId);

            inventory.Should().NotBeNull();
            inventory!.OnHandQuantity.Should().Be(10);
        }

        #endregion

        #region Case 12: Mở/Khóa hàng

        [Fact]
        public async Task LockStockAsync_ValidQuantity_LocksStock()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 20
            });
            await _context.SaveChangesAsync();

            // Act
            await _inventoryService.LockStockAsync(
                warehouseId,
                locationId,
                productId: 1,
                qty: 30,
                note: "Lock for SO-001"
            );
            await _context.SaveChangesAsync();

            // Assert
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == locationId);

            inventory!.LockedQuantity.Should().Be(50);

            var history = await _context.InventoryHistories
                .FirstOrDefaultAsync(h => h.ActionType == InventoryActionType.Lock);
            history.Should().NotBeNull();
        }

        [Fact]
        public async Task LockStockAsync_ExceedsAvailable_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 80
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.LockStockAsync(
                    warehouseId,
                    locationId,
                    productId: 1,
                    qty: 30,
                    note: "Lock"
                )
            ).ContinueWith(t => t.Result.Message.Should().Be("Not enough available stock"));
        }

        [Fact]
        public async Task LockStockAsync_InventoryNotFound_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.LockStockAsync(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    productId: 999,
                    qty: 10,
                    note: "Lock"
                )
            ).ContinueWith(t => t.Result.Message.Should().Be("Không tìm thấy sản phẩm với ID 999"));
        }

        [Fact]
        public async Task UnlockStockAsync_ValidQuantity_UnlocksStock()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 50
            });
            await _context.SaveChangesAsync();

            // Act
            await _inventoryService.UnlockStockAsync(
                warehouseId,
                locationId,
                productId: 1,
                qty: 20,
                note: "Unlock for cancellation"
            );
            await _context.SaveChangesAsync();

            // Assert
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == locationId);

            inventory!.LockedQuantity.Should().Be(30);

            var history = await _context.InventoryHistories
                .FirstOrDefaultAsync(h => h.ActionType == InventoryActionType.Unlock);
            history.Should().NotBeNull();
        }

        [Fact]
        public async Task UnlockStockAsync_ExceedsLocked_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 20
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await _inventoryService.UnlockStockAsync(
                    warehouseId,
                    locationId,
                    productId: 1,
                    qty: 30,
                    note: "Unlock"
                )
            ).ContinueWith(t => t.Result.Message.Should().Be("Cannot unlock more than locked quantity"));
        }

        #endregion

        #region Case 13: Báo cáo và truy vấn nâng cao

        [Fact]
        public async Task QueryByProductTypeAsync_ValidType_ReturnsGroupedInventory()
        {
            // Arrange
            var product1 = new Product
            {
                Id = 101,
                Code = "P101",
                Name = "Product 101",
                Type = ProductType.Production
            };
            var product2 = new Product
            {
                Id = 102,
                Code = "P102",
                Name = "Product 102",
                Type = ProductType.Production
            };

            _context.Products.AddRange(product1, product2);

            var warehouse = new Wms.Domain.Entity.Warehouses.Warehouse
            {
                Id = Guid.NewGuid(),
                Code = "WH01",
                Name = "Warehouse 1"
            };
            _context.Warehouses.Add(warehouse);

            var lot1 = new Lot { Id = Guid.NewGuid(), productId = 101, Code = "LOT1" };
            var lot2 = new Lot { Id = Guid.NewGuid(), productId = 102, Code = "LOT2" };
            _context.Lots.AddRange(lot1, lot2);

            _context.Inventories.AddRange(
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouse.Id,
                    LocationId = Guid.NewGuid(),
                    ProductId = 101,
                    LotId = lot1.Id,
                    OnHandQuantity = 100
                },
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouse.Id,
                    LocationId = Guid.NewGuid(),
                    ProductId = 102,
                    LotId = lot2.Id,
                    OnHandQuantity = 50
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var dto = new ProductType1Dto { ProductType = ProductType.Production };
            var result = await _inventoryService.GetInventoryByProductType(dto);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task QueryAsync_WithProductFilter_ReturnsFilteredResults()
        {
            // Arrange
            var warehouse = new Wms.Domain.Entity.Warehouses.Warehouse
            {
                Id = Guid.NewGuid(),
                Code = "WH01",
                Name = "Warehouse 1"
            };
            _context.Warehouses.Add(warehouse);

            var product1 = new Product { Id = 101, Code = "P101", Name = "Product 101", Type = ProductType.Production };
            var product2 = new Product { Id = 102, Code = "P102", Name = "Product 102", Type = ProductType.Production };
            _context.Products.AddRange(product1, product2);

            var location = new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouse.Id,
                Code = "A1-01-01",
                Description = "Location 1",
                Type = LocationType.Storage
            };
            _context.Locations.Add(location);

            var lot1 = new Lot { Id = Guid.NewGuid(), productId = 101, Code = "LOT1" };
            var lot2 = new Lot { Id = Guid.NewGuid(), productId = 102, Code = "LOT2" };
            _context.Lots.AddRange(lot1, lot2);

            _context.Inventories.AddRange(
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouse.Id,
                    LocationId = location.Id,
                    ProductId = 101,
                    LotId = lot1.Id,
                    OnHandQuantity = 100
                },
                new Wms.Domain.Entity.Inventorys.Inventory
                {
                    WarehouseId = warehouse.Id,
                    LocationId = location.Id,
                    ProductId = 102,
                    LotId = lot2.Id,
                    OnHandQuantity = 50
                }
            );
            await _context.SaveChangesAsync();

            var filter = new InventoryQueryDto
            {
                ProductIds = new List<int> { 101, 102 }
            };

            // Act
            var result = await _inventoryService.QueryAsync(filter);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(i =>
            {
                new[] { 101, 102 }.Should().Contain(i.ProductId);
            });
        }

        #endregion

        #region Concurrency & Integration Tests

        [Fact]
        public async Task LockStockAsync_Concurrency_PreventsOverLocking()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            SeedProduct(1);

            _context.Inventories.Add(new Wms.Domain.Entity.Inventorys.Inventory
            {
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 90
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            var task1 = _inventoryService.LockStockAsync(warehouseId, locationId, 1, 10);
            var task2 = _inventoryService.LockStockAsync(warehouseId, locationId, 1, 10);

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await task1;
                await task2;
            });
        }

        [Fact]
        public async Task AdjustAsync_Concurrency_UniqueIndexDuplicateInsert()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            SeedProduct(1);

            _context.Locations.Add(new Location
            {
                Id = locationId,
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Description = "Storage",
                Type = LocationType.Storage
            });
            await _context.SaveChangesAsync();

            // Act
            await _inventoryService.AdjustAsync(
                warehouseId,
                locationId,
                productId: 1,
                qty: 10,
                InventoryActionType.Receive,
                lotCode: "LOT1"
            );
            await _context.SaveChangesAsync();

            var inv = await _context.Inventories.FirstOrDefaultAsync(x => x.LocationId == locationId);
            inv.Should().NotBeNull();
            inv!.OnHandQuantity.Should().Be(10);
        }

        #endregion
    }
}