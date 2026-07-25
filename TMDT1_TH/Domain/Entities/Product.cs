using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Domain.Entities;

public class Product : AuditableEntity
{
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public int StoreId { get; set; } =
        StoreDefaults.OfficialStoreId;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ModelNumber { get; set; }
    public string Unit { get; set; } = "Cái";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? ManufacturerName { get; set; }
    public string? ManufacturerAddress { get; set; }
    public int? WarrantyMonths { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public bool IsFeatured { get; set; }
    public bool HasVariants { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public int MinPurchaseQuantity { get; set; } = 1;
    public int? MaxPurchaseQuantity { get; set; }
    public decimal? Weight { get; set; }
    public decimal? PackageLengthCm { get; set; }
    public decimal? PackageWidthCm { get; set; }
    public decimal? PackageHeightCm { get; set; }
    public bool IsDeleted { get; set; }

    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    public ICollection<PriceSchedule> PriceSchedules { get; set; } = new List<PriceSchedule>();
}


