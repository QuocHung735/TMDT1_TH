using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionRedemptionConfiguration
    : IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(
        EntityTypeBuilder<PromotionRedemption> builder)
    {
        builder.ToTable(
            "PromotionRedemptions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_PromotionRedemptions_Discount",
                    "[DiscountAmount] > 0");

                table.HasCheckConstraint(
                    "CK_PromotionRedemptions_Release",
                    "([IsReleased] = 0 AND [ReleasedAt] IS NULL) OR " +
                    "([IsReleased] = 1 AND [ReleasedAt] IS NOT NULL)");
            });

        builder.ConfigureAudit();

        builder.Property(x => x.PromotionCode)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.PromotionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.RedeemedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ReleasedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ReleaseReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.PromotionId,
            x.IsReleased,
            x.RedeemedAt
        });

        builder.HasIndex(x => x.CustomerUserId);

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Redemptions)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.PromotionRedemption)
            .HasForeignKey<PromotionRedemption>(
                x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
