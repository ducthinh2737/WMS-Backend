using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.MasterData;
using Wms.Application.DTOs.MasterData.Categories;
using Wms.Application.DTOs.MasterData.Customers;
using Wms.Application.DTOs.MasterData.Products;
using Wms.Domain.Entity.MasterData;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Tests.Services.MasterData
{
    #region CategoryService Tests

    public class CategoryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _categoryService = new CategoryService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidData_CreatesCategory()
        {
            // Arrange
            var dto = new CreateCategoryDto
            {
                Code = "CAT01",
                Name = "Điện tử"
            };

            // Act
            var result = await _categoryService.CreateAsync(dto);

            // Assert
            result.Should().BeGreaterThan(0);
            var category = await _context.Categories.FindAsync(result);
            category.Should().NotBeNull();
            category!.Code.Should().Be("CAT01");
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Categories.Add(new Category { Code = "CAT01", Name = "Category 1" });
            await _context.SaveChangesAsync();

            var dto = new CreateCategoryDto { Code = "CAT01", Name = "Category 2" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _categoryService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Code already exists"));
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesCategory()
        {
            // Arrange
            var category = new Category { Code = "CAT01", Name = "Old Name", IsActive = true };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var dto = new UpdateCategoryDto { Name = "New Name", IsActive = false };

            // Act
            await _categoryService.UpdateAsync(category.Id, dto);

            // Assert
            var updated = await _context.Categories.FindAsync(category.Id);
            updated!.Name.Should().Be("New Name");
            updated.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllCategoriesWithCreatedDate()
        {
            // Arrange
            _context.Categories.AddRange(
                new Category { Code = "CAT01", Name = "Category 1", CreatedAt = DateTime.UtcNow },
                new Category { Code = "CAT02", Name = "Category 2", CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _categoryService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.CreateAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)));
        }
    }

    #endregion

    #region CustomerService Tests

    public class CustomerServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CustomerService _customerService;

        public CustomerServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _customerService = new CustomerService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidData_CreatesCustomerWithContactInfo()
        {
            // Arrange
            var dto = new CreateCustomerDto
            {
                Code = "CUS01",
                Name = "Công ty ABC",
                Email = "abc@company.com",
                Phone = "0123456789",
                Address = "123 Street, City"
            };

            // Act
            var result = await _customerService.CreateAsync(dto);

            // Assert
            var customer = await _context.Customers.FindAsync(result);
            customer.Should().NotBeNull();
            customer!.Email.Should().Be("abc@company.com");
            customer.Phone.Should().Be("0123456789");
            customer.Address.Should().Be("123 Street, City");
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Customers.Add(new Customer { Code = "CUS01", Name = "Customer 1" });
            await _context.SaveChangesAsync();

            var dto = new CreateCustomerDto { Code = "CUS01", Name = "Customer 2" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _customerService.CreateAsync(dto)
            );
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesContactInfo()
        {
            // Arrange
            var customer = new Customer
            {
                Code = "CUS01",
                Name = "Old Name",
                Email = "old@test.com",
                Phone = "111",
                Address = "Old Address"
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var dto = new UpdateCustomerDto
            {
                Name = "New Name",
                Email = "new@test.com",
                Phone = "222",
                Address = "New Address",
                IsActive = false
            };

            // Act
            await _customerService.UpdateAsync(customer.Id, dto);

            // Assert
            var updated = await _context.Customers.FindAsync(customer.Id);
            updated!.Email.Should().Be("new@test.com");
            updated.Phone.Should().Be("222");
        }

        [Fact]
        public async Task DeleteAsync_ValidId_DeletesCustomer()
        {
            // Arrange
            var customer = new Customer { Code = "CUS01", Name = "Test" };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Act
            await _customerService.DeleteAsync(customer.Id);

            // Assert
            var deleted = await _context.Customers.FindAsync(customer.Id);
            deleted.Should().BeNull();
        }
    }

    #endregion

    #region ProductService Tests

    public class ProductServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _productService = new ProductService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 19: Quản lý sản phẩm

        [Fact]
        public async Task CreateAsync_ValidData_CreatesProductWithForeignKeys()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Code = "PROD01",
                Name = "Product 1",
                CategoryId = 1,
                BrandId = 1,
                UnitId = 1,
                SupplierId = 1,
                Type = ProductType.Production,
            };

            // Act
            var result = await _productService.CreateAsync(dto);

            // Assert
            var product = await _context.Products.FindAsync(result);
            product.Should().NotBeNull();
            product!.CategoryId.Should().Be(1);
            product.BrandId.Should().Be(1);
            product.SupplierId.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Products.Add(new Product { Code = "PROD01", Name = "Existing" });
            await _context.SaveChangesAsync();

            var dto = new CreateProductDto { Code = "PROD01", Name = "New" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _productService.CreateAsync(dto)
            );
        }

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesOnlyEditableFieldsAndPreservesImmutableFields()
        {
            // Arrange
            var creationTime = DateTime.UtcNow.AddDays(-1);
            var product = new Product
            {
                Code = "PROD_ORIGINAL",
                Name = "Original Product",
                Description = "Original Desc",
                CategoryId = 1,
                UnitId = 1,
                BrandId = 1,
                SupplierId = 1,
                Type = ProductType.Material,
                IsActive = true,
                CreatedAt = creationTime
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var dto = new UpdateProductDto
            {
                Code = "PROD_CHANGED_BUT_SHOULD_BE_IGNORED",
                Name = "New Name",
                Description = "New Desc",
                CategoryId = 2,
                UnitId = 2,
                BrandId = 2,
                SupplierId = 2,
                Type = ProductType.Production,
                IsActive = false
            };

            // Act
            var result = await _productService.UpdateAsync(product.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(product.Id);
            result.Code.Should().Be("PROD_ORIGINAL"); // Code preserved
            result.Name.Should().Be("New Name");
            result.Description.Should().Be("New Desc");
            result.CategoryId.Should().Be(2);
            result.UnitId.Should().Be(2);
            result.BrandId.Should().Be(2);
            result.SupplierId.Should().Be(2);
            result.Type.Should().Be(ProductType.Material); // Type preserved / immutable
            result.IsActive.Should().BeFalse();
            result.CreateAt.Should().Be(creationTime); // CreatedAt preserved

            var dbEntity = await _context.Products.FindAsync(product.Id);
            dbEntity!.Code.Should().Be("PROD_ORIGINAL");
            dbEntity.CreatedAt.Should().Be(creationTime);
            dbEntity.UpdatedAt.Should().NotBeNull();
            dbEntity.UpdatedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateAsync_IsActiveOmitted_DoesNotDeactivateProduct()
        {
            // Arrange
            var product = new Product
            {
                Code = "PROD01",
                Name = "Product 1",
                IsActive = true
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var dto = new UpdateProductDto
            {
                Code = "PROD01",
                Name = "Updated Product Name",
                IsActive = null // Omitted/null
            };

            // Act
            var result = await _productService.UpdateAsync(product.Id, dto);

            // Assert
            result.IsActive.Should().BeTrue(); // Should preserve existing true state
            var dbEntity = await _context.Products.FindAsync(product.Id);
            dbEntity!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_ProductNotFound_ThrowsException()
        {
            // Arrange
            var dto = new UpdateProductDto
            {
                Name = "Non existent"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _productService.UpdateAsync(999, dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Product not found"));
        }

        [Fact]
        public async Task GetAllBySupplierAsync_ValidSupplierId_ReturnsProducts()
        {
            // Arrange
            _context.Products.AddRange(
                new Product { Code = "P1", Name = "Product 1", SupplierId = 1, Type = ProductType.Production},
                new Product { Code = "P2", Name = "Product 2", SupplierId = 1, Type = ProductType.Production},
                new Product { Code = "P3", Name = "Product 3", SupplierId = 2, Type = ProductType.Production}
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _productService.GetAllBySupplierAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.SupplierId.Should().Be(1));
        }

        [Fact]
        public async Task GetAllBySupplierAsync_NonExistentSupplier_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _productService.GetAllBySupplierAsync(999)
            ).ContinueWith(t =>
            {
                t.Result.Message.Should().Contain("Không tìm thấy sản phẩm của nhà cung cấp");
            });
        }

        [Fact]
        public async Task GetAllByType_ValidType_ReturnsProducts()
        {
            // Arrange
            _context.Products.AddRange(
                new Product { Code = "P1", Name = "Product 1", Type = ProductType.Production },
                new Product { Code = "P2", Name = "Product 2", Type = ProductType.Production },
                new Product { Code = "P3", Name = "Product 3", Type = ProductType.Material }
            );
            await _context.SaveChangesAsync();

            var dto = new ProductTypeDto { Type = ProductType.Production };

            // Act
            var result = await _productService.GetAllByType(dto);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.Type.Should().Be(ProductType.Production));
        }

        [Fact]
        public async Task GetAllByType_NoProductsOfType_ThrowsException()
        {
            // Arrange
            var dto = new ProductTypeDto { Type = (ProductType)99 };    
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _productService.GetAllByType(dto)
            ).ContinueWith(t =>
            {
                t.Result.Message.Should().Contain("Không tìm thấy sản phẩm thuộc loại");
            });
        }

        #endregion

        #region Case 20: Bộ lọc và phân trang

        [Fact]
        public async Task FilterAsync_ByKeyword_SearchesInNameAndCode()
        {
            // Arrange
            _context.Products.AddRange(
                new Product { Code = "IPHONE13", Name = "iPhone 13 Pro", CreatedAt = DateTime.UtcNow },
                new Product { Code = "IPHONE14", Name = "iPhone 14 Pro", CreatedAt = DateTime.UtcNow },
                new Product { Code = "SAMSUNG", Name = "Samsung Galaxy", CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var filter = new ProductFilterDto
            {
                Keyword = "Iphone",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _productService.FilterAsync(filter);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p =>
            {
                // Chuyển logic OR thành một biến bool hoặc dùng FluentAssertions logic
                bool match = p.Name.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                             p.Code.Contains("IPHONE", StringComparison.OrdinalIgnoreCase);

                match.Should().BeTrue($"vì sản phẩm {p.Name} (Code: {p.Code}) phải khớp với từ khóa 'Iphone'");
            });
        }

        [Fact]
        public async Task FilterAsync_ByCategoryId_ReturnsOnlyMatchingProducts()
        {
            // Arrange
            _context.Products.AddRange(
                new Product { Code = "P1", Name = "Product 1", CategoryId = 1, CreatedAt = DateTime.UtcNow },
                new Product { Code = "P2", Name = "Product 2", CategoryId = 1, CreatedAt = DateTime.UtcNow },
                new Product { Code = "P3", Name = "Product 3", CategoryId = 2, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var filter = new ProductFilterDto
            {
                CategoryId = 1,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _productService.FilterAsync(filter);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.CategoryId.Should().Be(1));
        }

        [Fact]
        public async Task FilterAsync_WithPaging_ReturnsCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 15; i++)
            {
                _context.Products.Add(new Product
                {
                    Code = $"P{i}",
                    Name = $"Product {i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await _context.SaveChangesAsync();

            var filter = new ProductFilterDto
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _productService.FilterAsync(filter);

            // Assert
            result.Should().HaveCount(10);
        }

        [Fact]
        public async Task FilterAsync_OrdersByCreatedAtDescending()
        {
            // Arrange
            var now = DateTime.UtcNow;
            _context.Products.AddRange(
                new Product { Code = "P1", Name = "Product 1", CreatedAt = now.AddDays(-2) },
                new Product { Code = "P2", Name = "Product 2", CreatedAt = now.AddDays(-1) },
                new Product { Code = "P3", Name = "Product 3", CreatedAt = now }
            );
            await _context.SaveChangesAsync();

            var filter = new ProductFilterDto { Page = 1, PageSize = 10 };

            // Act
            var result = await _productService.FilterAsync(filter);

            // Assert
            result.First().Code.Should().Be("P3"); // Newest first
        }

        #endregion
    }

    #endregion
}