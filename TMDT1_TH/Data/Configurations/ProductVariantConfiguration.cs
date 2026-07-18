using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants", table =>
        {
            table.HasCheckConstraint("CK_ProductVariants_StockQuantity", "[StockQuantity] >= 0");
            table.HasCheckConstraint("CK_ProductVariants_Weight", "[Weight] IS NULL OR [Weight] >= 0");
        });
        builder.ConfigureAudit();

        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CombinationKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(18, 3);

        builder.HasIndex(x => new { x.ProductId, x.CombinationKey })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Sku)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
