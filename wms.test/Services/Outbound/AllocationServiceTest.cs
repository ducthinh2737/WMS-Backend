using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Outbound;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Entity.Outbound;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wms.Application.DTOS.Warehouse;

namespace Wms.Tests.Services.Outbound
{
    public class AllocationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly AllocationService _allocationService;

        public AllocationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _inventoryServiceMock = new Mock<IInventoryService>();
            _allocationService = new AllocationService(_context, _inventoryServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AllocateInventoryAsync_FiltersOutNonStorageAndNonPickingLocations()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var giItem = new GoodsIssueItem
            {
                Id = Guid.NewGuid(),
                ProductId = 1,
                BaseQuantity = 10
            };
            _context.GoodsIssueItems.Add(giItem);
            
            var product = new Product { Id = 1, Code = "P1", Name = "Product 1" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Set up locations of different types
            var shippingLocationId = Guid.NewGuid();
            var receivingLocationId = Guid.NewGuid();
            var storageLocationId = Guid.NewGuid();
            var pickingLocationId = Guid.NewGuid();

            var availableLocations = new List<LocationQtyDto>
            {
                new()
                {
                    Id = shippingLocationId,
                    WarehouseId = warehouseId,
                    Type = LocationType.Shipping,
                    Code = "SHP-01",
                    AvailableQty = 5
                },
                new()
                {
                    Id = receivingLocationId,
                    WarehouseId = warehouseId,
                    Type = LocationType.Receiving,
                    Code = "RCV-01",
                    AvailableQty = 5
                },
                new()
                {
                    Id = storageLocationId,
                    WarehouseId = warehouseId,
                    Type = LocationType.Storage,
                    Code = "STG-01",
                    AvailableQty = 6
                },
                new()
                {
                    Id = pickingLocationId,
                    WarehouseId = warehouseId,
                    Type = LocationType.Picking,
                    Code = "PCK-01",
                    AvailableQty = 6
                }
            };

            _inventoryServiceMock
                .Setup(x => x.GetAvailableLocations(giItem.ProductId, warehouseId))
                .ReturnsAsync(availableLocations);

            // Act
            await _allocationService.AllocateInventoryAsync(giItem, warehouseId);
            await _context.SaveChangesAsync();

            // Assert
            // Total remainingQty was 10.
            // Storage has 6 available, Picking has 6 available.
            // It should only allocate from Storage and Picking.
            var allocations = await _context.GoodsIssueAllocates
                .Where(x => x.GoodsIssueItemId == giItem.Id)
                .ToListAsync();

            allocations.Should().NotBeEmpty();
            allocations.Should().OnlyContain(x => 
                x.LocationId == storageLocationId || x.LocationId == pickingLocationId);
            
            // Verifying that LockStockAsync was only called for allowed locations
            _inventoryServiceMock.Verify(
                x => x.LockStockAsync(warehouseId, shippingLocationId, It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>()),
                Times.Never);
            _inventoryServiceMock.Verify(
                x => x.LockStockAsync(warehouseId, receivingLocationId, It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>()),
                Times.Never);

            _inventoryServiceMock.Verify(
                x => x.LockStockAsync(warehouseId, It.Is<Guid>(id => id == storageLocationId || id == pickingLocationId), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<Guid?>()),
                Times.AtLeastOnce);
        }
    }
}
