using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ReportsController(
    ApplicationDbContext db) : Controller
{
    private const int MaxReportDays = 366;

    private static readonly TimeZoneInfo VietnamTimeZone =
        ResolveVietnamTimeZone();

    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var range = ResolveDateRange(from, to);
        var previousRange = ResolvePreviousRange(range);

        var createdOrders = await LoadCreatedOrdersAsync(
            range,
            cancellationToken);

        var previousCreatedOrderCount =
            await CountCreatedOrdersAsync(
                previousRange,
                cancellationToken);

        var completedOrders = await LoadCompletedOrdersAsync(
            range,
            cancellationToken);

        var previousRevenue =
            await SumCompletedRevenueAsync(
                previousRange,
                cancellationToken);

        var revenue = completedOrders.Sum(x => x.TotalAmount);
        var completedOrderCount = completedOrders.Count;
        var cancelledOrderCount = createdOrders.Count(x =>
            x.Status == OrderStatus.Cancelled);

        var model = new SalesReportViewModel
        {
            FromDate = range.FromDate,
            ToDate = range.ToDate,
            PeriodDays = range.PeriodDays,
            CurrencyCode = ResolveCurrencyCode(completedOrders),
            Revenue = revenue,
            PreviousRevenue = previousRevenue,
            RevenueChangePercent =
                CalculateChangePercent(
                    revenue,
                    previousRevenue),
            CreatedOrderCount = createdOrders.Count,
            PreviousCreatedOrderCount =
                previousCreatedOrderCount,
            OrderChangePercent =
                CalculateChangePercent(
                    createdOrders.Count,
                    previousCreatedOrderCount),
            CompletedOrderCount = completedOrderCount,
            CancelledOrderCount = cancelledOrderCount,
            CancellationRate = createdOrders.Count == 0
                ? 0
                : Math.Round(
                    cancelledOrderCount * 100m /
                    createdOrders.Count,
                    1),
            AverageOrderValue = completedOrderCount == 0
                ? 0
                : Math.Round(
                    revenue / completedOrderCount,
                    0),
            CompletedCustomerCount =
                CountCompletedCustomers(completedOrders),
            RevenueGroupingLabel =
                range.PeriodDays <= 62
                    ? "Theo ngày"
                    : "Theo tháng",
            RevenuePoints =
                BuildRevenuePoints(
                    completedOrders,
                    range),
            Statuses =
                BuildStatusReport(createdOrders),
            TopProducts =
                BuildTopProducts(completedOrders),
            TopCustomers =
                BuildTopCustomers(completedOrders),
            RecentCompletedOrders =
                completedOrders
                    .OrderByDescending(x => x.CompletedAt)
                    .Take(8)
                    .Select(x =>
                        new RecentCompletedOrderViewModel
                        {
                            Id = x.Id,
                            OrderNumber = x.OrderNumber,
                            CompletedAt =
                                ToVietnamTime(x.CompletedAt),
                            CustomerName = x.CustomerName,
                            TotalQuantity =
                                x.Items.Sum(item =>
                                    item.Quantity),
                            TotalAmount = x.TotalAmount,
                            CurrencyCode = x.CurrencyCode
                        })
                    .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var range = ResolveDateRange(from, to);
        var orders = await LoadCompletedOrdersAsync(
            range,
            cancellationToken);

        var csv = new StringBuilder();

        csv.AppendLine(
            string.Join(
                ",",
                new[]
                {
                    "Mã đơn",
                    "Ngày hoàn thành",
                    "Khách hàng",
                    "Điện thoại",
                    "Email",
                    "Sản phẩm",
                    "Phân loại",
                    "SKU",
                    "Số lượng",
                    "Đơn giá",
                    "Thành tiền dòng",
                    "Phí vận chuyển",
                    "Giảm giá",
                    "Tổng đơn",
                    "Tiền tệ"
                }
                .Select(Csv)));

        foreach (var order in orders
                     .OrderBy(x => x.CompletedAt))
        {
            var completedAt =
                ToVietnamTime(order.CompletedAt);

            foreach (var item in order.Items
                         .OrderBy(x => x.Id))
            {
                csv.AppendLine(
                    string.Join(
                        ",",
                        new[]
                        {
                            order.OrderNumber,
                            completedAt.ToString(
                                "dd/MM/yyyy HH:mm",
                                CultureInfo.InvariantCulture),
                            order.CustomerName,
                            order.CustomerPhone,
                            order.CustomerEmail ?? string.Empty,
                            item.ProductName,
                            item.VariantName ?? string.Empty,
                            item.Sku,
                            item.Quantity.ToString(
                                CultureInfo.InvariantCulture),
                            item.UnitPrice.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture),
                            item.LineTotal.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture),
                            order.ShippingFee.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture),
                            order.DiscountAmount.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture),
                            order.TotalAmount.ToString(
                                "0.##",
                                CultureInfo.InvariantCulture),
                            order.CurrencyCode
                        }
                        .Select(Csv)));
            }
        }

        var utf8WithBom = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: true);

        var bytes = utf8WithBom.GetBytes(
            csv.ToString());

        var fileName =
            $"bao-cao-ban-hang-{range.FromDate:yyyyMMdd}-" +
            $"{range.ToDate:yyyyMMdd}.csv";

        return File(
            bytes,
            "text/csv; charset=utf-8",
            fileName);
    }

    private async Task<List<CreatedOrderRow>>
        LoadCreatedOrdersAsync(
            ReportDateRange range,
            CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt >= range.FromUtc &&
                x.CreatedAt < range.ToUtcExclusive)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CreatedOrderRow(
                x.Id,
                x.Status,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> CountCreatedOrdersAsync(
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.CreatedAt >= range.FromUtc &&
                    x.CreatedAt < range.ToUtcExclusive,
                cancellationToken);
    }

    private async Task<List<CompletedOrderRow>>
        LoadCompletedOrdersAsync(
            ReportDateRange range,
            CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.Status == OrderStatus.Completed &&
                x.CompletedAt.HasValue &&
                x.CompletedAt.Value >= range.FromUtc &&
                x.CompletedAt.Value < range.ToUtcExclusive)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync(cancellationToken);

        return orders
            .Select(x => new CompletedOrderRow(
                x.Id,
                x.OrderNumber,
                x.CustomerUserId,
                x.CustomerName,
                x.CustomerPhone,
                x.CustomerEmail,
                x.CurrencyCode,
                x.CompletedAt!.Value,
                x.ShippingFee,
                x.DiscountAmount,
                x.TotalAmount,
                x.Items
                    .Select(item =>
                        new CompletedOrderItemRow(
                            item.Id,
                            item.ProductId,
                            item.ProductName,
                            item.VariantName,
                            item.Sku,
                            item.Quantity,
                            item.UnitPrice,
                            item.LineTotal))
                    .ToList()))
            .ToList();
    }

    private async Task<decimal> SumCompletedRevenueAsync(
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.Status == OrderStatus.Completed &&
                x.CompletedAt.HasValue &&
                x.CompletedAt.Value >= range.FromUtc &&
                x.CompletedAt.Value < range.ToUtcExclusive)
            .SumAsync(
                x => (decimal?)x.TotalAmount,
                cancellationToken)
            ?? 0;
    }

    private static IReadOnlyList<RevenueReportPointViewModel>
        BuildRevenuePoints(
            IReadOnlyList<CompletedOrderRow> orders,
            ReportDateRange range)
    {
        var useDaily = range.PeriodDays <= 62;

        var buckets = useDaily
            ? BuildDailyBuckets(orders, range)
            : BuildMonthlyBuckets(orders, range);

        var maxRevenue = buckets.Count == 0
            ? 0
            : buckets.Max(x => x.Revenue);

        return buckets
            .Select(x =>
                new RevenueReportPointViewModel
                {
                    Label = x.Label,
                    FullLabel = x.FullLabel,
                    Revenue = x.Revenue,
                    OrderCount = x.OrderCount,
                    HeightPercent = maxRevenue <= 0
                        ? 0
                        : Math.Max(
                            4,
                            Math.Round(
                                x.Revenue * 100m /
                                maxRevenue,
                                1))
                })
            .ToList();
    }

    private static List<RevenueBucket> BuildDailyBuckets(
        IReadOnlyList<CompletedOrderRow> orders,
        ReportDateRange range)
    {
        var values = orders
            .GroupBy(x =>
                ToVietnamTime(x.CompletedAt).Date)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Revenue =
                        group.Sum(x => x.TotalAmount),
                    Count = group.Count()
                });

        var result = new List<RevenueBucket>();

        for (var date = range.FromDate;
             date <= range.ToDate;
             date = date.AddDays(1))
        {
            values.TryGetValue(
                date,
                out var value);

            result.Add(
                new RevenueBucket(
                    date.ToString("dd/MM"),
                    date.ToString("dd/MM/yyyy"),
                    value?.Revenue ?? 0,
                    value?.Count ?? 0));
        }

        return result;
    }

    private static List<RevenueBucket> BuildMonthlyBuckets(
        IReadOnlyList<CompletedOrderRow> orders,
        ReportDateRange range)
    {
        var values = orders
            .GroupBy(x =>
            {
                var local =
                    ToVietnamTime(x.CompletedAt);

                return new YearMonth(
                    local.Year,
                    local.Month);
            })
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Revenue =
                        group.Sum(x => x.TotalAmount),
                    Count = group.Count()
                });

        var firstMonth =
            new DateTime(
                range.FromDate.Year,
                range.FromDate.Month,
                1);

        var lastMonth =
            new DateTime(
                range.ToDate.Year,
                range.ToDate.Month,
                1);

        var result = new List<RevenueBucket>();

        for (var month = firstMonth;
             month <= lastMonth;
             month = month.AddMonths(1))
        {
            var key = new YearMonth(
                month.Year,
                month.Month);

            values.TryGetValue(
                key,
                out var value);

            result.Add(
                new RevenueBucket(
                    month.ToString("MM/yyyy"),
                    $"Tháng {month:MM/yyyy}",
                    value?.Revenue ?? 0,
                    value?.Count ?? 0));
        }

        return result;
    }

    private static IReadOnlyList<OrderStatusReportViewModel>
        BuildStatusReport(
            IReadOnlyList<CreatedOrderRow> orders)
    {
        var total = orders.Count;

        return Enum.GetValues<OrderStatus>()
            .Select(status =>
            {
                var count = orders.Count(x =>
                    x.Status == status);

                return new OrderStatusReportViewModel
                {
                    Name = StatusName(status),
                    CssClass = StatusClass(status),
                    Count = count,
                    Percent = total == 0
                        ? 0
                        : Math.Round(
                            count * 100m / total,
                            1)
                };
            })
            .ToList();
    }

    private static IReadOnlyList<TopProductReportViewModel>
        BuildTopProducts(
            IReadOnlyList<CompletedOrderRow> orders)
    {
        var lines = orders
            .SelectMany(order =>
                order.Items.Select(item =>
                    new
                    {
                        OrderId = order.Id,
                        item.ProductId,
                        item.ProductName,
                        item.Sku,
                        item.Quantity,
                        item.LineTotal
                    }))
            .ToList();

        var totalRevenue = lines.Sum(x => x.LineTotal);

        return lines
            .GroupBy(x => new
            {
                x.ProductId,
                x.ProductName
            })
            .Select(group =>
            {
                var skus = group
                    .Select(x => x.Sku)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                var revenue =
                    group.Sum(x => x.LineTotal);

                return new TopProductReportViewModel
                {
                    ProductId = group.Key.ProductId,
                    ProductName =
                        group.Key.ProductName,
                    SkuSummary = skus.Count switch
                    {
                        0 => "Không có SKU",
                        1 => skus[0],
                        _ => $"{skus.Count} SKU"
                    },
                    Quantity =
                        group.Sum(x => x.Quantity),
                    OrderCount =
                        group.Select(x => x.OrderId)
                            .Distinct()
                            .Count(),
                    Revenue = revenue,
                    RevenueSharePercent =
                        totalRevenue == 0
                            ? 0
                            : Math.Round(
                                revenue * 100m /
                                totalRevenue,
                                1)
                };
            })
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<TopCustomerReportViewModel>
        BuildTopCustomers(
            IReadOnlyList<CompletedOrderRow> orders)
    {
        return orders
            .GroupBy(x =>
                BuildCustomerKey(x))
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(x =>
                        x.CompletedAt)
                    .First();

                return new TopCustomerReportViewModel
                {
                    CustomerUserId =
                        latest.CustomerUserId,
                    CustomerName =
                        latest.CustomerName,
                    Contact =
                        !string.IsNullOrWhiteSpace(
                            latest.CustomerEmail)
                            ? latest.CustomerEmail!
                            : latest.CustomerPhone,
                    OrderCount = group.Count(),
                    Quantity = group.Sum(order =>
                        order.Items.Sum(item =>
                            item.Quantity)),
                    Revenue =
                        group.Sum(x =>
                            x.TotalAmount)
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.OrderCount)
            .Take(10)
            .ToList();
    }

    private static int CountCompletedCustomers(
        IReadOnlyList<CompletedOrderRow> orders)
    {
        return orders
            .Select(BuildCustomerKey)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static string BuildCustomerKey(
        CompletedOrderRow order)
    {
        if (order.CustomerUserId.HasValue)
            return $"user:{order.CustomerUserId.Value}";

        if (!string.IsNullOrWhiteSpace(
                order.CustomerEmail))
        {
            return "email:" +
                   order.CustomerEmail
                       .Trim()
                       .ToLowerInvariant();
        }

        return "phone:" +
               order.CustomerPhone
                   .Trim()
                   .ToLowerInvariant();
    }

    private static ReportDateRange ResolveDateRange(
        DateTime? from,
        DateTime? to)
    {
        var today =
            ToVietnamTime(DateTime.UtcNow).Date;

        var fromDate =
            (from ?? today.AddDays(-29)).Date;

        var toDate =
            (to ?? today).Date;

        if (fromDate > toDate)
            (fromDate, toDate) =
                (toDate, fromDate);

        var periodDays =
            (toDate - fromDate).Days + 1;

        if (periodDays > MaxReportDays)
        {
            fromDate =
                toDate.AddDays(
                    -(MaxReportDays - 1));

            periodDays = MaxReportDays;
        }

        return CreateDateRange(
            fromDate,
            toDate,
            periodDays);
    }

    private static ReportDateRange ResolvePreviousRange(
        ReportDateRange current)
    {
        var previousTo =
            current.FromDate.AddDays(-1);

        var previousFrom =
            previousTo.AddDays(
                -(current.PeriodDays - 1));

        return CreateDateRange(
            previousFrom,
            previousTo,
            current.PeriodDays);
    }

    private static ReportDateRange CreateDateRange(
        DateTime fromDate,
        DateTime toDate,
        int periodDays)
    {
        var fromLocal =
            DateTime.SpecifyKind(
                fromDate,
                DateTimeKind.Unspecified);

        var toExclusiveLocal =
            DateTime.SpecifyKind(
                toDate.AddDays(1),
                DateTimeKind.Unspecified);

        return new ReportDateRange(
            fromDate,
            toDate,
            periodDays,
            TimeZoneInfo.ConvertTimeToUtc(
                fromLocal,
                VietnamTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(
                toExclusiveLocal,
                VietnamTimeZone));
    }

    private static decimal? CalculateChangePercent(
        decimal current,
        decimal previous)
    {
        if (previous == 0)
            return current == 0
                ? 0
                : null;

        return Math.Round(
            (current - previous) * 100m /
            previous,
            1);
    }

    private static decimal? CalculateChangePercent(
        int current,
        int previous)
    {
        return CalculateChangePercent(
            (decimal)current,
            previous);
    }

    private static string ResolveCurrencyCode(
        IReadOnlyList<CompletedOrderRow> orders)
    {
        return orders
            .Select(x => x.CurrencyCode)
            .FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x))
            ?? "VND";
    }

    private static DateTime ToVietnamTime(
        DateTime utcValue)
    {
        var normalized =
            utcValue.Kind == DateTimeKind.Utc
                ? utcValue
                : DateTime.SpecifyKind(
                    utcValue,
                    DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(
            normalized,
            VietnamTimeZone);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[]
                 {
                     "SE Asia Standard Time",
                     "Asia/Ho_Chi_Minh"
                 })
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "UTC+07",
            TimeSpan.FromHours(7),
            "UTC+07",
            "UTC+07");
    }

    private static string StatusName(
        OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending =>
                "Chờ xác nhận",
            OrderStatus.Confirmed =>
                "Đã xác nhận",
            OrderStatus.Processing =>
                "Đang chuẩn bị",
            OrderStatus.Shipping =>
                "Đang giao",
            OrderStatus.Completed =>
                "Hoàn thành",
            OrderStatus.Cancelled =>
                "Đã hủy",
            _ => status.ToString()
        };

    private static string StatusClass(
        OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending =>
                "is-pending",
            OrderStatus.Confirmed =>
                "is-confirmed",
            OrderStatus.Processing =>
                "is-processing",
            OrderStatus.Shipping =>
                "is-shipping",
            OrderStatus.Completed =>
                "is-completed",
            OrderStatus.Cancelled =>
                "is-cancelled",
            _ => string.Empty
        };

    private static string Csv(string? value)
    {
        var normalized = value ?? string.Empty;

        return "\"" +
               normalized.Replace(
                   "\"",
                   "\"\"") +
               "\"";
    }

    private sealed record ReportDateRange(
        DateTime FromDate,
        DateTime ToDate,
        int PeriodDays,
        DateTime FromUtc,
        DateTime ToUtcExclusive);

    private sealed record CreatedOrderRow(
        int Id,
        OrderStatus Status,
        DateTime CreatedAt);

    private sealed record CompletedOrderRow(
        int Id,
        string OrderNumber,
        int? CustomerUserId,
        string CustomerName,
        string CustomerPhone,
        string? CustomerEmail,
        string CurrencyCode,
        DateTime CompletedAt,
        decimal ShippingFee,
        decimal DiscountAmount,
        decimal TotalAmount,
        IReadOnlyList<CompletedOrderItemRow> Items);

    private sealed record CompletedOrderItemRow(
        int Id,
        int? ProductId,
        string ProductName,
        string? VariantName,
        string Sku,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    private sealed record RevenueBucket(
        string Label,
        string FullLabel,
        decimal Revenue,
        int OrderCount);

    private readonly record struct YearMonth(
        int Year,
        int Month);
}
