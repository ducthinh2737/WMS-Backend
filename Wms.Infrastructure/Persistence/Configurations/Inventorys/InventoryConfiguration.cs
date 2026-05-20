using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");

        // Primary key
        builder.HasKey(x => x.Id);

        // Quantities
        builder.Property(x => x.OnHandQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.LockedQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.InTransitQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired()
               .HasDefaultValue(0m);

        // Timestamps
        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
               .HasColumnType("datetime")
               .IsRequired(false);

        builder.Property(x => x.Version)
               .IsConcurrencyToken();

        // Tìm đoạn cấu hình Index cho Inventory và sửa thành:
        builder.HasIndex(i => new { i.WarehouseId, i.LocationId, i.ProductId, i.LotId })
               .IsUnique()
               .HasDatabaseName("IX_Inventories_WarehouseId_LocationId_ProductId_LotId");

        // Index để query nhanh
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => new { x.ProductId, x.LocationId, x.LotId })
               .HasDatabaseName("IX_Inventories_ProductId_LocationId_LotId");

        // Quan hệ
        builder.HasOne<Warehouse>()
               .WithMany()
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
               .WithMany()
               .HasForeignKey(x => x.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
