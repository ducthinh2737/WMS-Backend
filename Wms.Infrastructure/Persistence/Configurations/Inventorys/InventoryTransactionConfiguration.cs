using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Inventorys;

namespace Wms.Infrastructure.Persistence.Configurations.Inventorys
{
    public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReferenceCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            // ✅ Unique constraint for Idempotency
            builder.HasIndex(x => new { x.ReferenceCode, x.ProductId, x.LotId, x.LocationId, x.ActionType })
                .IsUnique()
                .HasDatabaseName("IX_InventoryTransactions_Idempotency");

            // ✅ Performance indexes
            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_InventoryTransactions_CreatedAt");

            builder.HasIndex(x => x.ProductId)
                .HasDatabaseName("IX_InventoryTransactions_ProductId");
        }
    }
}
