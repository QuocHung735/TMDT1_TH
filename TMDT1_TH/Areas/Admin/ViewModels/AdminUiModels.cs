namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class DashboardViewModel
{
    public IReadOnlyList<MetricCard> Metrics { get; init; } = [];
    public IReadOnlyList<RecentProductRow> RecentProducts { get; init; } = [];
    public IReadOnlyList<ActivityRow> Activities { get; init; } = [];
    public IReadOnlyList<PriceAlertRow> PriceAlerts { get; init; } = [];
}

public sealed record MetricCard(string Label, string Value, string Change, string Icon, string Tone, string Caption);
public sealed record RecentProductRow(string Name, string Sku, string Category, string Price, string Stock, string Status, string Initials, string Tone);
public sealed record ActivityRow(string Icon, string Title, string Description, string Time, string Tone);
public sealed record PriceAlertRow(string Product, string Market, string PriceType, string EffectiveDate, string Status);

public sealed class CategoriesViewModel
{
    public IReadOnlyList<CategoryRow> Items { get; init; } = [];
}
public sealed record CategoryRow(int Id, string Name, string Slug, string Parent, int ProductCount, string Status, int Level, string Icon);

public sealed class BrandsViewModel
{
    public IReadOnlyList<BrandRow> Items { get; init; } = [];
}
public sealed record BrandRow(int Id, string Name, string Country, int ProductCount, string Status, string Initials, string Tone);

public sealed class ProductsViewModel
{
    public IReadOnlyList<ProductRow> Items { get; init; } = [];
}
public sealed record ProductRow(int Id, string Name, string Sku, string Category, string Brand, int VariantCount, string Price, int Stock, string Status, string Initials, string Tone);

public sealed class ProductEditorViewModel
{
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEdit => Id.HasValue;
}

public sealed class PricingViewModel
{
    public IReadOnlyList<PriceScheduleRow> Items { get; init; } = [];
    public IReadOnlyList<PriceHistoryRow> History { get; init; } = [];
}
public sealed record PriceScheduleRow(int Id, string Product, string Variant, string Market, string CostPrice, string ListPrice, string SalePrice, string Period, string Status);
public sealed record PriceHistoryRow(string Product, string Variant, string Market, string PriceType, string OldPrice, string NewPrice, string Change, string User, string Time, string Tone);

public sealed class MarketsViewModel
{
    public IReadOnlyList<MarketRow> Items { get; init; } = [];
}
public sealed record MarketRow(int Id, string Code, string Name, string Currency, int PriceCount, string Status, bool IsDefault);
