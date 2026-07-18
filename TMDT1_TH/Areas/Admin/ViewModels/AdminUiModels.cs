namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class DashboardViewModel
{
    public IReadOnlyList<MetricCard> Metrics { get; init; } = [];
    public IReadOnlyList<RecentProductRow> RecentProducts { get; init; } = [];
    public IReadOnlyList<ActivityRow> Activities { get; init; } = [];
    public IReadOnlyList<PriceAlertRow> PriceAlerts { get; init; } = [];
    public StockHealthViewModel StockHealth { get; init; } = new();
    public int LowStockCount { get; init; }
    public int UpcomingPriceCount { get; init; }
}

public sealed record MetricCard(string Label, string Value, string Change, string Icon, string Tone, string Caption);
public sealed record RecentProductRow(int Id, string Name, string Sku, string Category, string Price, string Stock, string Status, string Initials, string Tone);
public sealed record ActivityRow(string Icon, string Title, string Description, string Time, string Tone);
public sealed record PriceAlertRow(string Product, string Market, string PriceType, string Day, string Month, string Status);

public sealed class StockHealthViewModel
{
    public int Total { get; init; }
    public int InStock { get; init; }
    public int InStockPercent { get; init; }
    public int LowStock { get; init; }
    public int OutOfStock { get; init; }
    public int ReadyPercent { get; init; }
}

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
