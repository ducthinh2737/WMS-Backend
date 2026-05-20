using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entity.Auth;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Inbound;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Outbound;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Entity.Transfer;
using Wms.Domain.Entity.StockTakes;

namespace Wms.Infrastructure.Persistence.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // AUTH
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        //// MASTER DATA
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductUom> ProductUoms => Set<ProductUom>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();

        // WAREHOUSE
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<Location> Locations => Set<Location>();

        //// INVENTORY
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<InventoryHistory> InventoryHistories => Set<InventoryHistory>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
        public DbSet<Lot> Lots => Set<Lot>();

        // INBOUND
        public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
        public DbSet<InboundOrderItem> InboundOrderItems => Set<InboundOrderItem>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
        public DbSet<ProductionReceiptItem> ProductionReceiptItems => Set<ProductionReceiptItem>();

        // OUTBOUND
        public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
        public DbSet<OutboundOrderItem> OutboundOrderItems => Set<OutboundOrderItem>();
        public DbSet<GoodsIssue> GoodsIssues => Set<GoodsIssue>();
        public DbSet<GoodsIssueItem> GoodsIssueItems => Set<GoodsIssueItem>();
        public DbSet<GoodsIssueAllocate> GoodsIssueAllocates => Set<GoodsIssueAllocate>();

        // TRANSFER
        public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
        public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();

        // STOCK TAKE
        public DbSet<StockTake> StockTakes => Set<StockTake>();
        public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            UpdateVersionedEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateVersionedEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateVersionedEntities()
        {
            foreach (var entry in ChangeTracker.Entries<Wms.Domain.Entity.IVersionedEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.Version = 1;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.Version++;
                }
            }
        }
    }
}

