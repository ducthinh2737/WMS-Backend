using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Entity.StockTakes;

namespace Wms.Infrastructure.Persistence.Configurations.Stocktake
{
    public class StockTakeItemConfiguration : IEntityTypeConfiguration<StockTakeItem>
    {
        public void Configure(EntityTypeBuilder<StockTakeItem> builder)
        {
            builder.ToTable("StockTakeItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SystemQty).HasPrecision(18, 2);
            builder.Property(x => x.CountedQty).HasPrecision(18, 2);

            builder.HasOne(x => x.Location)
                   .WithMany()
                   .HasForeignKey(x => x.LocationId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
