using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_StockQuantity", "[StockQuantity] >= 0");
            table.HasCheckConstraint("CK_Products_LowStockThreshold", "[LowStockThreshold] >= 0");
            table.HasCheckConstraint("CK_Products_PurchaseQuantity", "[MinPurchaseQuantity] >= 1 AND ([MaxPurchaseQuantity] IS NULL OR [MaxPurchaseQuantity] >= [MinPurchaseQuantity])");
            table.HasCheckConstraint("CK_Products_Weight", "[Weight] IS NULL OR [Weight] >= 0");
            table.HasCheckConstraint("CK_Products_PackageDimensions", "([PackageLengthCm] IS NULL OR [PackageLengthCm] >= 0) AND ([PackageWidthCm] IS NULL OR [PackageWidthCm] >= 0) AND ([PackageHeightCm] IS NULL OR [PackageHeightCm] >= 0)");
            table.HasCheckConstraint("CK_Products_WarrantyMonths", "[WarrantyMonths] IS NULL OR [WarrantyMonths] >= 0");
        });
        builder.ConfigureAudit();

        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(280).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ModelNumber).HasMaxLength(100);
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired().HasDefaultValue("Cái");
        builder.Property(x => x.ShortDescription).HasMaxLength(600);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CountryOfOrigin).HasMaxLength(100);
        builder.Property(x => x.ManufacturerName).HasMaxLength(250);
        builder.Property(x => x.ManufacturerAddress).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Weight).HasPrecision(18, 3);
        builder.Property(x => x.PackageLengthCm).HasPrecision(10, 2);
        builder.Property(x => x.PackageWidthCm).HasPrecision(10, 2);
        builder.Property(x => x.PackageHeightCm).HasPrecision(10, 2);
        builder.Property(x => x.LowStockThreshold).HasDefaultValue(5);
        builder.Property(x => x.MinPurchaseQuantity).HasDefaultValue(1);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Sku)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.BrandId, x.ModelNumber })
            .IsUnique()
            .HasFilter("[ModelNumber] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_Products_BrandId_ModelNumber");

        builder.HasIndex(x => new { x.CategoryId, x.Status });
        builder.HasIndex(x => new { x.BrandId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Brand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
