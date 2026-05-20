using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Outbound;

namespace Wms.Infrastructure.Persistence.Configurations.Outbound
{
    public class OutboundOrderItemConfiguration : IEntityTypeConfiguration<OutboundOrderItem>
    {
        public void Configure(EntityTypeBuilder<OutboundOrderItem> builder)
        {
            builder.ToTable("OutboundOrderItems");
            builder.HasKey(x => x.Id);


            builder.HasOne(x => x.OutboundOrder)
                   .WithMany(x => x.Items)
                   .HasForeignKey(x => x.OutboundOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Version)
                   .IsConcurrencyToken();
        }
    }
}

