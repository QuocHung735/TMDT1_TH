using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint(
                "CK_Orders_Amounts",
                "[Subtotal] >= 0 AND [ShippingFee] >= 0 AND " +
                "[DiscountAmount] >= 0 AND [TotalAmount] >= 0");

            table.HasCheckConstraint(
                "CK_Orders_TotalAmount",
                "[TotalAmount] = [Subtotal] + [ShippingFee] - " +
                "[DiscountAmount]");
        });

        builder.ConfigureAudit();

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<int>();

        builder.Property(x => x.PaymentStatus)
            .HasConversion<int>();

        builder.Property(x => x.CustomerName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.CustomerPhone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerEmail)
            .HasMaxLength(180);

        builder.Property(x => x.Province)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.District)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Ward)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.AddressLine)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.CustomerNote)
            .HasMaxLength(1000);

        builder.Property(x => x.ShippingCarrierName)
            .HasMaxLength(150);

        builder.Property(x => x.ShippingServiceName)
            .HasMaxLength(150);

        builder.Property(x => x.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(x => x.TrackingUrl)
            .HasMaxLength(700);

        builder.Property(x => x.ShippingNote)
            .HasMaxLength(1000);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Property(x => x.CustomerIp)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.ShippingFee)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.EstimatedDeliveryAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ShippedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.DeliveredAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ConfirmedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.CompletedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.CancelledAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => x.OrderNumber)
            .IsUnique();

        builder.HasIndex(x => x.PublicToken)
            .IsUnique();

        builder.HasIndex(x => x.CustomerUserId);
        builder.HasIndex(x => x.ShippingServiceId);
        builder.HasIndex(x => x.TrackingNumber);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.CustomerPhone, x.CreatedAt });

        builder.HasOne(x => x.CustomerUser)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ShippingService)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ShippingServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
