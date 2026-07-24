using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity",
                "[Quantity] > 0");
            table.HasCheckConstraint(
                "CK_OrderItems_Prices",
                "[ListPrice] >= 0 AND [UnitPrice] > 0 AND [LineTotal] > 0");
        });

        builder.ConfigureAudit();

        builder.Property(x => x.ProductName)
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(250);
        builder.Property(x => x.Sku)
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(700);
        builder.Property(x => x.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ListPrice).HasPrecision(18, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Product và ProductVariant đều được quản lý bằng soft-delete.
        // Không sử dụng SET NULL vì SQL Server xem đây là cascade action
        // và sẽ từ chối khi phát hiện nhiều đường cascade tới OrderItems.
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
