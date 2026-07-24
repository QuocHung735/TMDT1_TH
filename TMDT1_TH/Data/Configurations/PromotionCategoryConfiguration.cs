using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionCategoryConfiguration
    : IEntityTypeConfiguration<PromotionCategory>
{
    public void Configure(
        EntityTypeBuilder<PromotionCategory> builder)
    {
        builder.ToTable("PromotionCategories");

        builder.HasKey(x => new
        {
            x.PromotionId,
            x.CategoryId
        });

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CategoryId);
    }
}
