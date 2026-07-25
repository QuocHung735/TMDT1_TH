using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Infrastructure;
using TMDT1_TH.Infrastructure.Marketplace;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class StoresController(
    ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        bool? active,
        int? editId,
        CancellationToken cancellationToken)
    {
        var form = new StoreFormViewModel();

        if (editId.HasValue)
        {
            var store = await _db.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == editId.Value,
                    cancellationToken);

            if (store is null)
            {
                TempData["Error"] =
                    "Không tìm thấy cửa hàng cần chỉnh sửa.";

                return RedirectToAction(nameof(Index));
            }

            form = new StoreFormViewModel
            {
                Id = store.Id,
                Name = store.Name,
                Slug = store.Slug,
                Description = store.Description,
                LogoUrl = store.LogoUrl,
                ContactEmail = store.ContactEmail,
                PhoneNumber = store.PhoneNumber,
                AddressLine = store.AddressLine,
                Ward = store.Ward,
                District = store.District,
                Province = store.Province,
                DisplayOrder = store.DisplayOrder,
                IsActive = store.IsActive,
                IsVerified = store.IsVerified
            };
        }

        return View(
            await BuildViewModelAsync(
                q,
                active,
                form,
                cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")]
        StoreFormViewModel form,
        CancellationToken cancellationToken)
    {
        form.Id = null;
        Normalize(form);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    cancellationToken));
        }

        var slugSource =
            string.IsNullOrWhiteSpace(form.Slug)
                ? form.Name
                : form.Slug;

        var store = new Store
        {
            Name = form.Name,
            Slug = await CreateUniqueSlugAsync(
                slugSource!,
                null,
                cancellationToken),
            Description = Clean(form.Description),
            LogoUrl = Clean(form.LogoUrl),
            ContactEmail = Clean(form.ContactEmail),
            PhoneNumber = Clean(form.PhoneNumber),
            AddressLine = Clean(form.AddressLine),
            Ward = Clean(form.Ward),
            District = Clean(form.District),
            Province = Clean(form.Province),
            DisplayOrder = form.DisplayOrder,
            IsActive = form.IsActive,
            IsVerified = form.IsVerified,
            ReliabilityScore = null,
            CreatedBy = CurrentUserName()
        };

        _db.Stores.Add(store);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] =
            $"Đã tạo cửa hàng “{store.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Bind(Prefix = "Form")]
        StoreFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            TempData["Error"] =
                "Thiếu mã cửa hàng cần chỉnh sửa.";

            return RedirectToAction(nameof(Index));
        }

        Normalize(form);

        var store = await _db.Stores
            .FirstOrDefaultAsync(
                x => x.Id == form.Id.Value,
                cancellationToken);

        if (store is null)
        {
            TempData["Error"] =
                "Cửa hàng không còn tồn tại.";

            return RedirectToAction(nameof(Index));
        }

        var productCount = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(
                x => x.StoreId == store.Id,
                cancellationToken);

        if (store.Id ==
                StoreDefaults.OfficialStoreId &&
            !form.IsActive &&
            productCount > 0)
        {
            ModelState.AddModelError(
                nameof(form.IsActive),
                "Không thể ẩn cửa hàng mặc định khi " +
                "vẫn còn sản phẩm thuộc cửa hàng này.");
        }

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    cancellationToken));
        }

        var slugSource =
            string.IsNullOrWhiteSpace(form.Slug)
                ? form.Name
                : form.Slug;

        store.Name = form.Name;
        store.Slug =
            await CreateUniqueSlugAsync(
                slugSource!,
                store.Id,
                cancellationToken);
        store.Description = Clean(form.Description);
        store.LogoUrl = Clean(form.LogoUrl);
        store.ContactEmail =
            Clean(form.ContactEmail);
        store.PhoneNumber = Clean(form.PhoneNumber);
        store.AddressLine = Clean(form.AddressLine);
        store.Ward = Clean(form.Ward);
        store.District = Clean(form.District);
        store.Province = Clean(form.Province);
        store.DisplayOrder = form.DisplayOrder;
        store.IsActive = form.IsActive;
        store.IsVerified = form.IsVerified;
        store.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] =
            $"Đã cập nhật cửa hàng “{store.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        int id,
        CancellationToken cancellationToken)
    {
        var store = await _db.Stores
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (store is null)
        {
            TempData["Error"] =
                "Không tìm thấy cửa hàng.";

            return RedirectToAction(nameof(Index));
        }

        if (store.IsActive)
        {
            var productCount = await _db.Products
                .IgnoreQueryFilters()
                .CountAsync(
                    x => x.StoreId == store.Id,
                    cancellationToken);

            if (store.Id ==
                    StoreDefaults.OfficialStoreId &&
                productCount > 0)
            {
                TempData["Error"] =
                    "Không thể ẩn cửa hàng mặc định khi " +
                    "vẫn còn sản phẩm thuộc cửa hàng này.";

                return RedirectToAction(nameof(Index));
            }
        }

        store.IsActive = !store.IsActive;
        store.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = store.IsActive
            ? $"Đã kích hoạt cửa hàng “{store.Name}”."
            : $"Đã tạm ẩn cửa hàng “{store.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var store = await _db.Stores
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (store is null)
        {
            TempData["Error"] =
                "Không tìm thấy cửa hàng.";

            return RedirectToAction(nameof(Index));
        }

        var productCount = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(
                x => x.StoreId == store.Id,
                cancellationToken);

        if (!StorePolicy.CanDelete(
                store.Id,
                productCount))
        {
            TempData["Error"] =
                store.Id ==
                StoreDefaults.OfficialStoreId
                    ? "Không thể xóa cửa hàng mặc định."
                    : "Không thể xóa cửa hàng đang có " +
                      "sản phẩm. Hãy chuyển sản phẩm sang " +
                      "cửa hàng khác hoặc tạm ẩn cửa hàng.";

            return RedirectToAction(nameof(Index));
        }

        store.IsDeleted = true;
        store.IsActive = false;
        store.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] =
            $"Đã xóa cửa hàng “{store.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<StoresViewModel>
        BuildViewModelAsync(
            string? query,
            bool? active,
            StoreFormViewModel form,
            CancellationToken cancellationToken)
    {
        var stores = await _db.Stores
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.AddressLine,
                x.Ward,
                x.District,
                x.Province,
                x.ContactEmail,
                x.PhoneNumber,
                x.IsActive,
                x.IsVerified,
                x.ReliabilityScore,
                x.DisplayOrder,
                ProductCount = x.Products.Count
            })
            .ToListAsync(cancellationToken);

        var filtered = stores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var keyword = query.Trim();

            filtered = filtered.Where(x =>
                x.Name.Contains(
                    keyword,
                    StringComparison
                        .CurrentCultureIgnoreCase) ||
                x.Slug.Contains(
                    keyword,
                    StringComparison
                        .OrdinalIgnoreCase) ||
                (x.Province?.Contains(
                    keyword,
                    StringComparison
                        .CurrentCultureIgnoreCase)
                 ?? false));
        }

        if (active.HasValue)
        {
            filtered = filtered.Where(
                x => x.IsActive == active.Value);
        }

        var rows = filtered
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x =>
            {
                var location = string.Join(
                    ", ",
                    new[]
                    {
                        x.Ward,
                        x.District,
                        x.Province
                    }.Where(value =>
                        !string.IsNullOrWhiteSpace(
                            value)));

                var contact =
                    x.ContactEmail ??
                    x.PhoneNumber ??
                    "Chưa cập nhật";

                return new StoreRow(
                    x.Id,
                    x.Name,
                    x.Slug,
                    string.IsNullOrWhiteSpace(location)
                        ? "Chưa cập nhật"
                        : location,
                    contact,
                    x.ProductCount,
                    x.IsActive,
                    x.IsVerified,
                    x.ReliabilityScore,
                    StorePolicy.ReliabilityLabel(
                        x.ReliabilityScore),
                    x.DisplayOrder,
                    StorePolicy.CanDelete(
                        x.Id,
                        x.ProductCount));
            })
            .ToList();

        return new StoresViewModel
        {
            Items = rows,
            Form = form,
            Query = query,
            Active = active,
            TotalCount = stores.Count,
            ActiveCount =
                stores.Count(x => x.IsActive),
            VerifiedCount =
                stores.Count(x => x.IsVerified),
            ProductCount =
                stores.Sum(x => x.ProductCount)
        };
    }

    private async Task<string>
        CreateUniqueSlugAsync(
            string source,
            int? excludedId,
            CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(source);

        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "cua-hang";

        var slug = baseSlug;
        var suffix = 2;

        while (await _db.Stores.AnyAsync(
                   x =>
                       x.Slug == slug &&
                       (!excludedId.HasValue ||
                        x.Id != excludedId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static void Normalize(
        StoreFormViewModel form)
    {
        form.Name = form.Name?.Trim()
                    ?? string.Empty;
        form.Slug = Clean(form.Slug);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private string CurrentUserName() =>
        User.Identity?.Name ?? "Admin";
}
