using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class ShippingServiceConfiguration
    : IEntityTypeConfiguration<ShippingService>
{
    public void Configure(
        EntityTypeBuilder<ShippingService> builder)
    {
        builder.ToTable("ShippingServices", table =>
        {
            table.HasCheckConstraint(
                "CK_ShippingServices_BaseFee",
                "[BaseFee] >= 0");

            table.HasCheckConstraint(
                "CK_ShippingServices_EstimatedDays",
                "[EstimatedMinDays] >= 0 AND " +
                "[EstimatedMaxDays] >= [EstimatedMinDays]");
        });

        builder.ConfigureAudit();

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.BaseFee)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new
        {
            x.ShippingCarrierId,
            x.Code
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.DisplayOrder
        });

        builder.HasOne(x => x.ShippingCarrier)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.ShippingCarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedTime = new DateTime(
            2026,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        builder.HasData(
            new ShippingService
            {
                Id = 1,
                ShippingCarrierId = 1,
                Code = "STANDARD",
                Name = "Giao hàng tiêu chuẩn",
                Description =
                    "Phù hợp với đơn hàng thông thường.",
                BaseFee = 30000,
                EstimatedMinDays = 3,
                EstimatedMaxDays = 5,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = seedTime,
                CreatedBy = "Seed"
            },
            new ShippingService
            {
                Id = 2,
                ShippingCarrierId = 1,
                Code = "EXPRESS",
                Name = "Giao hàng nhanh",
                Description =
                    "Ưu tiên xử lý và giao trong thời gian ngắn.",
                BaseFee = 50000,
                EstimatedMinDays = 1,
                EstimatedMaxDays = 2,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = seedTime,
                CreatedBy = "Seed"
            });
    }
}
