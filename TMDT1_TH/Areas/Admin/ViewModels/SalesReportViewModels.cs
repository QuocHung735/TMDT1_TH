namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class SalesReportViewModel
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int PeriodDays { get; init; }
    public string CurrencyCode { get; init; } = "VND";

    public decimal Revenue { get; init; }
    public decimal PreviousRevenue { get; init; }
    public decimal? RevenueChangePercent { get; init; }

    public int CreatedOrderCount { get; init; }
    public int PreviousCreatedOrderCount { get; init; }
    public decimal? OrderChangePercent { get; init; }

    public int CompletedOrderCount { get; init; }
    public int CancelledOrderCount { get; init; }
    public decimal CancellationRate { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int CompletedCustomerCount { get; init; }

    public string RevenueGroupingLabel { get; init; } = "Theo ngày";

    public IReadOnlyList<RevenueReportPointViewModel> RevenuePoints { get; init; }
        = Array.Empty<RevenueReportPointViewModel>();

    public IReadOnlyList<OrderStatusReportViewModel> Statuses { get; init; }
        = Array.Empty<OrderStatusReportViewModel>();

    public IReadOnlyList<TopProductReportViewModel> TopProducts { get; init; }
        = Array.Empty<TopProductReportViewModel>();

    public IReadOnlyList<TopCustomerReportViewModel> TopCustomers { get; init; }
        = Array.Empty<TopCustomerReportViewModel>();

    public IReadOnlyList<RecentCompletedOrderViewModel> RecentCompletedOrders { get; init; }
        = Array.Empty<RecentCompletedOrderViewModel>();
}

public sealed class RevenueReportPointViewModel
{
    public string Label { get; init; } = string.Empty;
    public string FullLabel { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public decimal HeightPercent { get; init; }
}

public sealed class OrderStatusReportViewModel
{
    public string Name { get; init; } = string.Empty;
    public string CssClass { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Percent { get; init; }
}

public sealed class TopProductReportViewModel
{
    public int? ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string SkuSummary { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal RevenueSharePercent { get; init; }
}

public sealed class TopCustomerReportViewModel
{
    public int? CustomerUserId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Contact { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public int Quantity { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class RecentCompletedOrderViewModel
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "VND";
}
