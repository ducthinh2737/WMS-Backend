using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Entity.Inventorys;
public class InventoryHistoryConfiguration : IEntityTypeConfiguration<InventoryHistory>
{
    public void Configure(EntityTypeBuilder<InventoryHistory> builder)
    {
        builder.ToTable("InventoryHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuantityChange)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.ActionType)
            .HasConversion<int>() // Enum -> int
            .IsRequired();

        builder.Property(x => x.ReferenceCode)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.Note)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.LotCode)
            .HasMaxLength(50);

        // Index để query nhanh
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.LocationId);

        // Nếu có bảng liên quan
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
    }
}
