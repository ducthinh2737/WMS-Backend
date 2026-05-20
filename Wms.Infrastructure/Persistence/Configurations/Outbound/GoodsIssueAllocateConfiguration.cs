using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Outbound;

namespace Wms.Infrastructure.Persistence.Configurations.Outbound
{
    public class GoodsIssueAllocateConfiguration : IEntityTypeConfiguration<GoodsIssueAllocate>
    {
        public void Configure(EntityTypeBuilder<GoodsIssueAllocate> builder)
        {
            builder.Property(x => x.Version)
                   .IsConcurrencyToken();
                   
            builder.Property(x => x.IssuedQty)
                   .HasColumnType("decimal(18,4)")
                   .HasDefaultValue(0m);
        }
    }
}
