using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionBrandConfiguration
    : IEntityTypeConfiguration<PromotionBrand>
{
    public void Configure(
        EntityTypeBuilder<PromotionBrand> builder)
    {
        builder.ToTable("PromotionBrands");

        builder.HasKey(x => new
        {
            x.PromotionId,
            x.BrandId
        });

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Brands)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BrandId);
    }
}
