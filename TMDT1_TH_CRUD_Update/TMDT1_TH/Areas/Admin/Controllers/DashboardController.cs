using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var nextWeek = now.AddDays(7);
        var productCount = await db.Products.CountAsync(x => !x.IsDeleted);
        var variantCount = await db.ProductVariants.CountAsync(x => x.IsActive);
        var lowStock = await db.ProductVariants.CountAsync(x => x.IsActive && x.StockQuantity > 0 && x.StockQuantity <= 10);
        var pendingPrices = await db.PriceSchedules.CountAsync(x => x.IsActive && x.ValidFrom > now && x.ValidFrom <= nextWeek);

        var recentProducts = await db.Products.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.Category)
            .Include(x => x.Variants)
            .Include(x => x.PriceSchedules.Where(p => p.IsActive && p.ValidFrom <= now && (p.ValidTo == null || p.ValidTo >= now)))
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentProductRow(
                x.Name, x.Sku, x.Category.Name,
                x.PriceSchedules.OrderByDescending(p => p.ValidFrom).Select(p => p.SalePrice.ToString("N0") + "đ").FirstOrDefault() ?? "Chưa có giá",
                (x.HasVariants ? x.Variants.Sum(v => v.StockQuantity) : x.StockQuantity).ToString(),
                x.Status == ProductStatus.Active ? "Đang bán" : x.Status.ToString(),
                x.Name.Substring(0, Math.Min(2, x.Name.Length)).ToUpper(), "purple"))
            .ToListAsync();

        var alerts = await db.PriceSchedules.AsNoTracking()
            .Where(x => x.IsActive && x.ValidFrom > now && x.ValidFrom <= nextWeek)
            .Include(x => x.Product).Include(x => x.ProductVariant).Include(x => x.Market)
            .OrderBy(x => x.ValidFrom).Take(5)
            .Select(x => new PriceAlertRow(
                x.Product != null ? x.Product.Name : x.ProductVariant!.Product.Name,
                x.Market.Name, "Bộ 3 giá", x.ValidFrom.ToLocalTime().ToString("dd/MM/yyyy"), "Sắp áp dụng"))
            .ToListAsync();

        return View(new DashboardViewModel
        {
            Metrics =
            [
                new("Tổng sản phẩm", productCount.ToString("N0"), "", "bi-box-seam", "purple", "sản phẩm trong hệ thống"),
                new("Biến thể đang bán", variantCount.ToString("N0"), "", "bi-diagram-2", "mint", "biến thể đang hoạt động"),
                new("Sắp hết hàng", lowStock.ToString("N0"), "", "bi-exclamation-triangle", "amber", "tồn kho từ 1 đến 10"),
                new("Giá chờ áp dụng", pendingPrices.ToString("N0"), "", "bi-calendar2-check", "blue", "trong 7 ngày tới")
            ],
            RecentProducts = recentProducts,
            PriceAlerts = alerts,
            Activities = []
        });
    }
}
