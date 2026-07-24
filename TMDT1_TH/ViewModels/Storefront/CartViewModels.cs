namespace TMDT1_TH.ViewModels.Storefront;

public sealed class StoreCartPageViewModel
{
    public IReadOnlyList<StoreCartItemViewModel> Items { get; init; }
        = Array.Empty<StoreCartItemViewModel>();

    public IReadOnlyList<string> Warnings { get; init; }
        = Array.Empty<string>();

    public string CurrencyCode { get; init; } = "VND";
    public int TotalQuantity { get; init; }
    public decimal Subtotal { get; init; }
}

public sealed class StoreCartItemViewModel
{
    public int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string ProductSlug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public decimal SalePrice { get; init; }
    public decimal? ListPrice { get; init; }
    public int Quantity { get; init; }
    public int MinQuantity { get; init; } = 1;
    public int MaxQuantity { get; init; }
    public int StockQuantity { get; init; }
    public decimal LineTotal => SalePrice * Quantity;
}

public sealed class StoreCartAddRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

public sealed class StoreCartUpdateRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

public sealed class StoreCartRemoveRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
}
