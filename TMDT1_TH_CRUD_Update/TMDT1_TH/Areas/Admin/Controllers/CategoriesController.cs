using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoriesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? keyword, bool? active)
    {
        var query = db.Categories.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Slug.Contains(keyword));
        }

        if (active.HasValue)
            query = query.Where(x => x.IsActive == active.Value);

        var categories = await query
            .Include(x => x.Parent)
            .Include(x => x.Products.Where(p => !p.IsDeleted))
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        ViewBag.Keyword = keyword;
        ViewBag.Active = active;
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
        => View("Form", await BuildFormAsync(new CategoryFormViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid)
            return View("Form", await BuildFormAsync(model));

        var slug = await MakeUniqueSlugAsync(model.Slug, model.Name);
        db.Categories.Add(new Category
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Description = model.Description?.Trim(),
            ImageUrl = model.ImageUrl?.Trim(),
            DisplayOrder = model.DisplayOrder,
            ParentId = model.ParentId,
            IsActive = model.IsActive
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo danh mục mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();

        return View("Form", await BuildFormAsync(new CategoryFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            DisplayOrder = entity.DisplayOrder,
            ParentId = entity.ParentId,
            IsActive = entity.IsActive
        }, id));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        await ValidateAsync(model, id);
        if (!ModelState.IsValid)
            return View("Form", await BuildFormAsync(model, id));

        var entity = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();

        entity.Name = model.Name.Trim();
        entity.Slug = await MakeUniqueSlugAsync(model.Slug, model.Name, id);
        entity.Description = model.Description?.Trim();
        entity.ImageUrl = model.ImageUrl?.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.ParentId = model.ParentId;
        entity.IsActive = model.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();
        entity.IsActive = !entity.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = entity.IsActive ? "Đã hiển thị danh mục." : "Đã ẩn danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Categories
            .Include(x => x.Children)
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return NotFound();

        if (entity.Children.Any(x => !x.IsDeleted) || entity.Products.Any(x => !x.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa danh mục đang có danh mục con hoặc sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa mềm danh mục.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CategoryFormViewModel> BuildFormAsync(CategoryFormViewModel model, int? excludedId = null)
    {
        model.ParentOptions = await db.Categories.AsNoTracking()
            .Where(x => !x.IsDeleted && (!excludedId.HasValue || x.Id != excludedId.Value))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync();
        return model;
    }

    private async Task ValidateAsync(CategoryFormViewModel model, int? id = null)
    {
        if (model.ParentId == id)
            ModelState.AddModelError(nameof(model.ParentId), "Danh mục không thể là cha của chính nó.");

        var normalizedName = model.Name.Trim();
        if (await db.Categories.AnyAsync(x => !x.IsDeleted && x.Id != id && x.Name == normalizedName))
            ModelState.AddModelError(nameof(model.Name), "Tên danh mục đã tồn tại.");
    }

    private async Task<string> MakeUniqueSlugAsync(string? slug, string name, int? id = null)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(slug) ? name : slug);
        var candidate = baseSlug;
        var number = 2;
        while (await db.Categories.AnyAsync(x => x.Id != id && x.Slug == candidate))
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
