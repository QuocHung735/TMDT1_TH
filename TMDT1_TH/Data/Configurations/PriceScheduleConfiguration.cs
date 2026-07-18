using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class PriceScheduleConfiguration : IEntityTypeConfiguration<PriceSchedule>
{
    public void Configure(EntityTypeBuilder<PriceSchedule> builder)
    {
        builder.ToTable("PriceSchedules", table =>
        {
            // Metadata này giúp EF Core tương thích với bảng có trigger SQL Server.
            table.HasTrigger("TRG_PriceSchedules_Validate");
            table.HasTrigger("TRG_PriceSchedules_History");
            table.HasCheckConstraint(
                "CK_PriceSchedules_Target",
                "([ProductId] IS NOT NULL AND [ProductVariantId] IS NULL) OR ([ProductId] IS NULL AND [ProductVariantId] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_PriceSchedules_Prices",
                "[CostPrice] >= 0 AND [ListPrice] > 0 AND [SalePrice] > 0 AND [SalePrice] <= [ListPrice]");
            table.HasCheckConstraint(
                "CK_PriceSchedules_Dates",
                "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
        });
        builder.ConfigureAudit();

        builder.Property(x => x.CostPrice).HasPrecision(18, 2);
        builder.Property(x => x.ListPrice).HasPrecision(18, 2);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.ValidFrom).HasColumnType("datetime2");
        builder.Property(x => x.ValidTo).HasColumnType("datetime2");
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProductId, x.MarketId, x.ValidFrom });
        builder.HasIndex(x => new { x.ProductVariantId, x.MarketId, x.ValidFrom });
        builder.HasIndex(x => new { x.MarketId, x.IsActive, x.ValidFrom, x.ValidTo });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.PriceSchedules)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.PriceSchedules)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Market)
            .WithMany(x => x.PriceSchedules)
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
