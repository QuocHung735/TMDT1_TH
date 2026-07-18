using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class PricingController : Controller
{
    private readonly ApplicationDbContext _db;
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public PricingController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        int? marketId,
        string? state,
        int? editId)
    {
        PricingScheduleFormViewModel form;
        var openModal = false;

        if (editId.HasValue)
        {
            var schedule = await _db.PriceSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editId.Value);

            if (schedule is null)
            {
                TempData["Error"] = "Lịch giá không còn tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            form = new PricingScheduleFormViewModel
            {
                Id = schedule.Id,
                TargetKey = schedule.ProductVariantId.HasValue
                    ? $"variant:{schedule.ProductVariantId.Value}"
                    : $"product:{schedule.ProductId!.Value}",
                MarketId = schedule.MarketId,
                CostPrice = schedule.CostPrice,
                ListPrice = schedule.ListPrice,
                SalePrice = schedule.SalePrice,
                ValidFrom = schedule.ValidFrom,
                ValidTo = schedule.ValidTo,
                IsUnlimited = !schedule.ValidTo.HasValue,
                IsActive = schedule.IsActive,
                Note = schedule.Note
            };
            openModal = true;
        }
        else
        {
            var defaultMarketId = await _db.Markets
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            form = new PricingScheduleFormViewModel
            {
                MarketId = defaultMarketId,
                ValidFrom = DateTime.Now,
                IsUnlimited = true,
                IsActive = true
            };
        }

        var model = await BuildIndexModelAsync(q, marketId, state, form, openModal);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PricingIndexViewModel page)
    {
        var form = page.Form ?? new PricingScheduleFormViewModel();
        form.TargetKey = form.TargetKey?.Trim() ?? string.Empty;

        if (form.IsUnlimited)
            form.ValidTo = null;

        TargetReference target;
        if (!TryParseTargetKey(form.TargetKey, out target))
        {
            ModelState.AddModelError("Form.TargetKey", "Mục tiêu giá không hợp lệ.");
        }
        else
        {
            await ValidateTargetAsync(target, form);
        }

        var marketExists = form.MarketId.HasValue && await _db.Markets
            .AsNoTracking()
            .AnyAsync(x => x.Id == form.MarketId.Value && x.IsActive);

        if (!marketExists)
            ModelState.AddModelError("Form.MarketId", "Thị trường không tồn tại hoặc đã bị tắt.");

        if (ModelState.IsValid && form.IsActive)
        {
            var hasOverlap = await HasOverlapAsync(form, target);
            if (hasOverlap)
            {
                ModelState.AddModelError(
                    "Form.ValidFrom",
                    "Khoảng thời gian đang chồng lấn với lịch giá hoạt động khác của cùng mục tiêu và thị trường.");
            }
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexModelAsync(
                page.Query,
                page.MarketId,
                page.State,
                form,
                true);
            return View("Index", invalidModel);
        }

        try
        {
            PriceSchedule schedule;
            if (form.Id.HasValue)
            {
                schedule = await _db.PriceSchedules.FirstOrDefaultAsync(x => x.Id == form.Id.Value)
                    ?? throw new InvalidOperationException("Lịch giá không còn tồn tại.");
            }
            else
            {
                schedule = new PriceSchedule
                {
                    CreatedBy = CurrentUserName()
                };
                _db.PriceSchedules.Add(schedule);
            }

            schedule.ProductId = target.ProductId;
            schedule.ProductVariantId = target.ProductVariantId;
            schedule.MarketId = form.MarketId!.Value;
            schedule.CostPrice = form.CostPrice;
            schedule.ListPrice = form.ListPrice;
            schedule.SalePrice = form.SalePrice;
            schedule.ValidFrom = form.ValidFrom;
            schedule.ValidTo = form.ValidTo;
            schedule.IsActive = form.IsActive;
            schedule.Note = NullIfWhiteSpace(form.Note);
            schedule.UpdatedBy = CurrentUserName();

            await _db.SaveChangesAsync();

            TempData["Success"] = form.Id.HasValue
                ? "Đã cập nhật lịch giá."
                : "Đã tạo lịch giá mới.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            ModelState.AddModelError(string.Empty, GetDatabaseMessage(exception));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        var errorModel = await BuildIndexModelAsync(
            page.Query,
            page.MarketId,
            page.State,
            form,
            true);
        return View("Index", errorModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var schedule = await _db.PriceSchedules.FirstOrDefaultAsync(x => x.Id == id);
        if (schedule is null)
            return NotFound();

        if (!schedule.IsActive)
        {
            var form = new PricingScheduleFormViewModel
            {
                Id = schedule.Id,
                MarketId = schedule.MarketId,
                ValidFrom = schedule.ValidFrom,
                ValidTo = schedule.ValidTo,
                IsUnlimited = !schedule.ValidTo.HasValue,
                IsActive = true
            };

            var target = new TargetReference(schedule.ProductId, schedule.ProductVariantId);
            if (await HasOverlapAsync(form, target))
            {
                TempData["Error"] = "Không thể kích hoạt vì lịch giá đang chồng lấn với lịch khác.";
                return RedirectToAction(nameof(Index));
            }
        }

        schedule.IsActive = !schedule.IsActive;
        schedule.UpdatedBy = CurrentUserName();
        await _db.SaveChangesAsync();

        TempData["Success"] = schedule.IsActive
            ? "Đã kích hoạt lịch giá."
            : "Đã tạm tắt lịch giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _db.PriceSchedules.FirstOrDefaultAsync(x => x.Id == id);
        if (schedule is null)
            return NotFound();

        _db.PriceSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa lịch giá. Lịch sử thay đổi vẫn được giữ lại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportHistory()
    {
        var rows = await BuildHistoryRowsAsync(1000);
        var csv = new StringBuilder();
        csv.AppendLine("Sản phẩm,Biến thể,Thị trường,Nội dung,Giá trị cũ,Giá trị mới,Thay đổi,Hành động,Người cập nhật,Thời gian,Lý do");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(row.Product),
                Csv(row.Variant),
                Csv(row.Market),
                Csv(row.ChangeType),
                Csv(row.OldValue),
                Csv(row.NewValue),
                Csv(row.Change),
                Csv(row.Action),
                Csv(row.User),
                Csv(row.Time),
                Csv(row.Reason ?? string.Empty)
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"lich-su-gia-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    private async Task<PricingIndexViewModel> BuildIndexModelAsync(
        string? q,
        int? marketId,
        string? state,
        PricingScheduleFormViewModel form,
        bool openModal)
    {
        await LoadFormOptionsAsync(form);

        var now = DateTime.Now;
        var nextSevenDays = now.AddDays(7);
        var query = _db.PriceSchedules
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.ProductVariant)
                .ThenInclude(x => x!.Product)
            .Include(x => x.Market)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Market.Name.Contains(keyword) ||
                x.Market.Code.Contains(keyword) ||
                (x.Product != null && (x.Product.Name.Contains(keyword) || x.Product.Sku.Contains(keyword))) ||
                (x.ProductVariant != null &&
                    (x.ProductVariant.Name.Contains(keyword) ||
                     x.ProductVariant.Sku.Contains(keyword) ||
                     x.ProductVariant.Product.Name.Contains(keyword))));
        }

        if (marketId.HasValue)
            query = query.Where(x => x.MarketId == marketId.Value);

        if (string.Equals(state, "current", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.IsActive && x.ValidFrom <= now &&
                (!x.ValidTo.HasValue || x.ValidTo.Value > now));
        }
        else if (string.Equals(state, "upcoming", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.IsActive && x.ValidFrom > now);
        }
        else if (string.Equals(state, "expiring", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.IsActive && x.ValidFrom <= now &&
                x.ValidTo.HasValue && x.ValidTo.Value > now && x.ValidTo.Value <= nextSevenDays);
        }
        else if (string.Equals(state, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.IsActive);
        }
        else if (string.Equals(state, "expired", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.ValidTo.HasValue && x.ValidTo.Value <= now);
        }

        var schedules = await query
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.ValidFrom)
            .Take(300)
            .ToListAsync();

        var rows = schedules.Select(schedule => new PricingScheduleListItem
        {
            Id = schedule.Id,
            Product = schedule.Product?.Name ?? schedule.ProductVariant?.Product.Name ?? "Sản phẩm đã xóa",
            Variant = schedule.ProductVariant?.Name ?? "Toàn sản phẩm",
            Sku = schedule.Product?.Sku ?? schedule.ProductVariant?.Sku ?? string.Empty,
            Market = schedule.Market.Name,
            CostPrice = Money(schedule.CostPrice),
            ListPrice = Money(schedule.ListPrice),
            SalePrice = Money(schedule.SalePrice),
            Period = FormatPeriod(schedule.ValidFrom, schedule.ValidTo),
            Status = GetScheduleStatus(schedule, now, nextSevenDays),
            IsActive = schedule.IsActive
        }).ToList();

        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var model = new PricingIndexViewModel
        {
            Items = rows,
            History = await BuildHistoryRowsAsync(200),
            MarketFilterOptions = await BuildMarketOptionsAsync(marketId, false),
            Form = form,
            Query = q,
            MarketId = marketId,
            State = state,
            OpenFormModal = openModal,
            CurrentCount = await _db.PriceSchedules.CountAsync(x => x.IsActive && x.ValidFrom <= now &&
                (!x.ValidTo.HasValue || x.ValidTo.Value > now)),
            UpcomingCount = await _db.PriceSchedules.CountAsync(x => x.IsActive && x.ValidFrom > now && x.ValidFrom <= nextSevenDays),
            ExpiringCount = await _db.PriceSchedules.CountAsync(x => x.IsActive && x.ValidFrom <= now &&
                x.ValidTo.HasValue && x.ValidTo.Value > now && x.ValidTo.Value <= nextSevenDays),
            ChangedThisMonthCount = await _db.PriceHistories.CountAsync(x => x.ChangedAt >= startOfMonth),
            ActiveMarketCount = await _db.Markets.CountAsync(x => x.IsActive)
        };

        return model;
    }

    private async Task LoadFormOptionsAsync(PricingScheduleFormViewModel form)
    {
        form.TargetOptions = await BuildTargetOptionsAsync(form.TargetKey);
        form.MarketOptions = await BuildMarketOptionsAsync(form.MarketId, true);
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildTargetOptionsAsync(string? selectedKey)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var result = new List<SelectListItem>();
        foreach (var product in products)
        {
            var productKey = $"product:{product.Id}";
            result.Add(new SelectListItem
            {
                Value = productKey,
                Text = $"{product.Name} — toàn sản phẩm",
                Selected = string.Equals(selectedKey, productKey, StringComparison.OrdinalIgnoreCase)
            });

            var group = new SelectListGroup { Name = $"Biến thể — {product.Name}" };
            foreach (var variant in product.Variants.Where(x => x.IsActive).OrderBy(x => x.Name))
            {
                var variantKey = $"variant:{variant.Id}";
                result.Add(new SelectListItem
                {
                    Value = variantKey,
                    Text = $"{variant.Name} ({variant.Sku})",
                    Group = group,
                    Selected = string.Equals(selectedKey, variantKey, StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        return result;
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildMarketOptionsAsync(int? selectedId, bool includeInactiveSelected)
    {
        var markets = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive || (includeInactiveSelected && selectedId.HasValue && x.Id == selectedId.Value))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return markets.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = $"{x.Name} ({x.Code} · {x.CurrencyCode})",
            Selected = selectedId == x.Id
        }).ToList();
    }

    private async Task ValidateTargetAsync(TargetReference target, PricingScheduleFormViewModel form)
    {
        if (target.ProductId.HasValue)
        {
            var exists = await _db.Products.AsNoTracking().AnyAsync(x => x.Id == target.ProductId.Value);
            if (!exists)
                ModelState.AddModelError("Form.TargetKey", "Sản phẩm không tồn tại hoặc đã bị xóa.");
        }
        else if (target.ProductVariantId.HasValue)
        {
            var exists = await _db.ProductVariants.AsNoTracking()
                .AnyAsync(x => x.Id == target.ProductVariantId.Value && x.IsActive);
            if (!exists)
                ModelState.AddModelError("Form.TargetKey", "Biến thể không tồn tại hoặc đã bị tắt.");
        }
        else
        {
            ModelState.AddModelError("Form.TargetKey", "Vui lòng chọn sản phẩm hoặc biến thể.");
        }
    }

    private async Task<bool> HasOverlapAsync(PricingScheduleFormViewModel form, TargetReference target)
    {
        var end = form.ValidTo ?? DateTime.MaxValue;
        var query = _db.PriceSchedules
            .AsNoTracking()
            .Where(x => x.Id != form.Id &&
                        x.IsActive &&
                        x.MarketId == form.MarketId!.Value &&
                        x.ValidFrom < end &&
                        form.ValidFrom < (x.ValidTo ?? DateTime.MaxValue));

        if (target.ProductId.HasValue)
            query = query.Where(x => x.ProductId == target.ProductId.Value && x.ProductVariantId == null);
        else
            query = query.Where(x => x.ProductVariantId == target.ProductVariantId && x.ProductId == null);

        return await query.AnyAsync();
    }

    private async Task<IReadOnlyList<PricingHistoryListItem>> BuildHistoryRowsAsync(int take)
    {
        var histories = await _db.PriceHistories
            .AsNoTracking()
            .OrderByDescending(x => x.ChangedAt)
            .Take(take)
            .ToListAsync();

        if (histories.Count == 0)
            return Array.Empty<PricingHistoryListItem>();

        var productIds = histories.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
        var variantIds = histories.Where(x => x.ProductVariantId.HasValue).Select(x => x.ProductVariantId!.Value).Distinct().ToList();
        var marketIds = histories.Select(x => x.MarketId).Distinct().ToList();

        var products = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new TargetName(x.Name, "Toàn sản phẩm"));

        var variants = await _db.ProductVariants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => variantIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new TargetName(x.Product.Name, x.Name));

        var markets = await _db.Markets
            .AsNoTracking()
            .Where(x => marketIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var rows = new List<PricingHistoryListItem>();
        foreach (var history in histories)
        {
            TargetName target;
            if (history.ProductVariantId.HasValue && variants.TryGetValue(history.ProductVariantId.Value, out var variantTarget))
                target = variantTarget;
            else if (history.ProductId.HasValue && products.TryGetValue(history.ProductId.Value, out var productTarget))
                target = productTarget;
            else
                target = new TargetName("Sản phẩm đã xóa", "—");

            var market = markets.TryGetValue(history.MarketId, out var marketName) ? marketName : "Thị trường đã xóa";
            var beforeCount = rows.Count;

            AddMoneyHistory(rows, history, target, market, "Giá vốn", history.OldCostPrice, history.NewCostPrice);
            AddMoneyHistory(rows, history, target, market, "Giá niêm yết", history.OldListPrice, history.NewListPrice);
            AddMoneyHistory(rows, history, target, market, "Giá bán", history.OldSalePrice, history.NewSalePrice);

            if (history.OldValidFrom != history.NewValidFrom || history.OldValidTo != history.NewValidTo)
            {
                rows.Add(CreateHistoryRow(
                    history,
                    target,
                    market,
                    "Thời gian áp dụng",
                    FormatNullablePeriod(history.OldValidFrom, history.OldValidTo),
                    FormatNullablePeriod(history.NewValidFrom, history.NewValidTo),
                    "Đã đổi",
                    "neutral"));
            }

            if (rows.Count == beforeCount)
            {
                rows.Add(CreateHistoryRow(
                    history,
                    target,
                    market,
                    "Lịch giá",
                    "—",
                    GetActionLabel(history.Action),
                    "Đã cập nhật",
                    "neutral"));
            }
        }

        return rows.Take(take).ToList();
    }

    private static void AddMoneyHistory(
        ICollection<PricingHistoryListItem> rows,
        PriceHistory history,
        TargetName target,
        string market,
        string type,
        decimal? oldValue,
        decimal? newValue)
    {
        if (history.Action == PriceChangeType.Updated && oldValue == newValue)
            return;

        if (!oldValue.HasValue && !newValue.HasValue)
            return;

        var change = CalculateChange(oldValue, newValue);
        rows.Add(CreateHistoryRow(
            history,
            target,
            market,
            type,
            oldValue.HasValue ? Money(oldValue.Value) : "—",
            newValue.HasValue ? Money(newValue.Value) : "—",
            change.Text,
            change.Tone));
    }

    private static PricingHistoryListItem CreateHistoryRow(
        PriceHistory history,
        TargetName target,
        string market,
        string type,
        string oldValue,
        string newValue,
        string change,
        string tone)
    {
        return new PricingHistoryListItem
        {
            Product = target.Product,
            Variant = target.Variant,
            Market = market,
            ChangeType = type,
            OldValue = oldValue,
            NewValue = newValue,
            Change = change,
            Tone = tone,
            Action = GetActionLabel(history.Action),
            User = history.ChangedBy,
            Time = history.ChangedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            Reason = history.Reason
        };
    }

    private static bool TryParseTargetKey(string? value, out TargetReference target)
    {
        target = new TargetReference(null, null);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var id) || id <= 0)
            return false;

        if (string.Equals(parts[0], "product", StringComparison.OrdinalIgnoreCase))
        {
            target = new TargetReference(id, null);
            return true;
        }

        if (string.Equals(parts[0], "variant", StringComparison.OrdinalIgnoreCase))
        {
            target = new TargetReference(null, id);
            return true;
        }

        return false;
    }

    private static string GetScheduleStatus(PriceSchedule schedule, DateTime now, DateTime nextSevenDays)
    {
        if (!schedule.IsActive)
            return "Tạm tắt";

        if (schedule.ValidTo.HasValue && schedule.ValidTo.Value <= now)
            return "Hết hạn";

        if (schedule.ValidFrom > now)
            return "Sắp áp dụng";

        if (schedule.ValidTo.HasValue && schedule.ValidTo.Value <= nextSevenDays)
            return "Sắp hết hạn";

        return "Đang áp dụng";
    }

    private static string FormatPeriod(DateTime from, DateTime? to)
    {
        return to.HasValue
            ? $"{from:dd/MM/yyyy HH:mm} – {to.Value:dd/MM/yyyy HH:mm}"
            : $"Từ {from:dd/MM/yyyy HH:mm} · vô hạn";
    }

    private static string FormatNullablePeriod(DateTime? from, DateTime? to)
    {
        if (!from.HasValue)
            return "—";

        return FormatPeriod(from.Value, to);
    }

    private static (string Text, string Tone) CalculateChange(decimal? oldValue, decimal? newValue)
    {
        if (!oldValue.HasValue && newValue.HasValue)
            return ("Mới", "up");

        if (oldValue.HasValue && !newValue.HasValue)
            return ("Đã xóa", "down");

        if (!oldValue.HasValue || !newValue.HasValue || oldValue.Value == 0)
            return ("—", "neutral");

        var percent = (newValue.Value - oldValue.Value) / oldValue.Value * 100;
        if (percent > 0)
            return ($"+{percent:N1}%", "up");
        if (percent < 0)
            return ($"{percent:N1}%", "down");
        return ("0%", "neutral");
    }

    private static string GetActionLabel(PriceChangeType action)
    {
        if (action == PriceChangeType.Created)
            return "Tạo mới";
        if (action == PriceChangeType.Deleted)
            return "Xóa";
        return "Cập nhật";
    }

    private static string Money(decimal value) => value.ToString("N0", ViCulture) + "đ";

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string CurrentUserName() => User.Identity?.Name ?? "Admin";

    private static string GetDatabaseMessage(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        if (message.Contains("51001", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("chồng lấn", StringComparison.OrdinalIgnoreCase))
        {
            return "Khoảng thời gian giá đang chồng lấn với lịch giá khác của cùng mục tiêu và thị trường.";
        }

        return "Không thể lưu lịch giá. Hãy kiểm tra dữ liệu và thử lại.";
    }

    private sealed record TargetReference(int? ProductId, int? ProductVariantId);
    private sealed record TargetName(string Product, string Variant);
}
