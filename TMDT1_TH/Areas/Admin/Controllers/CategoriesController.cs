using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Infrastructure;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoriesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(
        string? q,
        bool? active,
        int? editId,
        CancellationToken cancellationToken)
    {
        var form = new CategoryFormViewModel();
        var openModal = false;

        if (editId.HasValue)
        {
            var category = await dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editId.Value, cancellationToken);

            if (category is null)
            {
                TempData["Error"] = "Không tìm thấy danh mục cần chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            form = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentId = category.ParentId,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive
            };
            openModal = true;
        }

        return View(await BuildViewModelAsync(q, active, form, openModal, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel form, CancellationToken cancellationToken)
    {
        form.Id = null;
        await ValidateParentAsync(form, cancellationToken);

        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        var slugSource = string.IsNullOrWhiteSpace(form.Slug) ? form.Name : form.Slug;
        var category = new Category
        {
            Name = form.Name.Trim(),
            Slug = await CreateUniqueSlugAsync(slugSource, null, cancellationToken),
            ParentId = form.ParentId,
            Description = Clean(form.Description),
            ImageUrl = Clean(form.ImageUrl),
            DisplayOrder = form.DisplayOrder,
            IsActive = form.IsActive
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã tạo danh mục “{category.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            TempData["Error"] = "Thiếu mã danh mục cần chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        var category = await dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == form.Id.Value, cancellationToken);

        if (category is null)
        {
            TempData["Error"] = "Danh mục không còn tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        await ValidateParentAsync(form, cancellationToken);
        if (!ModelState.IsValid)
            return View("Index", await BuildViewModelAsync(null, null, form, true, cancellationToken));

        var slugSource = string.IsNullOrWhiteSpace(form.Slug) ? form.Name : form.Slug;
        category.Name = form.Name.Trim();
        category.Slug = await CreateUniqueSlugAsync(slugSource, category.Id, cancellationToken);
        category.ParentId = form.ParentId;
        category.Description = Clean(form.Description);
        category.ImageUrl = Clean(form.ImageUrl);
        category.DisplayOrder = form.DisplayOrder;
        category.IsActive = form.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã cập nhật danh mục “{category.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            TempData["Error"] = "Không tìm thấy danh mục.";
            return RedirectToAction(nameof(Index));
        }

        category.IsActive = !category.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = category.IsActive
            ? $"Đã hiển thị danh mục “{category.Name}”."
            : $"Đã ẩn danh mục “{category.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            TempData["Error"] = "Không tìm thấy danh mục.";
            return RedirectToAction(nameof(Index));
        }

        var hasChildren = await dbContext.Categories.AnyAsync(x => x.ParentId == id, cancellationToken);
        var hasProducts = await dbContext.Products.AnyAsync(x => x.CategoryId == id, cancellationToken);

        if (hasChildren || hasProducts)
        {
            TempData["Error"] = hasChildren
                ? "Không thể xóa danh mục đang có danh mục con. Hãy di chuyển hoặc xóa danh mục con trước."
                : "Không thể xóa danh mục đang có sản phẩm. Bạn có thể chuyển sang trạng thái ẩn.";
            return RedirectToAction(nameof(Index));
        }

        category.IsDeleted = true;
        category.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Đã xóa danh mục “{category.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CategoriesViewModel> BuildViewModelAsync(
        string? search,
        bool? active,
        CategoryFormViewModel form,
        bool openModal,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Select(x => new CategoryData(
                x.Id,
                x.Name,
                x.Slug,
                x.ParentId,
                x.Parent != null ? x.Parent.Name : null,
                x.Products.Count,
                x.DisplayOrder,
                x.IsActive))
            .ToListAsync(cancellationToken);

        var flattened = Flatten(categories);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            flattened = flattened
                .Where(x => x.Name.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
                    || x.Slug.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (active.HasValue)
            flattened = flattened.Where(x => x.IsActive == active.Value).ToList();

        var rows = flattened.Select(x => new CategoryRow(
            x.Id,
            x.Name,
            x.Slug,
            x.ParentName ?? "—",
            x.ProductCount,
            x.IsActive ? "Đang hiển thị" : "Đã ẩn",
            x.Level,
            x.Level == 0 ? "bi-folder2-open" : "bi-folder2",
            x.DisplayOrder,
            x.IsActive)).ToList();

        var excludedIds = form.Id.HasValue
            ? GetDescendantIds(categories, form.Id.Value).Append(form.Id.Value).ToHashSet()
            : new HashSet<int>();

        var parentOptions = Flatten(categories)
            .Where(x => !excludedIds.Contains(x.Id))
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{new string('—', x.Level)}{(x.Level > 0 ? " " : string.Empty)}{x.Name}",
                Selected = form.ParentId == x.Id
            })
            .ToList();
        parentOptions.Insert(0, new SelectListItem("Không có (danh mục gốc)", string.Empty, !form.ParentId.HasValue));

        return new CategoriesViewModel
        {
            Items = rows,
            Form = form,
            ParentOptions = parentOptions,
            Search = search,
            Active = active,
            TotalCount = categories.Count,
            ActiveCount = categories.Count(x => x.IsActive),
            ProductCount = categories.Sum(x => x.ProductCount),
            OpenFormModal = openModal
        };
    }

    private async Task ValidateParentAsync(CategoryFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.ParentId.HasValue)
            return;

        if (form.Id.HasValue && form.ParentId.Value == form.Id.Value)
        {
            ModelState.AddModelError(nameof(form.ParentId), "Danh mục không thể là cha của chính nó.");
            return;
        }

        var parentExists = await dbContext.Categories.AnyAsync(x => x.Id == form.ParentId.Value, cancellationToken);
        if (!parentExists)
        {
            ModelState.AddModelError(nameof(form.ParentId), "Danh mục cha không tồn tại.");
            return;
        }

        if (!form.Id.HasValue)
            return;

        var currentParentId = form.ParentId;
        var visited = new HashSet<int>();
        while (currentParentId.HasValue && visited.Add(currentParentId.Value))
        {
            if (currentParentId.Value == form.Id.Value)
            {
                ModelState.AddModelError(nameof(form.ParentId), "Không thể chọn một danh mục con làm danh mục cha.");
                return;
            }

            currentParentId = await dbContext.Categories
                .Where(x => x.Id == currentParentId.Value)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private async Task<string> CreateUniqueSlugAsync(string source, int? excludedId, CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(source);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "danh-muc";

        var slug = baseSlug;
        var suffix = 2;
        while (await dbContext.Categories.AnyAsync(
                   x => x.Slug == slug && (!excludedId.HasValue || x.Id != excludedId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static List<CategoryTreeData> Flatten(IReadOnlyCollection<CategoryData> categories)
    {
        var children = categories
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.DisplayOrder).ThenBy(y => y.Name).ToList());
        var roots = categories
            .Where(x => !x.ParentId.HasValue)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();
        var result = new List<CategoryTreeData>();
        var visited = new HashSet<int>();

        void Visit(CategoryData item, int level)
        {
            if (!visited.Add(item.Id))
                return;

            result.Add(new CategoryTreeData(item, level));
            if (!children.TryGetValue(item.Id, out var childItems))
                return;

            foreach (var child in childItems)
                Visit(child, level + 1);
        }

        foreach (var root in roots)
            Visit(root, 0);

        foreach (var orphan in categories.Where(x => !visited.Contains(x.Id)).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name))
            Visit(orphan, 0);

        return result;
    }

    private static HashSet<int> GetDescendantIds(IReadOnlyCollection<CategoryData> categories, int parentId)
    {
        var result = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in categories.Where(x => x.ParentId == current))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CategoryData(
        int Id,
        string Name,
        string Slug,
        int? ParentId,
        string? ParentName,
        int ProductCount,
        int DisplayOrder,
        bool IsActive);

    private sealed record CategoryTreeData(CategoryData Item, int Level)
    {
        public int Id => Item.Id;
        public string Name => Item.Name;
        public string Slug => Item.Slug;
        public string? ParentName => Item.ParentName;
        public int ProductCount => Item.ProductCount;
        public int DisplayOrder => Item.DisplayOrder;
        public bool IsActive => Item.IsActive;
    }
}
