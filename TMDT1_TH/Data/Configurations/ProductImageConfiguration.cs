using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.ConfigureAudit();

        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(250);
        builder.HasIndex(x => new { x.ProductId, x.DisplayOrder });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProductVariant)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
