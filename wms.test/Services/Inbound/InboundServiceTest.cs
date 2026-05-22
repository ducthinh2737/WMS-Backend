using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Inbound;
using Wms.Application.DTOS.Inbound;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Application.Interfaces.Services.MasterData;
using Wms.Domain.Entity.Inbound;
using Wms.Domain.Enums.Inbound;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wms.Tests.Services.Inbound
{
    public class InboundServiceTest : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IWarehouseService> _warehouseServiceMock;
        private readonly Mock<IProductUomService> _productUomServiceMock;
        private readonly InboundService _inboundService;

        public InboundServiceTest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _inventoryServiceMock = new Mock<IInventoryService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _warehouseServiceMock = new Mock<IWarehouseService>();
            _productUomServiceMock = new Mock<IProductUomService>();

            _inboundService = new InboundService(
                _context,
                _inventoryServiceMock.Object,
                _jwtServiceMock.Object,
                _warehouseServiceMock.Object,
                _productUomServiceMock.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task IncomingStockCount_ManufacturingDateGreaterThanExpiryDate_ThrowsBusinessException()
        {
            // Arrange
            var grItemId = Guid.NewGuid();
            _context.Products.Add(new Wms.Domain.Entity.MasterData.Product { Id = 1, Code = "P1", Name = "Product 1", UnitId = 1 });
            _context.GoodsReceiptItems.Add(new GoodsReceiptItem
            {
                Id = grItemId,
                ProductId = 1,
                Quantity = 10,
                Received_Qty = 0,
                UnitId = 1
            });
            await _context.SaveChangesAsync();

            var dto = new GoodsReceiptItem1Dto
            {
                Id = grItemId,
                ProductId = 1,
                Received_Qty = 5,
                ManufacturingDate = DateTime.Today.AddDays(1),
                ExpiryDate = DateTime.Today, // Manufacturing date is AFTER expiry date
                UnitId = 1
            };

            // Act
            Func<Task> act = async () => await _inboundService.IncomingStockCount(dto);

            // Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(act);
            exception.Code.Should().Be("INVALID_DATE");
            exception.Message.Should().Be("Ngày sản xuất không được lớn hơn hạn sử dụng");
        }

        [Fact]
        public async Task CountingReceiptProduction_ManufacturingDateGreaterThanExpiryDate_ThrowsBusinessException()
        {
            // Arrange
            var grId = Guid.NewGuid();
            var productionItemId = Guid.NewGuid();

            var gr = new GoodsReceipt
            {
                Id = grId,
                Code = "GR-PROD-01",
                WarehouseId = Guid.NewGuid(),
                Status = InboundStatus.Approve,
                ReceiptType = ReceiptType.Production,
                Productions = new List<ProductionReceiptItem>
                {
                    new ProductionReceiptItem
                    {
                        Id = productionItemId,
                        ProductId = 1,
                        Quantity = 10,
                        Receipt_Qty = 0,
                        UnitId = 1,
                        Status = GRIStatus.Pending
                    }
                }
            };
            _context.GoodsReceipts.Add(gr);
            await _context.SaveChangesAsync();

            var dto = new GoodsReceiptDto
            {
                Id = grId,
                WarehouseId = gr.WarehouseId,
                ProductionReceiptItems = new List<ProductionReceiptItemDto>
                {
                    new ProductionReceiptItemDto
                    {
                        Id = productionItemId,
                        ProductId = 1,
                        Receipt_Qty = 5,
                        ManufacturingDate = DateTime.Today.AddDays(1),
                        ExpiryDate = DateTime.Today // Manufacturing date is AFTER expiry date
                    }
                }
            };

            // Act
            Func<Task> act = async () => await _inboundService.CountingReceiptProduction(dto);

            // Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(act);
            exception.Code.Should().Be("INVALID_DATE");
            exception.Message.Should().Be("Ngày sản xuất không được lớn hơn hạn sử dụng");
        }
    }
}
