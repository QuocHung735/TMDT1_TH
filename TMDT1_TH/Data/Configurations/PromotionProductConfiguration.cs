using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class PromotionProductConfiguration
    : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(
        EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("PromotionProducts");

        builder.HasKey(x => new
        {
            x.PromotionId,
            x.ProductId
        });

        builder.HasOne(x => x.Promotion)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductId);
    }
}
