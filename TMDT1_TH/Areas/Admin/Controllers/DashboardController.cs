using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;
using TMDT1_TH.Infrastructure.Time;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController(ApplicationDbContext dbContext) : Controller
{
    private static readonly string[] Tones = new[] { "purple", "mint", "amber", "blue", "rose" };

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var priceNow = StorePriceClock.Now;
        var nextSevenDays = priceNow.AddDays(7);

        var totalProducts = await dbContext.Products.CountAsync(cancellationToken);
        var activeProducts = await dbContext.Products.CountAsync(x => x.Status == ProductStatus.Active, cancellationToken);
        var activeVariants = await dbContext.ProductVariants.CountAsync(x => x.IsActive, cancellationToken);
        var upcomingPriceCount = await dbContext.PriceSchedules.CountAsync(
            x => x.IsActive && x.ValidFrom > priceNow && x.ValidFrom <= nextSevenDays,
            cancellationToken);

        var stockValues = await dbContext.Products
            .AsNoTracking()
            .Select(x => x.HasVariants
                ? x.Variants.Where(v => v.IsActive).Sum(v => (int?)v.StockQuantity) ?? 0
                : x.StockQuantity)
            .ToListAsync(cancellationToken);

        var inStock = stockValues.Count(x => x > 10);
        var lowStock = stockValues.Count(x => x is > 0 and <= 10);
        var outOfStock = stockValues.Count(x => x <= 0);
        var inStockPercent = stockValues.Count == 0
            ? 0
            : (int)Math.Round(inStock * 100d / stockValues.Count);
        var readyPercent = stockValues.Count == 0
            ? 0
            : (int)Math.Round((inStock + lowStock) * 100d / stockValues.Count);

        var recentData = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(5)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Sku,
                Category = x.Category.Name,
                x.Status,
                x.HasVariants,
                Stock = x.HasVariants
                    ? x.Variants.Where(v => v.IsActive).Sum(v => (int?)v.StockQuantity) ?? 0
                    : x.StockQuantity
            })
            .ToListAsync(cancellationToken);

        var recentIds = recentData.Select(x => x.Id).ToArray();
        var currentPriceData = await dbContext.PriceSchedules
            .AsNoTracking()
            .Where(x => x.IsActive
                && x.ValidFrom <= priceNow
                && (x.ValidTo == null || x.ValidTo > priceNow)
                && ((x.ProductId.HasValue && recentIds.Contains(x.ProductId.Value))
                    || (x.ProductVariant != null && recentIds.Contains(x.ProductVariant.ProductId))))
            .Select(x => new PricePoint(
                x.ProductId ?? x.ProductVariant!.ProductId,
                x.SalePrice,
                x.ValidFrom))
            .ToListAsync(cancellationToken);

        var priceByProduct = currentPriceData
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.ValidFrom).Min(y => y.SalePrice));

        var recentProducts = recentData.Select((x, index) => new RecentProductRow(
            x.Id,
            x.Name,
            x.Sku,
            x.Category,
            priceByProduct.TryGetValue(x.Id, out var price) ? FormatMoney(price) : "Chưa thiết lập",
            x.Stock.ToString("N0"),
            GetProductStatus(x.Status, x.Stock),
            GetInitials(x.Name),
            Tones[index % Tones.Length])).ToList();

        var upcomingSchedules = await dbContext.PriceSchedules
            .AsNoTracking()
            .Where(x => x.IsActive && x.ValidFrom > priceNow && x.ValidFrom <= nextSevenDays)
            .OrderBy(x => x.ValidFrom)
            .Take(5)
            .Select(x => new UpcomingPriceData(
                x.Product != null ? x.Product.Name : x.ProductVariant!.Product.Name,
                x.ProductVariant != null ? x.ProductVariant.Name : null,
                x.Market.Name,
                x.ValidFrom))
            .ToListAsync(cancellationToken);

        var priceAlerts = upcomingSchedules.Select(x => new PriceAlertRow(
            x.ProductName,
            x.MarketName,
            x.VariantName is null ? "Giá sản phẩm" : $"Biến thể {x.VariantName}",
            x.ValidFrom.ToString("dd"),
            $"Thg {x.ValidFrom:MM}",
            "Sắp áp dụng")).ToList();

        var activities = await LoadActivitiesAsync(utcNow, cancellationToken);

        var model = new DashboardViewModel
        {
            Metrics =
            [
                new("Tổng sản phẩm", totalProducts.ToString("N0"), $"{activeProducts:N0} đang bán", "bi-box-seam", "purple", "dữ liệu trực tiếp từ database"),
                new("Biến thể đang bán", activeVariants.ToString("N0"), "Đang hoạt động", "bi-diagram-2", "mint", "các SKU biến thể đã bật"),
                new("Sắp hết hàng", lowStock.ToString("N0"), $"{outOfStock:N0} hết hàng", "bi-exclamation-triangle", "amber", "mức tồn từ 1 đến 10"),
                new("Giá chờ áp dụng", upcomingPriceCount.ToString("N0"), "7 ngày tới", "bi-calendar2-check", "blue", "lịch giá đã được thiết lập")
            ],
            RecentProducts = recentProducts,
            Activities = activities,
            PriceAlerts = priceAlerts,
            LowStockCount = lowStock,
            UpcomingPriceCount = upcomingPriceCount,
            StockHealth = new StockHealthViewModel
            {
                Total = stockValues.Count,
                InStock = inStock,
                InStockPercent = inStockPercent,
                LowStock = lowStock,
                OutOfStock = outOfStock,
                ReadyPercent = readyPercent
            }
        };

        return View(model);
    }

    private async Task<IReadOnlyList<ActivityRow>> LoadActivitiesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(3)
            .Select(x => new ActivityData(
                "bi-box-seam",
                x.UpdatedAt.HasValue ? "Cập nhật sản phẩm" : "Thêm sản phẩm mới",
                x.Name,
                x.UpdatedAt ?? x.CreatedAt,
                "mint"))
            .ToListAsync(cancellationToken);

        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(2)
            .Select(x => new ActivityData(
                "bi-diagram-3",
                x.UpdatedAt.HasValue ? "Cập nhật danh mục" : "Thêm danh mục",
                x.Name,
                x.UpdatedAt ?? x.CreatedAt,
                "blue"))
            .ToListAsync(cancellationToken);

        var brands = await dbContext.Brands
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(2)
            .Select(x => new ActivityData(
                "bi-patch-check",
                x.UpdatedAt.HasValue ? "Cập nhật thương hiệu" : "Thêm thương hiệu",
                x.Name,
                x.UpdatedAt ?? x.CreatedAt,
                "amber"))
            .ToListAsync(cancellationToken);

        return products
            .Concat(categories)
            .Concat(brands)
            .OrderByDescending(x => x.Time)
            .Take(5)
            .Select(x => new ActivityRow(x.Icon, x.Title, x.Description, TimeAgo(x.Time, now), x.Tone))
            .ToList();
    }

    private static string GetProductStatus(ProductStatus status, int stock)
    {
        if (stock <= 0 && status == ProductStatus.Active)
            return "Hết hàng";
        if (stock <= 10 && status == ProductStatus.Active)
            return "Sắp hết";

        return status switch
        {
            ProductStatus.Active => "Đang bán",
            ProductStatus.Draft => "Bản nháp",
            ProductStatus.Inactive => "Tạm ẩn",
            ProductStatus.OutOfStock => "Hết hàng",
            ProductStatus.Discontinued => "Ngừng kinh doanh",
            _ => "Không xác định"
        };
    }

    private static string TimeAgo(DateTime time, DateTime now)
    {
        var difference = now - DateTime.SpecifyKind(time, DateTimeKind.Utc);
        if (difference.TotalMinutes < 1) return "Vừa xong";
        if (difference.TotalHours < 1) return $"{Math.Max(1, (int)difference.TotalMinutes)} phút trước";
        if (difference.TotalDays < 1) return $"{Math.Max(1, (int)difference.TotalHours)} giờ trước";
        if (difference.TotalDays < 7) return $"{Math.Max(1, (int)difference.TotalDays)} ngày trước";
        return VietnamDateTime.Format(time, "dd/MM/yyyy");
    }

    private static string FormatMoney(decimal amount) => $"{amount:N0}đ";

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "SP",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
    }

    private sealed record ActivityData(string Icon, string Title, string Description, DateTime Time, string Tone);
    private sealed record PricePoint(int ProductId, decimal SalePrice, DateTime ValidFrom);
    private sealed record UpcomingPriceData(string ProductName, string? VariantName, string MarketName, DateTime ValidFrom);
}

