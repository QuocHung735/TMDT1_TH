using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class PromotionsController(
    ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    private static readonly CultureInfo ViCulture =
        CultureInfo.GetCultureInfo("vi-VN");

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? state,
        int? editId)
    {
        PromotionFormViewModel form;
        var openModal = false;

        if (editId.HasValue)
        {
            var promotion = await _db.Promotions
                .AsNoTracking()
                .Include(x => x.Markets)
                .Include(x => x.Products)
                .Include(x => x.Categories)
                .Include(x => x.Brands)
                .FirstOrDefaultAsync(
                    x => x.Id == editId.Value);

            if (promotion is null)
            {
                TempData["Error"] =
                    "Chương trình khuyến mãi không còn tồn tại.";

                return RedirectToAction(nameof(Index));
            }

            form = new PromotionFormViewModel
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Code = promotion.Code,
                Description = promotion.Description,
                DiscountType = promotion.DiscountType,
                ScopeType = promotion.ScopeType,
                DiscountValue = promotion.DiscountValue,
                MaximumDiscountAmount =
                    promotion.MaximumDiscountAmount,
                MinimumOrderAmount =
                    promotion.MinimumOrderAmount,
                UsageLimit = promotion.UsageLimit,
                StartsAt = promotion.StartsAt,
                EndsAt = promotion.EndsAt,
                IsActive = promotion.IsActive,
                MarketIds = promotion.Markets
                    .Select(x => x.MarketId)
                    .ToList(),
                ProductIds = promotion.Products
                    .Select(x => x.ProductId)
                    .ToList(),
                CategoryIds = promotion.Categories
                    .Select(x => x.CategoryId)
                    .ToList(),
                BrandIds = promotion.Brands
                    .Select(x => x.BrandId)
                    .ToList()
            };

            openModal = true;
        }
        else
        {
            form = new PromotionFormViewModel
            {
                Code = await GeneratePromotionCodeAsync(),
                StartsAt = StorePriceClock.Now,
                EndsAt = StorePriceClock.Now.AddDays(7),
                IsActive = true,
                ScopeType = PromotionScopeType.AllProducts
            };
        }

        return View(
            await BuildModelAsync(
                q,
                state,
                form,
                openModal));
    }

    [HttpGet]
    public async Task<IActionResult> Redemptions(
        string? q,
        string? state)
    {
        q = string.IsNullOrWhiteSpace(q)
            ? null
            : q.Trim();

        state = string.IsNullOrWhiteSpace(state)
            ? null
            : state.Trim().ToLowerInvariant();

        var baseQuery =
            _db.PromotionRedemptions
                .AsNoTracking();

        var totalCount =
            await baseQuery.CountAsync();

        var activeCount =
            await baseQuery.CountAsync(x =>
                !x.IsReleased);

        var releasedCount =
            await baseQuery.CountAsync(x =>
                x.IsReleased);

        var totalDiscountAmount =
            await baseQuery.SumAsync(x =>
                (decimal?)x.DiscountAmount)
            ?? 0;

        var releasedDiscountAmount =
            await baseQuery
                .Where(x => x.IsReleased)
                .SumAsync(x =>
                    (decimal?)x.DiscountAmount)
            ?? 0;

        var query =
            _db.PromotionRedemptions
                .AsNoTracking()
                .Include(x => x.Order)
                .AsQueryable();

        if (q is not null)
        {
            query = query.Where(x =>
                x.PromotionCode.Contains(q) ||
                x.PromotionName.Contains(q) ||
                x.Order.OrderNumber.Contains(q) ||
                x.Order.CustomerName.Contains(q) ||
                (x.Order.CustomerEmail != null &&
                 x.Order.CustomerEmail.Contains(q)));
        }

        if (state == "active")
        {
            query = query.Where(x =>
                !x.IsReleased);
        }
        else if (state == "released")
        {
            query = query.Where(x =>
                x.IsReleased);
        }

        var items = await query
            .OrderByDescending(x =>
                x.RedeemedAt)
            .Take(500)
            .Select(x =>
                new PromotionRedemptionListItem
                {
                    Id = x.Id,
                    PromotionCode =
                        x.PromotionCode,
                    PromotionName =
                        x.PromotionName,
                    OrderId =
                        x.OrderId,
                    OrderNumber =
                        x.Order.OrderNumber,
                    CustomerName =
                        x.Order.CustomerName,
                    CustomerEmail =
                        x.Order.CustomerEmail,
                    DiscountAmount =
                        x.DiscountAmount,
                    CurrencyCode =
                        x.Order.CurrencyCode,
                    RedeemedAt =
                        x.RedeemedAt,
                    IsReleased =
                        x.IsReleased,
                    ReleasedAt =
                        x.ReleasedAt,
                    ReleaseReason =
                        x.ReleaseReason
                })
            .ToListAsync();

        return View(
            new PromotionRedemptionHistoryViewModel
            {
                Query = q,
                State = state,
                TotalCount = totalCount,
                ActiveCount = activeCount,
                ReleasedCount = releasedCount,
                TotalDiscountAmount =
                    totalDiscountAmount,
                ReleasedDiscountAmount =
                    releasedDiscountAmount,
                Items = items
            });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        PromotionIndexViewModel page)
    {
        var form = page.Form ?? new PromotionFormViewModel();

        form.Name = form.Name?.Trim() ?? string.Empty;
        form.Description =
            string.IsNullOrWhiteSpace(form.Description)
                ? null
                : form.Description.Trim();

        form.MarketIds = NormalizeIds(form.MarketIds);
        form.ProductIds = NormalizeIds(form.ProductIds);
        form.CategoryIds = NormalizeIds(form.CategoryIds);
        form.BrandIds = NormalizeIds(form.BrandIds);

        if (form.ScopeType != PromotionScopeType.Products)
            form.ProductIds.Clear();

        if (form.ScopeType != PromotionScopeType.Categories)
            form.CategoryIds.Clear();

        if (form.ScopeType != PromotionScopeType.Brands)
            form.BrandIds.Clear();

        if (!form.Id.HasValue)
        {
            var postedCode =
                NormalizeGeneratedCode(form.Code);

            form.Code =
                postedCode is not null &&
                !await _db.Promotions
                    .AsNoTracking()
                    .AnyAsync(x => x.Code == postedCode)
                    ? postedCode
                    : await GeneratePromotionCodeAsync();
        }

        ModelState.Clear();
        TryValidateModel(form, nameof(page.Form));

        await ValidateReferencesAsync(form);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildModelAsync(
                    page.Query,
                    page.State,
                    form,
                    true));
        }

        Promotion promotion;

        if (form.Id.HasValue)
        {
            promotion = await _db.Promotions
                .Include(x => x.Markets)
                .Include(x => x.Products)
                .Include(x => x.Categories)
                .Include(x => x.Brands)
                .FirstOrDefaultAsync(
                    x => x.Id == form.Id.Value)
                ?? throw new InvalidOperationException(
                    "Chương trình khuyến mãi không còn tồn tại.");
        }
        else
        {
            promotion = new Promotion
            {
                Code = form.Code,
                CreatedBy = CurrentUserName()
            };

            _db.Promotions.Add(promotion);
        }

        promotion.Name = form.Name;
        promotion.Description = form.Description;
        promotion.DiscountType = form.DiscountType;
        promotion.ScopeType = form.ScopeType;
        promotion.DiscountValue = form.DiscountValue;
        promotion.MaximumDiscountAmount =
            form.DiscountType ==
                PromotionDiscountType.Percentage
                ? form.MaximumDiscountAmount
                : null;

        promotion.MinimumOrderAmount =
            form.MinimumOrderAmount;

        promotion.UsageLimit = form.UsageLimit;
        promotion.StartsAt = form.StartsAt;
        promotion.EndsAt = form.EndsAt;
        promotion.IsActive = form.IsActive;
        promotion.UpdatedBy = CurrentUserName();

        SyncMarkets(promotion, form.MarketIds);
        SyncProducts(promotion, form.ProductIds);
        SyncCategories(promotion, form.CategoryIds);
        SyncBrands(promotion, form.BrandIds);

        await _db.SaveChangesAsync();

        TempData["Success"] = form.Id.HasValue
            ? "Đã cập nhật chương trình khuyến mãi."
            : $"Đã tạo khuyến mãi với mã {promotion.Code}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion is null)
            return NotFound();

        promotion.IsActive = !promotion.IsActive;
        promotion.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync();

        TempData["Success"] = promotion.IsActive
            ? "Đã kích hoạt chương trình khuyến mãi."
            : "Đã tạm tắt chương trình khuyến mãi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion is null)
            return NotFound();

        if (promotion.UsedCount > 0)
        {
            promotion.IsActive = false;
            promotion.UpdatedBy = CurrentUserName();

            await _db.SaveChangesAsync();

            TempData["Error"] =
                "Khuyến mãi đã phát sinh lượt dùng nên chỉ được tạm tắt.";

            return RedirectToAction(nameof(Index));
        }

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Đã xóa chương trình khuyến mãi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<PromotionIndexViewModel>
        BuildModelAsync(
            string? q,
            string? state,
            PromotionFormViewModel form,
            bool openModal)
    {
        await LoadOptionsAsync(form);

        var now = StorePriceClock.Now;

        var query = _db.Promotions
            .AsNoTracking()
            .Include(x => x.Markets)
                .ThenInclude(x => x.Market)
            .Include(x => x.Products)
                .ThenInclude(x => x.Product)
            .Include(x => x.Categories)
                .ThenInclude(x => x.Category)
            .Include(x => x.Brands)
                .ThenInclude(x => x.Brand)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();

            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Code.Contains(keyword));
        }

        if (string.Equals(state, "active",
                StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.IsActive &&
                x.StartsAt <= now &&
                x.EndsAt > now &&
                (!x.UsageLimit.HasValue ||
                 x.UsedCount < x.UsageLimit.Value));
        }
        else if (string.Equals(state, "upcoming",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.IsActive &&
                x.StartsAt > now);
        }
        else if (string.Equals(state, "expired",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.EndsAt <= now);
        }
        else if (string.Equals(state, "inactive",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.IsActive);
        }
        else if (string.Equals(state, "exhausted",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.UsageLimit.HasValue &&
                x.UsedCount >= x.UsageLimit.Value);
        }

        var promotions = await query
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartsAt)
            .Take(300)
            .ToListAsync();

        var items = promotions
            .Select(x => new PromotionListItem
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DiscountText = FormatDiscount(x),
                MinimumOrderText =
                    x.MinimumOrderAmount > 0
                        ? $"Tổng đơn từ {Money(x.MinimumOrderAmount)}"
                        : "Không giới hạn",
                ScopeText = FormatScope(x),
                Markets = string.Join(
                    ", ",
                    x.Markets
                        .OrderBy(m => m.Market.Name)
                        .Select(m => m.Market.Name)),
                UsageText = x.UsageLimit.HasValue
                    ? $"{x.UsedCount:N0}/{x.UsageLimit.Value:N0}"
                    : $"{x.UsedCount:N0}/∞",
                Period =
                    $"{x.StartsAt:dd/MM/yyyy HH:mm} – " +
                    $"{x.EndsAt:dd/MM/yyyy HH:mm}",
                Status = GetStatus(x, now),
                IsActive = x.IsActive,
                UsedCount = x.UsedCount
            })
            .ToList();

        return new PromotionIndexViewModel
        {
            Items = items,
            Form = form,
            Query = q,
            State = state,
            OpenFormModal = openModal,
            ActiveCount = await _db.Promotions
                .CountAsync(x =>
                    x.IsActive &&
                    x.StartsAt <= now &&
                    x.EndsAt > now &&
                    (!x.UsageLimit.HasValue ||
                     x.UsedCount < x.UsageLimit.Value)),
            UpcomingCount = await _db.Promotions
                .CountAsync(x =>
                    x.IsActive &&
                    x.StartsAt > now),
            ExpiredCount = await _db.Promotions
                .CountAsync(x =>
                    x.EndsAt <= now),
            TotalUsedCount = await _db.Promotions
                .SumAsync(x => (int?)x.UsedCount)
                ?? 0
        };
    }

    private async Task LoadOptionsAsync(
        PromotionFormViewModel form)
    {
        var marketIds = form.MarketIds.ToHashSet();
        var productIds = form.ProductIds.ToHashSet();
        var categoryIds = form.CategoryIds.ToHashSet();
        var brandIds = form.BrandIds.ToHashSet();

        form.MarketOptions = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Name} ({x.Code} · {x.CurrencyCode})",
                Selected = marketIds.Contains(x.Id)
            })
            .ToListAsync();

        form.ProductOptions = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Name} ({x.Sku})",
                Selected = productIds.Contains(x.Id)
            })
            .ToListAsync();

        form.CategoryOptions = await _db.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = categoryIds.Contains(x.Id)
            })
            .ToListAsync();

        form.BrandOptions = await _db.Brands
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = brandIds.Contains(x.Id)
            })
            .ToListAsync();
    }

    private async Task ValidateReferencesAsync(
        PromotionFormViewModel form)
    {
        await ValidateIdsAsync(
            "Form.MarketIds",
            form.MarketIds,
            _db.Markets.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.Id),
            "Có thị trường không tồn tại hoặc đã bị tắt.");

        if (form.ScopeType == PromotionScopeType.Products)
        {
            await ValidateIdsAsync(
                "Form.ProductIds",
                form.ProductIds,
                _db.Products.IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Select(x => x.Id),
                "Có sản phẩm không tồn tại hoặc đã bị xóa.");
        }
        else if (form.ScopeType == PromotionScopeType.Categories)
        {
            await ValidateIdsAsync(
                "Form.CategoryIds",
                form.CategoryIds,
                _db.Categories.IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Select(x => x.Id),
                "Có danh mục không tồn tại hoặc đã bị tắt.");
        }
        else if (form.ScopeType == PromotionScopeType.Brands)
        {
            await ValidateIdsAsync(
                "Form.BrandIds",
                form.BrandIds,
                _db.Brands.IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Select(x => x.Id),
                "Có thương hiệu không tồn tại hoặc đã bị tắt.");
        }
    }

    private async Task ValidateIdsAsync(
        string field,
        IReadOnlyCollection<int> selectedIds,
        IQueryable<int> allowedQuery,
        string error)
    {
        if (selectedIds.Count == 0)
            return;

        var allowedIds = await allowedQuery
            .Where(x => selectedIds.Contains(x))
            .ToListAsync();

        if (allowedIds.Count != selectedIds.Count)
        {
            ModelState.AddModelError(field, error);
        }
    }

    private static List<int> NormalizeIds(
        IEnumerable<int>? values)
    {
        return values?
            .Where(x => x > 0)
            .Distinct()
            .ToList()
            ?? new List<int>();
    }

    private static void SyncMarkets(
        Promotion promotion,
        IReadOnlyCollection<int> selectedIds)
    {
        Sync(
            promotion.Markets,
            selectedIds,
            x => x.MarketId,
            id => new PromotionMarket
            {
                MarketId = id
            });
    }

    private static void SyncProducts(
        Promotion promotion,
        IReadOnlyCollection<int> selectedIds)
    {
        Sync(
            promotion.Products,
            selectedIds,
            x => x.ProductId,
            id => new PromotionProduct
            {
                ProductId = id
            });
    }

    private static void SyncCategories(
        Promotion promotion,
        IReadOnlyCollection<int> selectedIds)
    {
        Sync(
            promotion.Categories,
            selectedIds,
            x => x.CategoryId,
            id => new PromotionCategory
            {
                CategoryId = id
            });
    }

    private static void SyncBrands(
        Promotion promotion,
        IReadOnlyCollection<int> selectedIds)
    {
        Sync(
            promotion.Brands,
            selectedIds,
            x => x.BrandId,
            id => new PromotionBrand
            {
                BrandId = id
            });
    }

    private static void Sync<T>(
        ICollection<T> current,
        IReadOnlyCollection<int> selectedIds,
        Func<T, int> getId,
        Func<int, T> create)
    {
        foreach (var removed in current
                     .Where(x =>
                         !selectedIds.Contains(getId(x)))
                     .ToList())
        {
            current.Remove(removed);
        }

        var existingIds =
            current.Select(getId).ToHashSet();

        foreach (var id in selectedIds
                     .Where(x =>
                         !existingIds.Contains(x)))
        {
            current.Add(create(id));
        }
    }

    private async Task<string> GeneratePromotionCodeAsync()
    {
        const string alphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var randomPart = new string(
                Enumerable.Range(0, 4)
                    .Select(_ =>
                        alphabet[
                            Random.Shared.Next(
                                alphabet.Length)])
                    .ToArray());

            var code =
                $"KM-{StorePriceClock.Now:yyMMdd}-{randomPart}";

            var exists = await _db.Promotions
                .AsNoTracking()
                .AnyAsync(x => x.Code == code);

            if (!exists)
                return code;
        }

        return
            $"KM-{StorePriceClock.Now:yyMMddHHmmss}";
    }

    private static string? NormalizeGeneratedCode(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var code =
            value.Trim().ToUpperInvariant();

        return System.Text.RegularExpressions.Regex
            .IsMatch(
                code,
                @"^KM-\d{6}-[A-Z0-9]{4}$")
            ? code
            : null;
    }

    private static string FormatDiscount(
        Promotion promotion)
    {
        if (promotion.DiscountType ==
            PromotionDiscountType.FixedAmount)
        {
            return $"Giảm {Money(promotion.DiscountValue)}";
        }

        var text =
            $"Giảm {promotion.DiscountValue:0.##}%";

        if (promotion.MaximumDiscountAmount.HasValue)
        {
            text +=
                $" · tối đa " +
                Money(
                    promotion.MaximumDiscountAmount.Value);
        }

        return text;
    }

    private static string FormatScope(
        Promotion promotion)
    {
        return promotion.ScopeType switch
        {
            PromotionScopeType.Products =>
                FormatNames(
                    "Sản phẩm",
                    promotion.Products
                        .Select(x => x.Product.Name)),
            PromotionScopeType.Categories =>
                FormatNames(
                    "Danh mục",
                    promotion.Categories
                        .Select(x => x.Category.Name)),
            PromotionScopeType.Brands =>
                FormatNames(
                    "Thương hiệu",
                    promotion.Brands
                        .Select(x => x.Brand.Name)),
            _ =>
                "Toàn bộ sản phẩm"
        };
    }

    private static string FormatNames(
        string prefix,
        IEnumerable<string> names)
    {
        var values = names
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x)
            .ToList();

        if (values.Count <= 3)
            return $"{prefix}: {string.Join(", ", values)}";

        return
            $"{prefix}: {string.Join(", ", values.Take(3))} " +
            $"và {values.Count - 3} mục khác";
    }

    private static string GetStatus(
        Promotion promotion,
        DateTime now)
    {
        if (!promotion.IsActive)
            return "Tạm tắt";

        if (promotion.UsageLimit.HasValue &&
            promotion.UsedCount >=
            promotion.UsageLimit.Value)
        {
            return "Hết lượt";
        }

        if (promotion.EndsAt <= now)
            return "Hết hạn";

        if (promotion.StartsAt > now)
            return "Sắp áp dụng";

        return "Đang áp dụng";
    }

    private static string Money(decimal value) =>
        value.ToString("N0", ViCulture) + "đ";

    private string CurrentUserName() =>
        User.Identity?.Name ?? "Admin";
}



