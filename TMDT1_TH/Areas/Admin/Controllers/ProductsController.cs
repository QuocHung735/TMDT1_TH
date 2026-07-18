using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductsController : Controller
{
    private static readonly string[] AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSize = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        int? categoryId,
        int? brandId,
        ProductStatus? status)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Variants)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Sku.Contains(keyword));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        if (brandId.HasValue)
            query = query.Where(x => x.BrandId == brandId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var products = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync();

        var now = DateTime.Now;
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var tones = new[] { "purple", "mint", "amber", "rose", "blue" };

        var rows = products.Select((product, index) =>
        {
            var stock = product.HasVariants
                ? product.Variants.Where(x => x.IsActive).Sum(x => x.StockQuantity)
                : product.StockQuantity;

            var currentPrice = product.PriceSchedules
                .Where(x => x.IsActive && x.ValidFrom <= now && (!x.ValidTo.HasValue || x.ValidTo.Value >= now))
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefault()
                ?? product.PriceSchedules
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.ValidFrom)
                    .FirstOrDefault();

            var imageUrl = product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();

            return new ProductRow(
                product.Id,
                product.Name,
                product.Sku,
                product.Category.Name,
                product.Brand.Name,
                product.Variants.Count(x => x.IsActive),
                currentPrice is null ? "Chưa có giá" : $"{currentPrice.SalePrice.ToString("N0", culture)}đ",
                stock,
                GetStatusLabel(product.Status, stock),
                GetInitials(product.Name),
                tones[index % tones.Length],
                imageUrl);
        }).ToList();

        var model = new ProductsViewModel
        {
            Items = rows,
            Query = q,
            CategoryId = categoryId,
            BrandId = brandId,
            Status = status,
            TotalCount = await _db.Products.CountAsync(),
            ActiveCount = await _db.Products.CountAsync(x => x.Status == ProductStatus.Active),
            DraftCount = await _db.Products.CountAsync(x => x.Status == ProductStatus.Draft),
            OutOfStockCount = await _db.Products.CountAsync(x =>
                x.Status == ProductStatus.OutOfStock ||
                (!x.HasVariants && x.StockQuantity == 0) ||
                (x.HasVariants && !x.Variants.Any(v => v.IsActive && v.StockQuantity > 0))),
            CategoryOptions = await BuildCategoryOptionsAsync(categoryId),
            BrandOptions = await BuildBrandOptionsAsync(brandId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var defaultMarketId = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        var model = new ProductEditorViewModel
        {
            MarketId = defaultMarketId,
            ValidFrom = DateTime.Now,
            Status = ProductStatus.Draft
        };

        await LoadEditorOptionsAsync(model);
        return View("Editor", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(x => x.Options)
                .ThenInclude(x => x.Values)
            .Include(x => x.Variants)
            .Include(x => x.Images)
            .Include(x => x.PriceSchedules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
            return NotFound();

        var options = product.Options
            .OrderBy(x => x.DisplayOrder)
            .Take(2)
            .ToList();

        var now = DateTime.Now;
        var price = product.PriceSchedules
            .Where(x => x.IsActive && x.ValidFrom <= now && (!x.ValidTo.HasValue || x.ValidTo.Value >= now))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault()
            ?? product.PriceSchedules
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefault();

        var model = new ProductEditorViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Status = product.Status,
            IsFeatured = product.IsFeatured,
            HasVariants = product.HasVariants,
            StockQuantity = product.StockQuantity,
            Weight = product.Weight,
            OptionName1 = options.ElementAtOrDefault(0)?.Name,
            OptionValues1 = JoinOptionValues(options.ElementAtOrDefault(0)),
            OptionName2 = options.ElementAtOrDefault(1)?.Name,
            OptionValues2 = JoinOptionValues(options.ElementAtOrDefault(1)),
            VariantStockQuantity = product.Variants.Where(x => x.IsActive).Select(x => x.StockQuantity).FirstOrDefault(),
            MarketId = price?.MarketId,
            CostPrice = price?.CostPrice ?? 0,
            ListPrice = price?.ListPrice ?? 0,
            SalePrice = price?.SalePrice ?? 0,
            ValidFrom = price?.ValidFrom ?? DateTime.Now,
            ValidTo = price?.ValidTo,
            PriceNote = price?.Note,
            ExistingPrimaryImageUrl = product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault()
        };

        if (!model.MarketId.HasValue)
        {
            model.MarketId = await _db.Markets
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        await LoadEditorOptionsAsync(model);
        return View("Editor", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductEditorViewModel model, string? mode)
    {
        if (string.Equals(mode, "draft", StringComparison.OrdinalIgnoreCase))
            model.Status = ProductStatus.Draft;

        await ValidateReferencesAsync(model);
        ValidateImage(model);

        var normalizedSku = NormalizeSku(model.Sku, model.Name);
        if (await _db.Products.AnyAsync(x => x.Id != model.Id && x.Sku == normalizedSku))
            ModelState.AddModelError(nameof(model.Sku), "Mã sản phẩm đã tồn tại.");

        if (!ModelState.IsValid)
        {
            model.Sku = normalizedSku;
            await LoadEditorOptionsAsync(model);
            return View("Editor", model);
        }

        string? uploadedImageUrl = null;
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            Product product;
            if (model.Id.HasValue)
            {
                product = await _db.Products.FirstOrDefaultAsync(x => x.Id == model.Id.Value)
                    ?? throw new InvalidOperationException("Sản phẩm không còn tồn tại.");
            }
            else
            {
                product = new Product();
                _db.Products.Add(product);
            }

            product.Name = model.Name.Trim();
            product.Slug = await CreateUniqueSlugAsync(model.Name, model.Id);
            product.Sku = normalizedSku;
            product.CategoryId = model.CategoryId!.Value;
            product.BrandId = model.BrandId!.Value;
            product.ShortDescription = NullIfWhiteSpace(model.ShortDescription);
            product.Description = NullIfWhiteSpace(model.Description);
            product.Status = model.Status;
            product.IsFeatured = model.IsFeatured;
            product.HasVariants = model.HasVariants;
            product.Weight = model.Weight;
            product.StockQuantity = model.HasVariants ? 0 : model.StockQuantity;
            product.UpdatedBy = CurrentUserName();

            await _db.SaveChangesAsync();

            if (model.PrimaryImageFile is not null)
            {
                uploadedImageUrl = await SaveImageAsync(model.PrimaryImageFile);
                var oldPrimaryImages = await _db.ProductImages
                    .Where(x => x.ProductId == product.Id && x.IsPrimary)
                    .ToListAsync();

                foreach (var image in oldPrimaryImages)
                    image.IsPrimary = false;

                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = uploadedImageUrl,
                    AltText = product.Name,
                    DisplayOrder = 0,
                    IsPrimary = true,
                    CreatedBy = CurrentUserName()
                });
            }

            await ReplaceVariantsAsync(product, model);
            await UpsertProductPriceAsync(product, model);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = model.Id.HasValue
                ? $"Đã cập nhật sản phẩm “{product.Name}”."
                : $"Đã tạo sản phẩm “{product.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            DeleteUploadedFile(uploadedImageUrl);
            ModelState.AddModelError(string.Empty, GetDatabaseMessage(exception));
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            DeleteUploadedFile(uploadedImageUrl);
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        await LoadEditorOptionsAsync(model);
        return View("Editor", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (product is null)
            return NotFound();

        product.Status = product.Status == ProductStatus.Active
            ? ProductStatus.Inactive
            : ProductStatus.Active;
        product.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync();
        TempData["Success"] = product.Status == ProductStatus.Active
            ? $"Đã bật bán sản phẩm “{product.Name}”."
            : $"Đã tạm ẩn sản phẩm “{product.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .Include(x => x.PriceSchedules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
            return NotFound();

        product.IsDeleted = true;
        product.Status = ProductStatus.Discontinued;
        product.UpdatedBy = CurrentUserName();

        foreach (var variant in product.Variants)
        {
            variant.IsDeleted = true;
            variant.IsActive = false;
            variant.IsDefault = false;
            variant.UpdatedBy = CurrentUserName();

            foreach (var price in variant.PriceSchedules)
                price.IsActive = false;
        }

        foreach (var price in product.PriceSchedules)
            price.IsActive = false;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa sản phẩm “{product.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateReferencesAsync(ProductEditorViewModel model)
    {
        var categoryExists = model.CategoryId.HasValue && await _db.Categories
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.CategoryId.Value && x.IsActive);

        var brandExists = model.BrandId.HasValue && await _db.Brands
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.BrandId.Value && x.IsActive);

        var marketExists = model.MarketId.HasValue && await _db.Markets
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.MarketId.Value && x.IsActive);

        if (!categoryExists)
            ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại hoặc đã bị ẩn.");

        if (!brandExists)
            ModelState.AddModelError(nameof(model.BrandId), "Thương hiệu không tồn tại hoặc đã bị ẩn.");

        if (!marketExists)
            ModelState.AddModelError(nameof(model.MarketId), "Thị trường không tồn tại hoặc đã bị tắt.");
    }

    private void ValidateImage(ProductEditorViewModel model)
    {
        if (model.PrimaryImageFile is null)
            return;

        var extension = Path.GetExtension(model.PrimaryImageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
            ModelState.AddModelError(nameof(model.PrimaryImageFile), "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");

        if (model.PrimaryImageFile.Length <= 0 || model.PrimaryImageFile.Length > MaxImageSize)
            ModelState.AddModelError(nameof(model.PrimaryImageFile), "Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
    }

    private async Task ReplaceVariantsAsync(Product product, ProductEditorViewModel model)
    {
        var allVariantIds = await _db.ProductVariants
            .IgnoreQueryFilters()
            .Where(x => x.ProductId == product.Id)
            .Select(x => x.Id)
            .ToListAsync();

        if (allVariantIds.Count > 0)
        {
            var oldLinks = await _db.ProductVariantValues
                .Where(x => allVariantIds.Contains(x.ProductVariantId))
                .ToListAsync();
            _db.ProductVariantValues.RemoveRange(oldLinks);

            var activeVariants = await _db.ProductVariants
                .Where(x => x.ProductId == product.Id)
                .ToListAsync();

            foreach (var variant in activeVariants)
            {
                variant.IsDeleted = true;
                variant.IsActive = false;
                variant.IsDefault = false;
                variant.UpdatedBy = CurrentUserName();
            }

            await _db.SaveChangesAsync();
        }

        var oldOptions = await _db.ProductOptions
            .Where(x => x.ProductId == product.Id)
            .Include(x => x.Values)
            .ToListAsync();

        if (oldOptions.Count > 0)
        {
            _db.ProductOptionValues.RemoveRange(oldOptions.SelectMany(x => x.Values));
            _db.ProductOptions.RemoveRange(oldOptions);
            await _db.SaveChangesAsync();
        }

        if (!model.HasVariants)
        {
            product.StockQuantity = model.StockQuantity;
            return;
        }

        var definitions = BuildOptionDefinitions(model);
        var optionEntities = new List<ProductOption>();

        for (var optionIndex = 0; optionIndex < definitions.Count; optionIndex++)
        {
            var definition = definitions[optionIndex];
            var option = new ProductOption
            {
                ProductId = product.Id,
                Name = definition.Name,
                DisplayOrder = optionIndex,
                CreatedBy = CurrentUserName()
            };

            for (var valueIndex = 0; valueIndex < definition.Values.Count; valueIndex++)
            {
                option.Values.Add(new ProductOptionValue
                {
                    Value = definition.Values[valueIndex],
                    DisplayOrder = valueIndex,
                    CreatedBy = CurrentUserName()
                });
            }

            optionEntities.Add(option);
        }

        _db.ProductOptions.AddRange(optionEntities);
        await _db.SaveChangesAsync();

        var groups = optionEntities
            .OrderBy(x => x.DisplayOrder)
            .Select(option => option.Values
                .OrderBy(value => value.DisplayOrder)
                .Select(value => new OptionValuePair(option, value))
                .ToList())
            .ToList();

        var combinations = BuildCombinations(groups);
        var variants = new List<ProductVariant>();

        for (var index = 0; index < combinations.Count; index++)
        {
            var combination = combinations[index];
            var valueText = string.Join(" / ", combination.Select(x => x.Value.Value));
            var combinationKey = string.Join("|", combination.Select(x =>
                $"{x.Option.Name.Trim().ToUpperInvariant()}={x.Value.Value.Trim().ToUpperInvariant()}"));

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Name = valueText,
                CombinationKey = combinationKey,
                Sku = BuildVariantSku(product.Sku, combination, index),
                StockQuantity = model.VariantStockQuantity,
                Weight = model.Weight,
                IsDefault = index == 0,
                IsActive = true,
                CreatedBy = CurrentUserName()
            };

            foreach (var pair in combination)
            {
                variant.VariantValues.Add(new ProductVariantValue
                {
                    ProductOptionValueId = pair.Value.Id
                });
            }

            variants.Add(variant);
        }

        _db.ProductVariants.AddRange(variants);
        product.StockQuantity = variants.Sum(x => x.StockQuantity);
    }

    private async Task UpsertProductPriceAsync(Product product, ProductEditorViewModel model)
    {
        var schedule = await _db.PriceSchedules
            .Where(x => x.ProductId == product.Id &&
                        x.ProductVariantId == null &&
                        x.MarketId == model.MarketId!.Value &&
                        x.IsActive)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefaultAsync(x =>
                x.ValidFrom <= model.ValidFrom &&
                (!x.ValidTo.HasValue || x.ValidTo.Value >= model.ValidFrom));

        if (schedule is null)
        {
            schedule = new PriceSchedule
            {
                ProductId = product.Id,
                MarketId = model.MarketId.Value,
                IsActive = true,
                CreatedBy = CurrentUserName()
            };
            _db.PriceSchedules.Add(schedule);
        }

        schedule.CostPrice = model.CostPrice;
        schedule.ListPrice = model.ListPrice;
        schedule.SalePrice = model.SalePrice;
        schedule.ValidFrom = model.ValidFrom;
        schedule.ValidTo = model.ValidTo;
        schedule.Note = NullIfWhiteSpace(model.PriceNote);
        schedule.UpdatedBy = CurrentUserName();
    }

    private async Task LoadEditorOptionsAsync(ProductEditorViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.BrandOptions = await BuildBrandOptionsAsync(model.BrandId);

        model.MarketOptions = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Name} ({x.CurrencyCode})",
                Selected = model.MarketId == x.Id
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategoryOptionsAsync(int? selectedId)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                ParentName = x.Parent != null ? x.Parent.Name : null
            })
            .ToListAsync();

        return categories.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = string.IsNullOrWhiteSpace(x.ParentName) ? x.Name : $"{x.ParentName} › {x.Name}",
            Selected = selectedId == x.Id
        }).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBrandOptionsAsync(int? selectedId)
    {
        return await _db.Brands
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = selectedId == x.Id
            })
            .ToListAsync();
    }

    private async Task<string> CreateUniqueSlugAsync(string name, int? currentId)
    {
        var baseSlug = SlugHelper.Generate(name);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "san-pham";

        var slug = baseSlug;
        var suffix = 2;
        while (await _db.Products.AnyAsync(x => x.Id != currentId && x.Slug == slug))
            slug = $"{baseSlug}-{suffix++}";

        return slug;
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(physicalPath);
        await file.CopyToAsync(stream);
        return $"/uploads/products/{fileName}";
    }

    private void DeleteUploadedFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/products/", StringComparison.Ordinal))
            return;

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);
        if (System.IO.File.Exists(physicalPath))
            System.IO.File.Delete(physicalPath);
    }

    private static IReadOnlyList<OptionDefinition> BuildOptionDefinitions(ProductEditorViewModel model)
    {
        var result = new List<OptionDefinition>();
        var firstValues = SplitValues(model.OptionValues1);
        if (!string.IsNullOrWhiteSpace(model.OptionName1) && firstValues.Count > 0)
            result.Add(new OptionDefinition(model.OptionName1.Trim(), firstValues));

        var secondValues = SplitValues(model.OptionValues2);
        if (!string.IsNullOrWhiteSpace(model.OptionName2) && secondValues.Count > 0)
            result.Add(new OptionDefinition(model.OptionName2.Trim(), secondValues));

        return result;
    }

    private static IReadOnlyList<string> SplitValues(string? values) =>
        (values ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<List<OptionValuePair>> BuildCombinations(IReadOnlyList<List<OptionValuePair>> groups) 
    {
        var combinations = new List<List<OptionValuePair>> { new List<OptionValuePair>() };
        foreach (var group in groups)
        {
            combinations = combinations
                .SelectMany(existing => group.Select(item => existing.Append(item).ToList()))
                .ToList();
        }

        return combinations;
    }

    private static string BuildVariantSku(
        string baseSku,
        IReadOnlyList<OptionValuePair> combination,
        int index)
    {
        var suffix = string.Join("-", combination.Select(x => ToSkuToken(x.Value.Value)));
        var ending = $"-{index + 1:00}";
        var maxBaseLength = Math.Max(1, 100 - suffix.Length - ending.Length - 1);
        var safeBase = baseSku.Length > maxBaseLength ? baseSku[..maxBaseLength] : baseSku;
        return $"{safeBase}-{suffix}{ending}".ToUpperInvariant();
    }

    private static string ToSkuToken(string value)
    {
        var token = SlugHelper.Generate(value).Replace("-", string.Empty).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(token) ? "VAR" : token[..Math.Min(token.Length, 12)];
    }

    private static string NormalizeSku(string? sku, string productName)
    {
        if (!string.IsNullOrWhiteSpace(sku))
            return sku.Trim().ToUpperInvariant();

        var token = SlugHelper.Generate(productName).Replace("-", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(token))
            token = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        return $"HOME-{token[..Math.Min(token.Length, 24)]}";
    }

    private static string GetStatusLabel(ProductStatus status, int stock)
    {
        if (status == ProductStatus.Active && stock <= 0)
            return "Hết hàng";

        if (status == ProductStatus.Active && stock <= 10)
            return "Sắp hết";

        return status switch
        {
            ProductStatus.Active => "Đang bán",
            ProductStatus.Draft => "Bản nháp",
            ProductStatus.Inactive => "Tạm ẩn",
            ProductStatus.OutOfStock => "Hết hàng",
            ProductStatus.Discontinued => "Ngừng kinh doanh",
            _ => "Không xác định"
        };
    }

    private static string GetInitials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return "SP";

        return string.Concat(words.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }

    private static string JoinOptionValues(ProductOption? option) =>
        option is null
            ? string.Empty
            : string.Join(", ", option.Values.OrderBy(x => x.DisplayOrder).Select(x => x.Value));

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string CurrentUserName() => User.Identity?.Name ?? "Admin";

    private static string GetDatabaseMessage(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        if (message.Contains("51001", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("chồng lấn", StringComparison.OrdinalIgnoreCase))
        {
            return "Khoảng thời gian giá đang chồng lấn với lịch giá khác của cùng sản phẩm và thị trường.";
        }

        if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return "Dữ liệu bị trùng. Hãy kiểm tra lại SKU, slug hoặc tổ hợp biến thể.";
        }

        return "Không thể lưu sản phẩm. Hãy kiểm tra dữ liệu và thử lại.";
    }

    private sealed record OptionDefinition(string Name, IReadOnlyList<string> Values);
    private sealed record OptionValuePair(ProductOption Option, ProductOptionValue Value);
}
