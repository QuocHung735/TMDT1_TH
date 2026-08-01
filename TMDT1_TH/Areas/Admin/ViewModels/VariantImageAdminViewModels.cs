namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class VariantImagesAdminViewModel
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public IReadOnlyList<VariantImageAdminItem> Items { get; init; } =
        Array.Empty<VariantImageAdminItem>();
}

public sealed record VariantImageAdminItem(
    int Id,
    string Name,
    string Sku,
    int StockQuantity,
    bool IsActive,
    string? ImageUrl);