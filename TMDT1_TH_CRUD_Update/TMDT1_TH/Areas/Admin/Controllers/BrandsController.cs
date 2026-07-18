using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class BrandsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? keyword, bool? active)
    {
        var query = db.Brands.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Slug.Contains(keyword));
        }
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);

        ViewBag.Keyword = keyword;
        ViewBag.Active = active;
        return View(await query.Include(x => x.Products.Where(p => !p.IsDeleted)).OrderBy(x => x.Name).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new BrandFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) return View("Form", model);

        db.Brands.Add(new Brand
        {
            Name = model.Name.Trim(),
            Slug = await MakeUniqueSlugAsync(model.Slug, model.Name),
            LogoUrl = model.LogoUrl?.Trim(),
            Description = model.Description?.Trim(),
            WebsiteUrl = model.WebsiteUrl?.Trim(),
            Country = model.Country?.Trim(),
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo thương hiệu mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await db.Brands.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();
        return View("Form", new BrandFormViewModel
        {
            Id = entity.Id, Name = entity.Name, Slug = entity.Slug, LogoUrl = entity.LogoUrl,
            Description = entity.Description, WebsiteUrl = entity.WebsiteUrl, Country = entity.Country,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BrandFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateAsync(model, id);
        if (!ModelState.IsValid) return View("Form", model);

        var entity = await db.Brands.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();
        entity.Name = model.Name.Trim();
        entity.Slug = await MakeUniqueSlugAsync(model.Slug, model.Name, id);
        entity.LogoUrl = model.LogoUrl?.Trim();
        entity.Description = model.Description?.Trim();
        entity.WebsiteUrl = model.WebsiteUrl?.Trim();
        entity.Country = model.Country?.Trim();
        entity.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thương hiệu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var entity = await db.Brands.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = entity.IsActive ? "Đã bật thương hiệu." : "Đã tạm ẩn thương hiệu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Brands.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();
        if (entity.Products.Any(x => !x.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa thương hiệu đang được gán cho sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        entity.IsDeleted = true;
        entity.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa mềm thương hiệu.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(BrandFormViewModel model, int? id = null)
    {
        var normalizedName = model.Name.Trim();
        if (await db.Brands.AnyAsync(x => !x.IsDeleted && x.Id != id && x.Name == normalizedName))
            ModelState.AddModelError(nameof(model.Name), "Tên thương hiệu đã tồn tại.");
    }

    private async Task<string> MakeUniqueSlugAsync(string? slug, string name, int? id = null)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(slug) ? name : slug);
        var candidate = baseSlug;
        var number = 2;
        while (await db.Brands.AnyAsync(x => x.Id != id && x.Slug == candidate))
            candidate = $"{baseSlug}-{number++}";
        return candidate;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        var ascii = builder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd');
        return Regex.Replace(Regex.Replace(ascii, "[^a-z0-9]+", "-"), "-+", "-").Trim('-');
    }
}
