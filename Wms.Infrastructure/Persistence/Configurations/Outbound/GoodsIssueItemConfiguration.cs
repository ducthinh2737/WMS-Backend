using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Outbound;

namespace Wms.Infrastructure.Persistence.Configurations.Outbound
{
    public class GoodsIssueItemConfiguration : IEntityTypeConfiguration<GoodsIssueItem>
    {
        public void Configure(EntityTypeBuilder<GoodsIssueItem> builder)
        {
            builder.ToTable("GoodsIssueItems");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.GoodsIssue)
                   .WithMany(x => x.Items)
                   .HasForeignKey(x => x.GoodsIssueId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.OutboundOrderItem)
                   .WithMany()
                   .HasForeignKey(x => x.OutboundOrderItemId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Location)
                   .WithMany()
                   .HasForeignKey(x => x.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

