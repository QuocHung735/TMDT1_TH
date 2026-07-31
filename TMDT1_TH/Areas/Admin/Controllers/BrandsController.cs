using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Infrastructure;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class BrandsController(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment) : Controller
{
    private static readonly string[] Tones =
        { "purple", "mint", "amber", "blue", "rose" };

    private static readonly string[] AllowedLogoExtensions =
        { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxLogoSize = 2 * 1024 * 1024;

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
                .FirstOrDefaultAsync(
                    x => x.Id == editId.Value,
                    cancellationToken);

            if (brand is null)
            {
                TempData["Error"] =
                    "Không tìm thấy thương hiệu cần chỉnh sửa.";

                return RedirectToAction(nameof(Index));
            }

            form = MapForm(brand);
            openModal = true;
        }

        return View(
            await BuildViewModelAsync(
                q,
                active,
                form,
                openModal,
                cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        BrandFormViewModel form,
        CancellationToken cancellationToken)
    {
        form.Id = null;
        NormalizeForm(form);

        await ValidateLogoAsync(
            form.LogoFile,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    true,
                    cancellationToken));
        }

        string? uploadedLogoUrl = null;

        try
        {
            if (form.LogoFile is { Length: > 0 })
            {
                uploadedLogoUrl =
                    await SaveLogoAsync(
                        form.LogoFile,
                        cancellationToken);
            }

            var slugSource =
                string.IsNullOrWhiteSpace(form.Slug)
                    ? form.Name
                    : form.Slug;

            var brand = new Brand
            {
                Name = form.Name,
                Slug = await CreateUniqueSlugAsync(
                    slugSource,
                    null,
                    cancellationToken),
                Country = Clean(form.Country),
                WebsiteUrl = Clean(form.WebsiteUrl),
                LogoUrl = uploadedLogoUrl,
                Description = Clean(form.Description),
                IsActive = form.IsActive,
                CreatedBy = CurrentUserName()
            };

            dbContext.Brands.Add(brand);
            await dbContext.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Đã tạo thương hiệu “{brand.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            DeleteLocalLogo(uploadedLogoUrl);

            ModelState.AddModelError(
                string.Empty,
                exception is DbUpdateException
                    ? "Không thể lưu thương hiệu. Tên hoặc slug có thể đã tồn tại."
                    : $"Không thể lưu thương hiệu: {exception.Message}");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    true,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        BrandFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            TempData["Error"] =
                "Thiếu mã thương hiệu cần chỉnh sửa.";

            return RedirectToAction(nameof(Index));
        }

        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(
                x => x.Id == form.Id.Value,
                cancellationToken);

        if (brand is null)
        {
            TempData["Error"] =
                "Thương hiệu không còn tồn tại.";

            return RedirectToAction(nameof(Index));
        }

        NormalizeForm(form);
        form.LogoUrl = brand.LogoUrl;

        await ValidateLogoAsync(
            form.LogoFile,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    true,
                    cancellationToken));
        }

        var oldLogoUrl = brand.LogoUrl;
        string? uploadedLogoUrl = null;

        try
        {
            if (form.LogoFile is { Length: > 0 })
            {
                uploadedLogoUrl =
                    await SaveLogoAsync(
                        form.LogoFile,
                        cancellationToken);

                brand.LogoUrl = uploadedLogoUrl;
            }
            else if (form.RemoveLogo)
            {
                brand.LogoUrl = null;
            }

            var slugSource =
                string.IsNullOrWhiteSpace(form.Slug)
                    ? form.Name
                    : form.Slug;

            brand.Name = form.Name;
            brand.Slug = await CreateUniqueSlugAsync(
                slugSource,
                brand.Id,
                cancellationToken);
            brand.Country = Clean(form.Country);
            brand.WebsiteUrl = Clean(form.WebsiteUrl);
            brand.Description = Clean(form.Description);
            brand.IsActive = form.IsActive;
            brand.UpdatedBy = CurrentUserName();

            await dbContext.SaveChangesAsync(cancellationToken);

            if (!string.Equals(
                    oldLogoUrl,
                    brand.LogoUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                DeleteLocalLogo(oldLogoUrl);
            }

            TempData["Success"] =
                $"Đã cập nhật thương hiệu “{brand.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            DeleteLocalLogo(uploadedLogoUrl);

            form.LogoUrl = oldLogoUrl;

            ModelState.AddModelError(
                string.Empty,
                exception is DbUpdateException
                    ? "Không thể cập nhật thương hiệu. Tên hoặc slug có thể đã tồn tại."
                    : $"Không thể cập nhật thương hiệu: {exception.Message}");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    form,
                    true,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(
        int id,
        CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (brand is null)
        {
            TempData["Error"] =
                "Không tìm thấy thương hiệu.";

            return RedirectToAction(nameof(Index));
        }

        brand.IsActive = !brand.IsActive;
        brand.UpdatedBy = CurrentUserName();

        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["Success"] = brand.IsActive
            ? $"Đã kích hoạt thương hiệu “{brand.Name}”."
            : $"Đã tạm ẩn thương hiệu “{brand.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (brand is null)
        {
            TempData["Error"] =
                "Không tìm thấy thương hiệu.";

            return RedirectToAction(nameof(Index));
        }

        if (brand.IsActive)
        {
            TempData["Error"] =
                "Hãy tạm ẩn thương hiệu trước khi xóa mềm. " +
                "Bước này giúp tránh xóa nhầm thương hiệu đang sử dụng.";

            return RedirectToAction(nameof(Index));
        }

        var hasProducts = await dbContext.Products
            .AnyAsync(
                x => x.BrandId == id,
                cancellationToken);

        if (hasProducts)
        {
            TempData["Error"] =
                "Không thể xóa thương hiệu đang có sản phẩm. " +
                "Hãy chuyển sản phẩm sang thương hiệu khác trước.";

            return RedirectToAction(nameof(Index));
        }

        var hasActivePromotion =
            await dbContext.PromotionBrands
                .AnyAsync(
                    x =>
                        x.BrandId == id &&
                        x.Promotion.IsActive,
                    cancellationToken);

        if (hasActivePromotion)
        {
            TempData["Error"] =
                "Không thể xóa thương hiệu đang được dùng bởi " +
                "khuyến mãi hoạt động. Hãy tắt hoặc chỉnh lại " +
                "khuyến mãi trước.";

            return RedirectToAction(nameof(Index));
        }

        brand.IsDeleted = true;
        brand.IsActive = false;
        brand.UpdatedBy = CurrentUserName();

        // Đây là xóa mềm nên giữ lại LogoUrl và tệp logo.
        // Dữ liệu có thể được kiểm tra hoặc phục hồi về sau.
        await dbContext.SaveChangesAsync(
            cancellationToken);

        TempData["Success"] =
            $"Đã xóa mềm thương hiệu “{brand.Name}”. " +
            "Thông tin và logo vẫn được giữ trong cơ sở dữ liệu.";

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

            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Slug.Contains(keyword) ||
                (x.Country != null &&
                 x.Country.Contains(keyword)));
        }

        if (active.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == active.Value);
        }

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

        var rows = items
            .Select(x => new BrandRow(
                x.Id,
                x.Name,
                x.Slug,
                x.Country ?? "Chưa cập nhật",
                x.WebsiteUrl,
                x.LogoUrl,
                x.ProductCount,
                x.IsActive
                    ? "Đang hoạt động"
                    : "Tạm ẩn",
                GetInitials(x.Name),
                Tones[Math.Abs(x.Id) % Tones.Length],
                x.IsActive))
            .ToList();

        return new BrandsViewModel
        {
            Items = rows,
            Form = form,
            Search = search,
            Active = active,
            TotalCount =
                await dbContext.Brands
                    .CountAsync(cancellationToken),
            ActiveCount =
                await dbContext.Brands
                    .CountAsync(
                        x => x.IsActive,
                        cancellationToken),
            ProductCount =
                await dbContext.Products
                    .CountAsync(cancellationToken),
            OpenFormModal = openModal
        };
    }

    private async Task ValidateLogoAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            return;

        var extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        if (!AllowedLogoExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(BrandFormViewModel.LogoFile),
                "Logo chỉ nhận JPG, JPEG, PNG hoặc WEBP.");

            return;
        }

        if (file.Length > MaxLogoSize)
        {
            ModelState.AddModelError(
                nameof(BrandFormViewModel.LogoFile),
                "Logo vượt quá dung lượng tối đa 2 MB.");

            return;
        }

        if (!await HasValidImageSignatureAsync(
                file,
                extension,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(BrandFormViewModel.LogoFile),
                "Nội dung tệp không đúng định dạng ảnh đã chọn.");
        }
    }

    private async Task<string> SaveLogoAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        var webRoot =
            environment.WebRootPath ??
            Path.Combine(
                environment.ContentRootPath,
                "wwwroot");

        var folder = Path.Combine(
            webRoot,
            "uploads",
            "brands");

        Directory.CreateDirectory(folder);

        var fileName =
            $"{Guid.NewGuid():N}{extension}";

        var physicalPath =
            Path.Combine(folder, fileName);

        await using var stream =
            new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

        await file.CopyToAsync(
            stream,
            cancellationToken);

        return $"/uploads/brands/{fileName}";
    }

    private void DeleteLocalLogo(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) ||
            !logoUrl.StartsWith(
                "/uploads/brands/",
                StringComparison.Ordinal))
        {
            return;
        }

        var webRoot =
            environment.WebRootPath ??
            Path.Combine(
                environment.ContentRootPath,
                "wwwroot");

        var uploadRoot =
            Path.GetFullPath(
                Path.Combine(
                    webRoot,
                    "uploads",
                    "brands"));

        var relativeFile =
            Path.GetFileName(logoUrl);

        if (string.IsNullOrWhiteSpace(relativeFile))
            return;

        var physicalPath =
            Path.GetFullPath(
                Path.Combine(
                    uploadRoot,
                    relativeFile));

        if (!physicalPath.StartsWith(
                uploadRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private static async Task<bool>
        HasValidImageSignatureAsync(
            IFormFile file,
            string extension,
            CancellationToken cancellationToken)
    {
        var header = new byte[12];

        await using var stream =
            file.OpenReadStream();

        var bytesRead =
            await stream.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);

        return extension switch
        {
            ".jpg" or ".jpeg" =>
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,

            ".png" =>
                bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A,

            ".webp" =>
                bytesRead >= 12 &&
                header[0] == 0x52 &&
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x45 &&
                header[10] == 0x42 &&
                header[11] == 0x50,

            _ => false
        };
    }

    private async Task<string> CreateUniqueSlugAsync(
        string source,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.Generate(source);

        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "thuong-hieu";

        var slug = baseSlug;
        var suffix = 2;

        while (await dbContext.Brands.AnyAsync(
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

    private static BrandFormViewModel MapForm(
        Brand brand) =>
        new()
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

    private static void NormalizeForm(
        BrandFormViewModel form)
    {
        form.Name =
            form.Name?.Trim() ??
            string.Empty;

        form.Slug =
            Clean(form.Slug);

        form.Country =
            Clean(form.Country);

        form.WebsiteUrl =
            Clean(form.WebsiteUrl);

        form.Description =
            Clean(form.Description);
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => "BR",
            1 => parts[0][
                ..Math.Min(2, parts[0].Length)]
                .ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}"
                .ToUpperInvariant()
        };
    }

    private string CurrentUserName() =>
        User.Identity?.Name ?? "Admin";

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
