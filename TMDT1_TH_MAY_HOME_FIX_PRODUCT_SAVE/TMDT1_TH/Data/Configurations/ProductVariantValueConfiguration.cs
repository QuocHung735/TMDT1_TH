using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class ProductVariantValueConfiguration : IEntityTypeConfiguration<ProductVariantValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantValue> builder)
    {
        builder.ToTable("ProductVariantValues");
        builder.HasKey(x => new { x.ProductVariantId, x.ProductOptionValueId });

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.VariantValues)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ProductOptionValue)
            .WithMany(x => x.VariantValues)
            .HasForeignKey(x => x.ProductOptionValueId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
