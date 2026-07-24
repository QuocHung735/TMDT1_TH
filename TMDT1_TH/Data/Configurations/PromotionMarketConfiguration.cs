using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionMarketConfiguration
    : IEntityTypeConfiguration<PromotionMarket>
{
    public void Configure(
        EntityTypeBuilder<PromotionMarket> builder)
    {
        builder.ToTable("PromotionMarkets");

        builder.HasKey(x => new
        {
            x.PromotionId,
            x.MarketId
        });

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Markets)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MarketId);
    }
}
