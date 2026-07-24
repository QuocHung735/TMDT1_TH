using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionConfiguration
    : IEntityTypeConfiguration<Promotion>
{
    public void Configure(
        EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions", table =>
        {
            table.HasCheckConstraint(
                "CK_Promotions_DiscountValue",
                "[DiscountValue] > 0");

            table.HasCheckConstraint(
                "CK_Promotions_Percentage",
                "[DiscountType] <> 1 OR " +
                "([DiscountValue] > 0 AND [DiscountValue] <= 100)");

            table.HasCheckConstraint(
                "CK_Promotions_Amounts",
                "[MinimumOrderAmount] >= 0 AND " +
                "([MaximumDiscountAmount] IS NULL OR " +
                "[MaximumDiscountAmount] > 0)");

            table.HasCheckConstraint(
                "CK_Promotions_Usage",
                "[UsedCount] >= 0 AND " +
                "([UsageLimit] IS NULL OR " +
                "([UsageLimit] > 0 AND [UsedCount] <= [UsageLimit]))");

            table.HasCheckConstraint(
                "CK_Promotions_Period",
                "[EndsAt] > [StartsAt]");
        });

        builder.ConfigureAudit();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DiscountType)
            .HasConversion<int>();

        builder.Property(x => x.DiscountValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.MaximumDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.MinimumOrderAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.StartsAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.EndsAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.StartsAt,
            x.EndsAt
        });
    }
}
