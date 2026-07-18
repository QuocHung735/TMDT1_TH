using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class MarketsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(
        string? q,
        bool? active,
        int? editId,
        CancellationToken cancellationToken)
    {
        var form = new MarketFormViewModel();
        var openModal = false;

        if (editId.HasValue)
        {
            var market = await dbContext.Markets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editId.Value, cancellationToken);

            if (market is null)
            {
                TempData["Error"] = "Không tìm thấy thị trường cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            form = MapForm(market);
            openModal = true;
        }

        return View(await BuildViewModelAsync(q, active, form, openModal, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MarketFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = null;
        Normalize(form);
        await ValidateUniqueCodeAsync(form, cancellationToken);

        var hasAnyMarket = await dbContext.Markets.AnyAsync(cancellationToken);
        if (!hasAnyMarket)
        {
            form.IsDefault = true;
            form.IsActive = true;
        }

        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (form.IsDefault)
                await ClearCurrentDefaultAsync(null, cancellationToken);

            var market = new Market
            {
                Code = form.Code,
                Name = form.Name,
                CurrencyCode = form.CurrencyCode,
                CountryCode = Clean(form.CountryCode),
                Description = Clean(form.Description),
                IsActive = form.IsActive,
                IsDefault = form.IsDefault,
                CreatedBy = CurrentUserName()
            };

            dbContext.Markets.Add(market);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["Success"] = $"Đã tạo thị trường “{market.Name}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, GetDatabaseMessage(exception));
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MarketFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            TempData["Error"] = "Thiếu mã thị trường cần chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        Normalize(form);
        await ValidateUniqueCodeAsync(form, cancellationToken);

        var market = await dbContext.Markets.FirstOrDefaultAsync(x => x.Id == form.Id.Value, cancellationToken);
        if (market is null)
        {
            TempData["Error"] = "Thị trường không còn tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (market.IsDefault && !form.IsDefault)
        {
            ModelState.AddModelError(nameof(form.IsDefault),
                "Không thể bỏ trạng thái mặc định trực tiếp. Hãy đặt một thị trường khác làm mặc định.");
        }

        if (market.IsDefault && !form.IsActive)
        {
            ModelState.AddModelError(nameof(form.IsActive),
                "Không thể tạm tắt thị trường mặc định.");
        }

        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (form.IsDefault && !market.IsDefault)
                await ClearCurrentDefaultAsync(market.Id, cancellationToken);

            market.Code = form.Code;
            market.Name = form.Name;
            market.CurrencyCode = form.CurrencyCode;
            market.CountryCode = Clean(form.CountryCode);
            market.Description = Clean(form.Description);
            market.IsActive = form.IsActive;
            market.IsDefault = form.IsDefault;
            market.UpdatedBy = CurrentUserName();

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["Success"] = $"Đã cập nhật thị trường “{market.Name}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(string.Empty, GetDatabaseMessage(exception));
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var market = await dbContext.Markets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (market is null)
        {
            TempData["Error"] = "Không tìm thấy thị trường.";
            return RedirectToAction(nameof(Index));
        }

        if (market.IsDefault && market.IsActive)
        {
            TempData["Error"] = "Không thể tạm tắt thị trường mặc định. Hãy đặt thị trường khác làm mặc định trước.";
            return RedirectToAction(nameof(Index));
        }

        market.IsActive = !market.IsActive;
        market.UpdatedBy = CurrentUserName();
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = market.IsActive
            ? $"Đã kích hoạt thị trường “{market.Name}”."
            : $"Đã tạm tắt thị trường “{market.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id, CancellationToken cancellationToken)
    {
        var market = await dbContext.Markets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (market is null)
        {
            TempData["Error"] = "Không tìm thấy thị trường.";
            return RedirectToAction(nameof(Index));
        }

        if (market.IsDefault)
        {
            TempData["Success"] = $"“{market.Name}” đã là thị trường mặc định.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ClearCurrentDefaultAsync(market.Id, cancellationToken);
            market.IsDefault = true;
            market.IsActive = true;
            market.UpdatedBy = CurrentUserName();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["Success"] = $"Đã đặt “{market.Name}” làm thị trường mặc định.";
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            TempData["Error"] = GetDatabaseMessage(exception);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var market = await dbContext.Markets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (market is null)
        {
            TempData["Error"] = "Không tìm thấy thị trường.";
            return RedirectToAction(nameof(Index));
        }

        if (market.IsDefault)
        {
            TempData["Error"] = "Không thể xóa thị trường mặc định. Hãy đặt thị trường khác làm mặc định trước.";
            return RedirectToAction(nameof(Index));
        }

        var priceCount = await dbContext.PriceSchedules.CountAsync(x => x.MarketId == id, cancellationToken);
        if (priceCount > 0)
        {
            TempData["Error"] = $"Không thể xóa vì thị trường đang có {priceCount:N0} lịch giá. Bạn có thể tạm tắt thị trường này.";
            return RedirectToAction(nameof(Index));
        }

        dbContext.Markets.Remove(market);
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã xóa thị trường “{market.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<MarketsViewModel> BuildViewModelAsync(
        string? search,
        bool? active,
        MarketFormViewModel form,
        bool openModal,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var query = dbContext.Markets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.Code.Contains(keyword) ||
                x.Name.Contains(keyword) ||
                x.CurrencyCode.Contains(keyword) ||
                (x.CountryCode != null && x.CountryCode.Contains(keyword)));
        }

        if (active.HasValue)
            query = query.Where(x => x.IsActive == active.Value);

        var markets = await query
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.CountryCode,
                x.Description,
                x.IsDefault,
                x.IsActive,
                PriceCount = x.PriceSchedules.Count,
                ActivePriceCount = x.PriceSchedules.Count(p => p.IsActive && p.ValidFrom <= now && (!p.ValidTo.HasValue || p.ValidTo > now))
            })
            .ToListAsync(cancellationToken);

        var rows = markets.Select(x => new MarketRow(
            x.Id,
            x.Code,
            x.Name,
            x.CurrencyCode,
            x.CountryCode ?? "—",
            x.Description,
            x.PriceCount,
            x.ActivePriceCount,
            x.IsActive ? "Đang hoạt động" : "Tạm ẩn",
            x.IsDefault,
            x.IsActive)).ToList();

        return new MarketsViewModel
        {
            Items = rows,
            Form = form,
            Search = search,
            Active = active,
            TotalCount = await dbContext.Markets.CountAsync(cancellationToken),
            ActiveCount = await dbContext.Markets.CountAsync(x => x.IsActive, cancellationToken),
            TotalPriceCount = await dbContext.PriceSchedules.CountAsync(cancellationToken),
            OpenFormModal = openModal
        };
    }

    private async Task ValidateUniqueCodeAsync(MarketFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Code))
            return;

        var exists = await dbContext.Markets.AnyAsync(
            x => x.Code == form.Code && (!form.Id.HasValue || x.Id != form.Id.Value),
            cancellationToken);

        if (exists)
            ModelState.AddModelError(nameof(form.Code), "Mã thị trường đã tồn tại.");
    }

    private async Task ClearCurrentDefaultAsync(int? excludedId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = CurrentUserName();
        var query = dbContext.Markets.Where(x => x.IsDefault);
        if (excludedId.HasValue)
            query = query.Where(x => x.Id != excludedId.Value);

        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.IsDefault, false)
            .SetProperty(x => x.UpdatedAt, now)
            .SetProperty(x => x.UpdatedBy, user), cancellationToken);
    }

    private string CurrentUserName() => User.Identity?.Name ?? "Admin";

    private static MarketFormViewModel MapForm(Market market) => new()
    {
        Id = market.Id,
        Code = market.Code,
        Name = market.Name,
        CurrencyCode = market.CurrencyCode,
        CountryCode = market.CountryCode,
        Description = market.Description,
        IsActive = market.IsActive,
        IsDefault = market.IsDefault
    };

    private static void Normalize(MarketFormViewModel form)
    {
        form.Code = (form.Code ?? string.Empty).Trim().ToUpperInvariant();
        form.Name = (form.Name ?? string.Empty).Trim();
        form.CurrencyCode = (form.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        form.CountryCode = Clean(form.CountryCode)?.ToUpperInvariant();
        form.Description = Clean(form.Description);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetDatabaseMessage(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        if (message.Contains("IX_Markets_Code", StringComparison.OrdinalIgnoreCase))
            return "Mã thị trường đã tồn tại.";
        if (message.Contains("IX_Markets_IsDefault", StringComparison.OrdinalIgnoreCase))
            return "Hệ thống chỉ cho phép một thị trường mặc định.";
        return "Không thể lưu thị trường do dữ liệu đang được sử dụng hoặc vi phạm ràng buộc database.";
    }
}
