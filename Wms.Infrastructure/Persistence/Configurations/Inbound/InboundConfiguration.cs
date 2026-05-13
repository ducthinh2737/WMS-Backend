using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Inbound;

namespace Wms.Infrastructure.Persistence.Configurations.Inbound
{
    public class InboundOrderConfiguration : IEntityTypeConfiguration<InboundOrder>
    {
        public void Configure(EntityTypeBuilder<InboundOrder> builder)
        {
            builder.ToTable("InboundOrders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.SupplierId)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.InboundOrder)
                   .HasForeignKey(x => x.InboundOrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class InboundOrderItemConfiguration : IEntityTypeConfiguration<InboundOrderItem>
    {
        public void Configure(EntityTypeBuilder<InboundOrderItem> builder)
        {
            builder.ToTable("InboundOrderItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .IsRequired();

            builder.Property(x => x.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();
        }
    }

    public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
    {
        public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
        {
            builder.ToTable("GoodsReceipts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);


            builder.Property(x => x.WarehouseId)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.GoodsReceipt)
                   .HasForeignKey(x => x.GoodsReceiptId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.InboundOrder)
                   .WithMany(x => x.GoodsReceipts)
                   .HasForeignKey(x => x.InboundOrderId)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }

    public class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
        {
            builder.ToTable("GoodsReceiptItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .IsRequired();


            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();
        }
    }

    public class ProductionReceiptItemConfiguration : IEntityTypeConfiguration<ProductionReceiptItem>
    {
        public void Configure(EntityTypeBuilder<ProductionReceiptItem> builder)
        {
            builder.ToTable("ProductionReceiptItems");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        }
    }
}

