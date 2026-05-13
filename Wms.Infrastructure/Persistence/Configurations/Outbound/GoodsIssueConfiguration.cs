using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Outbound;

namespace Wms.Infrastructure.Persistence.Configurations.Outbound
{
    public class GoodsIssueConfiguration : IEntityTypeConfiguration<GoodsIssue>
    {
        public void Configure(EntityTypeBuilder<GoodsIssue> builder)
        {
            builder.ToTable("GoodsIssues");
            builder.HasKey(x => x.Id);

            // Code
            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);
            builder.HasIndex(x => x.Code).IsUnique();

            // Quan hệ với OutboundOrder
            builder.HasOne(x => x.OutboundOrder)
                   .WithMany(x => x.GoodsIssues)
                   .HasForeignKey(x => x.OutboundOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ với Warehouse
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ với GoodsIssueItem
            builder.HasMany(x => x.Items)
                   .WithOne(x => x.GoodsIssue)
                   .HasForeignKey(x => x.GoodsIssueId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

