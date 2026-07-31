namespace TMDT1_TH.ViewModels.Storefront;

public sealed class StoreHomeViewModel
{
    public string MarketName { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "VND";
    public IReadOnlyList<StoreCategoryViewModel> Categories { get; init; }
        = Array.Empty<StoreCategoryViewModel>();
    public IReadOnlyList<StoreProductCardViewModel> FeaturedProducts { get; init; }
        = Array.Empty<StoreProductCardViewModel>();
    public IReadOnlyList<StoreProductCardViewModel> LatestProducts { get; init; }
        = Array.Empty<StoreProductCardViewModel>();
}

public sealed class StoreCatalogViewModel
{
    public string? Query { get; init; }
    public string? CategorySlug { get; init; }
    public string? BrandSlug { get; init; }
    public string Sort { get; init; } = "newest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; } = 1;
    public string CurrencyCode { get; init; } = "VND";
    public IReadOnlyList<StoreProductCardViewModel> Products { get; init; }
        = Array.Empty<StoreProductCardViewModel>();
    public IReadOnlyList<StoreFilterOptionViewModel> Categories { get; init; }
        = Array.Empty<StoreFilterOptionViewModel>();
    public IReadOnlyList<StoreFilterOptionViewModel> Brands { get; init; }
        = Array.Empty<StoreFilterOptionViewModel>();
}

public sealed class StoreProductDetailsViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ModelNumber { get; init; }
    public string Unit { get; init; } = "Cái";
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? CountryOfOrigin { get; init; }
    public string? ManufacturerName { get; init; }
    public string? ManufacturerAddress { get; init; }
    public int? WarrantyMonths { get; init; }
    public decimal? Weight { get; init; }
    public int MinPurchaseQuantity { get; init; } = 1;
    public int? MaxPurchaseQuantity { get; init; }
    public bool HasVariants { get; init; }
    public int StockQuantity { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public decimal? SalePrice { get; init; }
    public decimal? ListPrice { get; init; }
    public IReadOnlyList<StoreImageViewModel> Images { get; init; }
        = Array.Empty<StoreImageViewModel>();
    public IReadOnlyList<StoreOptionViewModel> Options { get; init; }
        = Array.Empty<StoreOptionViewModel>();
    public IReadOnlyList<StoreVariantViewModel> Variants { get; init; }
        = Array.Empty<StoreVariantViewModel>();
    public IReadOnlyList<StoreSpecificationViewModel> Specifications { get; init; }
        = Array.Empty<StoreSpecificationViewModel>();
    public IReadOnlyList<StoreProductCardViewModel> RelatedProducts { get; init; }
        = Array.Empty<StoreProductCardViewModel>();
}

public sealed class StoreCategoryViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int ProductCount { get; init; }
}

public sealed class StoreFilterOptionViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class StoreProductCardViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public decimal? ListPrice { get; init; }
    public int? DiscountPercent { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public int StockQuantity { get; init; }
    public bool IsFeatured { get; init; }
    public bool HasVariants { get; init; }
    public DateTime CreatedAt { get; init; }

    public bool HasPrice => PriceMin.HasValue;
    public bool HasPriceRange =>
        PriceMin.HasValue &&
        PriceMax.HasValue &&
        PriceMin.Value != PriceMax.Value;
}

public sealed class StoreImageViewModel
{
    public string Url { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}

public sealed class StoreOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<StoreOptionValueViewModel> Values { get; init; }
        = Array.Empty<StoreOptionValueViewModel>();
}

public sealed class StoreOptionValueViewModel
{
    public int Id { get; init; }
    public string Value { get; init; } = string.Empty;
    public string? ColorCode { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class StoreVariantViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public decimal? Weight { get; init; }
    public bool IsDefault { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? ListPrice { get; init; }
    public string? ImageUrl { get; init; }
    public IReadOnlyList<int> OptionValueIds { get; init; }
        = Array.Empty<int>();
}

public sealed class StoreSpecificationViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
