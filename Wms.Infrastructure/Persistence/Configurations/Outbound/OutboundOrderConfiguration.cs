using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Outbound;

namespace Wms.Infrastructure.Persistence.Configurations.Outbound
{
    public class OutboundOrderConfiguration : IEntityTypeConfiguration<OutboundOrder>
    {
        public void Configure(EntityTypeBuilder<OutboundOrder> builder)
        {
            builder.ToTable("OutboundOrders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.Code).IsUnique();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.OutboundOrder)
                   .HasForeignKey(x => x.OutboundOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.GoodsIssues)
                   .WithOne(x => x.OutboundOrder)
                   .HasForeignKey(x => x.OutboundOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

