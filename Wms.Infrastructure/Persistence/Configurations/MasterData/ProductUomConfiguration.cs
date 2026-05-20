using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.MasterData;

namespace Wms.Infrastructure.Persistence.Configurations.MasterData
{
    public class ProductUomConfiguration : IEntityTypeConfiguration<ProductUom>
    {
        public void Configure(EntityTypeBuilder<ProductUom> builder)
        {
            builder.ToTable("ProductUoms");

            builder.HasKey(x => x.Id);

            // Constraint: ProductId + UnitId is unique
            builder.HasIndex(x => new { x.ProductId, x.UnitId })
                   .IsUnique()
                   .HasDatabaseName("IX_ProductUoms_ProductId_UnitId");

            // Index on ProductId for quick lookups
            builder.HasIndex(x => x.ProductId);

            builder.Property(x => x.Factor)
                   .HasColumnType("decimal(18,6)")
                   .IsRequired();

            builder.Property(x => x.IsBaseUnit)
                   .IsRequired();

            builder.HasOne(x => x.Product)
                   .WithMany(p => p.ProductUoms)
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Unit)
                   .WithMany()
                   .HasForeignKey(x => x.UnitId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
