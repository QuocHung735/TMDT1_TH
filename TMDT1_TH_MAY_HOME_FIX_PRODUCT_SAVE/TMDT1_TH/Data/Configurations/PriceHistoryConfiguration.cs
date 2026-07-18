using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");
        builder.ConfigureAudit();

        builder.Property(x => x.OldCostPrice).HasPrecision(18, 2);
        builder.Property(x => x.NewCostPrice).HasPrecision(18, 2);
        builder.Property(x => x.OldListPrice).HasPrecision(18, 2);
        builder.Property(x => x.NewListPrice).HasPrecision(18, 2);
        builder.Property(x => x.OldSalePrice).HasPrecision(18, 2);
        builder.Property(x => x.NewSalePrice).HasPrecision(18, 2);
        builder.Property(x => x.Action).HasConversion<int>();
        builder.Property(x => x.ChangedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ChangedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.Reason).HasMaxLength(1000);

        builder.HasIndex(x => new { x.PriceScheduleId, x.ChangedAt });
        builder.HasIndex(x => new { x.ProductId, x.ChangedAt });
        builder.HasIndex(x => new { x.ProductVariantId, x.ChangedAt });
        builder.HasIndex(x => new { x.MarketId, x.ChangedAt });
    }
}
