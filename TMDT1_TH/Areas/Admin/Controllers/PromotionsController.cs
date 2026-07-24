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
                    .ToList()
            };

            openModal = true;
        }
        else
        {
            form = new PromotionFormViewModel
            {
                StartsAt = DateTime.Now,
                EndsAt = DateTime.Now.AddDays(7),
                IsActive = true
            };
        }

        return View(
            await BuildModelAsync(
                q,
                state,
                form,
                openModal));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        PromotionIndexViewModel page)
    {
        var form = page.Form ?? new PromotionFormViewModel();

        form.Name = form.Name?.Trim() ?? string.Empty;
        form.Code =
            PromotionService.NormalizeCode(form.Code)
            ?? string.Empty;

        form.Description =
            string.IsNullOrWhiteSpace(form.Description)
                ? null
                : form.Description.Trim();

        form.MarketIds = form.MarketIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        ModelState.Clear();
        TryValidateModel(form, nameof(page.Form));

        if (form.MarketIds.Count > 0)
        {
            var activeMarketIds = await _db.Markets
                .AsNoTracking()
                .Where(x =>
                    form.MarketIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            if (activeMarketIds.Count !=
                form.MarketIds.Count)
            {
                ModelState.AddModelError(
                    "Form.MarketIds",
                    "Có thị trường không tồn tại hoặc đã bị tắt.");
            }
        }

        var duplicatedCode = await _db.Promotions
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id != form.Id &&
                x.Code == form.Code);

        if (duplicatedCode)
        {
            ModelState.AddModelError(
                "Form.Code",
                "Mã khuyến mãi đã được sử dụng.");
        }

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
                .FirstOrDefaultAsync(
                    x => x.Id == form.Id.Value)
                ?? throw new InvalidOperationException(
                    "Chương trình khuyến mãi không còn tồn tại.");
        }
        else
        {
            promotion = new Promotion
            {
                CreatedBy = CurrentUserName()
            };

            _db.Promotions.Add(promotion);
        }

        promotion.Name = form.Name;
        promotion.Code = form.Code;
        promotion.Description = form.Description;
        promotion.DiscountType = form.DiscountType;
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

        var existingMarketIds = promotion.Markets
            .Select(x => x.MarketId)
            .ToHashSet();

        foreach (var removed in promotion.Markets
                     .Where(x =>
                         !form.MarketIds.Contains(
                             x.MarketId))
                     .ToList())
        {
            promotion.Markets.Remove(removed);
        }

        foreach (var marketId in form.MarketIds
                     .Where(x =>
                         !existingMarketIds.Contains(x)))
        {
            promotion.Markets.Add(
                new PromotionMarket
                {
                    MarketId = marketId
                });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = form.Id.HasValue
            ? "Đã cập nhật chương trình khuyến mãi."
            : "Đã tạo chương trình khuyến mãi.";

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
            .Include(x => x.Markets)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (promotion is null)
            return NotFound();

        if (promotion.UsedCount > 0)
        {
            promotion.IsActive = false;
            promotion.UpdatedBy = CurrentUserName();

            await _db.SaveChangesAsync();

            TempData["Error"] =
                "Khuyến mãi đã phát sinh lượt dùng nên chỉ được tạm tắt, không thể xóa.";

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
        await LoadMarketOptionsAsync(form);

        var now = DateTime.Now;

        var query = _db.Promotions
            .AsNoTracking()
            .Include(x => x.Markets)
                .ThenInclude(x => x.Market)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();

            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Code.Contains(keyword));
        }

        if (string.Equals(
                state,
                "active",
                StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.IsActive &&
                x.StartsAt <= now &&
                x.EndsAt > now &&
                (!x.UsageLimit.HasValue ||
                 x.UsedCount < x.UsageLimit.Value));
        }
        else if (string.Equals(
                     state,
                     "upcoming",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.IsActive &&
                x.StartsAt > now);
        }
        else if (string.Equals(
                     state,
                     "expired",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.EndsAt <= now);
        }
        else if (string.Equals(
                     state,
                     "inactive",
                     StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.IsActive);
        }
        else if (string.Equals(
                     state,
                     "exhausted",
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
                DiscountText =
                    FormatDiscount(x),
                MinimumOrderText =
                    x.MinimumOrderAmount > 0
                        ? $"Từ {Money(x.MinimumOrderAmount)}"
                        : "Không giới hạn",
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

    private async Task LoadMarketOptionsAsync(
        PromotionFormViewModel form)
    {
        var selected = form.MarketIds.ToHashSet();

        form.MarketOptions = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text =
                    $"{x.Name} ({x.Code} · {x.CurrencyCode})",
                Selected = selected.Contains(x.Id)
            })
            .ToListAsync();
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
