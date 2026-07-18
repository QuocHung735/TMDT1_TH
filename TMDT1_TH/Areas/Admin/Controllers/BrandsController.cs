using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Infrastructure;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class BrandsController(ApplicationDbContext dbContext) : Controller
{
    private static readonly string[] Tones = ["purple", "mint", "amber", "blue", "rose"];

    public async Task<IActionResult> Index(
        string? q,
        bool? active,
        int? editId,
        CancellationToken cancellationToken)
    {
        var form = new BrandFormViewModel();
        var openModal = false;

        if (editId.HasValue)
        {
            var brand = await dbContext.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editId.Value, cancellationToken);

            if (brand is null)
            {
                TempData["Error"] = "Không tìm thấy thương hiệu cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            form = new BrandFormViewModel
            {
                Id = brand.Id,
                Name = brand.Name,
                Slug = brand.Slug,
                Country = brand.Country,
                WebsiteUrl = brand.WebsiteUrl,
                LogoUrl = brand.LogoUrl,
                Description = brand.Description,
                IsActive = brand.IsActive
            };
            openModal = true;
        }

        return View(await BuildViewModelAsync(q, active, form, openModal, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = null;
        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        var slugSource = string.IsNullOrWhiteSpace(form.Slug) ? form.Name : form.Slug;
        var brand = new Brand
        {
            Name = form.Name.Trim(),
            Slug = await CreateUniqueSlugAsync(slugSource, null, cancellationToken),
            Country = Clean(form.Country),
            WebsiteUrl = Clean(form.WebsiteUrl),
            LogoUrl = Clean(form.LogoUrl),
            Description = Clean(form.Description),
            IsActive = form.IsActive
        };

        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã tạo thương hiệu “{brand.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BrandFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            TempData["Error"] = "Thiếu mã thương hiệu cần chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        var brand = await dbContext.Brands.FirstOrDefaultAsync(x => x.Id == form.Id.Value, cancellationToken);
        if (brand is null)
        {
            TempData["Error"] = "Thương hiệu không còn tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        var slugSource = string.IsNullOrWhiteSpace(form.Slug) ? form.Name : form.Slug;
        brand.Name = form.Name.Trim();
        brand.Slug = await CreateUniqueSlugAsync(slugSource, brand.Id, cancellationToken);
        brand.Country = Clean(form.Country);
        brand.WebsiteUrl = Clean(form.WebsiteUrl);
        brand.LogoUrl = Clean(form.LogoUrl);
        brand.Description = Clean(form.Description);
        brand.IsActive = form.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã cập nhật thương hiệu “{brand.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (brand is null)
        {
            TempData["Error"] = "Không tìm thấy thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        brand.IsActive = !brand.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = brand.IsActive
            ? $"Đã kích hoạt thương hiệu “{brand.Name}”."
            : $"Đã tạm ẩn thương hiệu “{brand.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (brand is null)
        {
            TempData["Error"] = "Không tìm thấy thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        var hasProducts = await dbContext.Products.AnyAsync(x => x.BrandId == id, cancellationToken);
        if (hasProducts)
        {
            TempData["Error"] = "Không thể xóa thương hiệu đang có sản phẩm. Bạn có thể chuyển sang trạng thái tạm ẩn.";
            return RedirectToAction(nameof(Index));
        }

        brand.IsDeleted = true;
        brand.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã xóa thương hiệu “{brand.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BrandsViewModel> BuildViewModelAsync(
        string? search,
        bool? active,
        BrandFormViewModel form,
        bool openModal,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Brands.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Name.Contains(keyword)
                || x.Slug.Contains(keyword)
                || (x.Country != null && x.Country.Contains(keyword)));
        }

        if (active.HasValue)
            query = query.Where(x => x.IsActive == active.Value);

        var items = await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.Country,
                x.WebsiteUrl,
                x.LogoUrl,
                x.IsActive,
                ProductCount = x.Products.Count
            })
            .ToListAsync(cancellationToken);

        var rows = items.Select(x => new BrandRow(
            x.Id,
            x.Name,
            x.Slug,
            x.Country ?? "Chưa cập nhật",
            x.WebsiteUrl,
            x.LogoUrl,
            x.ProductCount,
            x.IsActive ? "Đang hoạt động" : "Tạm ẩn",
            GetInitials(x.Name),
            Tones[Math.Abs(x.Id) % Tones.Length],
            x.IsActive)).ToList();

        return new BrandsViewModel
        {
            Items = rows,
            Form = form,
            Search = search,
            Active = active,
            TotalCount = await dbContext.Brands.CountAsync(cancellationToken),
            ActiveCount = await dbContext.Brands.CountAsync(x => x.IsActive, cancellationToken),
            ProductCount = await dbContext.Products.CountAsync(cancellationToken),
            OpenFormModal = openModal
        };
    }

    private async Task<string> CreateUniqueSlugAsync(string source, int? excludedId, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(source);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "thuong-hieu";

        var slug = baseSlug;
        var suffix = 2;
        while (await dbContext.Brands.AnyAsync(
                   x => x.Slug == slug && (!excludedId.HasValue || x.Id != excludedId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "BR",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
