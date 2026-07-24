using System.Globalization;
using System.Text.RegularExpressions;
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
    private static readonly string[] ForbiddenTitleTerms = new[]
    {
        "giảm giá", "sale off", "freeship", "miễn phí vận chuyển", "bán chạy", "sản phẩm hot",
        "shopee", "tiktok shop", "lazada", "http://", "https://", "www."
    };

    private const long MaxImageSize = 5 * 1024 * 1024;
    private const int MaxImages = 9;
    private const int MaxOptionValues = 20;
    private const int MaxVariants = 100;

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
        var defaultMarketId = await GetDefaultMarketIdAsync();
        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Images)
            .Include(x => x.Specifications)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Sku.Contains(keyword) ||
                (x.ModelNumber != null && x.ModelNumber.Contains(keyword)) ||
                x.Variants.Any(v => v.Sku.Contains(keyword) || (v.Barcode != null && v.Barcode.Contains(keyword))));
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
            var activeVariants = product.Variants.Where(x => x.IsActive && !x.IsDeleted).ToList();
            var stock = product.HasVariants
                ? activeVariants.Sum(x => x.StockQuantity)
                : product.StockQuantity;

            var currentPrices = product.HasVariants
                ? activeVariants
                    .SelectMany(x => x.PriceSchedules)
                    .Where(x => x.IsActive &&
                                (!defaultMarketId.HasValue || x.MarketId == defaultMarketId.Value) &&
                                x.ValidFrom <= now &&
                                (!x.ValidTo.HasValue || x.ValidTo.Value >= now))
                    .Select(x => x.SalePrice)
                    .ToList()
                : product.PriceSchedules
                    .Where(x => x.IsActive &&
                                (!defaultMarketId.HasValue || x.MarketId == defaultMarketId.Value) &&
                                x.ValidFrom <= now &&
                                (!x.ValidTo.HasValue || x.ValidTo.Value >= now))
                    .Select(x => x.SalePrice)
                    .ToList();

            var imageUrl = product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();

            var issues = GetListingIssues(product, defaultMarketId, now);
            var listingScore = CalculateListingScore(product, defaultMarketId, now);

            return new ProductRow(
                product.Id,
                product.Name,
                product.Sku,
                product.Category.Name,
                product.Brand.Name,
                activeVariants.Count,
                FormatPriceRange(currentPrices, culture),
                stock,
                GetStatusLabel(product.Status, stock),
                GetInitials(product.Name),
                tones[index % tones.Length],
                imageUrl,
                listingScore,
                issues.Count);
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
        var model = new ProductEditorViewModel
        {
            MarketId = await GetDefaultMarketIdAsync(),
            ValidFrom = DateTime.Now,
            Status = ProductStatus.Draft,
            Unit = "Cái",
            MinPurchaseQuantity = 1,
            LowStockThreshold = 5,
            Specifications = new List<ProductSpecificationEditorItem>
            {
                new ProductSpecificationEditorItem()
            }
        };

        await LoadEditorStateAsync(model);
        return View("Editor", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int? marketId)
    {
        var product = await BuildProductEditorQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
            return NotFound();

        var selectedMarketId = marketId ?? await GetDefaultMarketIdAsync();
        var model = BuildEditorModel(product, selectedMarketId);
        await LoadEditorStateAsync(model);
        return View("Editor", model);
    }

    [HttpGet]
    public async Task<IActionResult> PricesForMarket(int productId, int marketId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product is null)
            return NotFound();

        var productPrice = SelectPreferredSchedule(product.PriceSchedules, marketId);
        var variants = product.Variants
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x =>
            {
                var price = SelectPreferredSchedule(x.PriceSchedules, marketId);
                return new
                {
                    variantId = x.Id,
                    priceScheduleId = price?.Id,
                    costPrice = price?.CostPrice ?? 0,
                    listPrice = price?.ListPrice ?? 0,
                    salePrice = price?.SalePrice ?? 0
                };
            })
            .ToList();

        return Json(new
        {
            product = new
            {
                priceScheduleId = productPrice?.Id,
                costPrice = productPrice?.CostPrice ?? 0,
                listPrice = productPrice?.ListPrice ?? 0,
                salePrice = productPrice?.SalePrice ?? 0,
                validFrom = (productPrice?.ValidFrom ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm"),
                validTo = productPrice?.ValidTo?.ToString("yyyy-MM-ddTHH:mm"),
                note = productPrice?.Note
            },
            variants
        });
    }

    [HttpGet]
    public async Task<IActionResult> GenerateCodes(
        string? name,
        int? categoryId,
        int? brandId,
        int? productId)
    {
        if (productId.HasValue)
        {
            var existing = await _db.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Id == productId.Value)
                .Select(x => new
                {
                    x.Name,
                    x.CategoryId,
                    x.BrandId,
                    x.Sku,
                    x.ModelNumber
                })
                .FirstOrDefaultAsync();

            if (existing is null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            if (!string.IsNullOrWhiteSpace(existing.ModelNumber))
            {
                return Json(new
                {
                    sku = existing.Sku,
                    modelNumber = existing.ModelNumber
                });
            }

            var missingModelSuggestion = await GenerateUniqueProductCodesAsync(
                existing.Name,
                existing.CategoryId,
                existing.BrandId,
                productId.Value);

            return Json(new
            {
                sku = existing.Sku,
                modelNumber = missingModelSuggestion.ModelNumber
            });
        }

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Vui lòng nhập tên sản phẩm trước." });

        if (!categoryId.HasValue || !brandId.HasValue)
            return BadRequest(new { message = "Vui lòng chọn danh mục và thương hiệu." });

        try
        {
            var suggestion = await GenerateUniqueProductCodesAsync(
                name.Trim(),
                categoryId.Value,
                brandId.Value,
                null);

            return Json(new
            {
                sku = suggestion.Sku,
                modelNumber = suggestion.ModelNumber
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductEditorViewModel model, string? mode)
    {
        var saveAsDraft = string.Equals(mode, "draft", StringComparison.OrdinalIgnoreCase);
        if (saveAsDraft)
            model.Status = ProductStatus.Draft;

        NormalizeEditorModel(model);
        await EnsureSystemManagedProductCodesAsync(model);

        // SKU và mã model do server quản lý. Xóa lỗi binding từ giá trị trống
        // hoặc giá trị đã bị sửa ở phía trình duyệt.
        ModelState.Remove(nameof(model.Sku));
        ModelState.Remove(nameof(model.ModelNumber));

        EnsureVariantRows(model);
        await ValidateProductAsync(model, saveAsDraft);
        await ValidateSkuUniquenessAsync(model);
        await ValidateModelNumberUniquenessAsync(model);

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty,
                "Sản phẩm chưa được lưu. Vui lòng kiểm tra các trường được đánh dấu lỗi.");
            await LoadEditorStateAsync(model);
            return View("Editor", model);
        }

        var uploadedUrls = new List<string>();
        var filesToDeleteAfterCommit = new List<string>();

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var product = model.Id.HasValue
                ? await _db.Products.FirstOrDefaultAsync(x => x.Id == model.Id.Value)
                    ?? throw new InvalidOperationException("Sản phẩm không còn tồn tại.")
                : new Product();

            if (!model.Id.HasValue)
                _db.Products.Add(product);

            await ApplyProductFieldsAsync(product, model);
            await _db.SaveChangesAsync();

            await SyncImagesAsync(product, model, uploadedUrls, filesToDeleteAfterCommit);
            await SyncSpecificationsAsync(product, model);
            var syncedVariants = await SyncOptionsAndVariantsAsync(product, model);
            await SyncPricesAsync(product, model, syncedVariants);

            var totalStock = product.HasVariants
                ? syncedVariants.Where(x => x.Entity.IsActive).Sum(x => x.Entity.StockQuantity)
                : product.StockQuantity;

            if (product.Status == ProductStatus.Active && totalStock == 0)
                product.Status = ProductStatus.OutOfStock;
            else if (product.Status == ProductStatus.OutOfStock && totalStock > 0)
                product.Status = ProductStatus.Active;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            foreach (var imageUrl in filesToDeleteAfterCommit.Distinct(StringComparer.OrdinalIgnoreCase))
                await DeleteUploadedFileIfUnusedAsync(imageUrl);

            TempData["Success"] = model.Id.HasValue
                ? $"Đã cập nhật sản phẩm “{product.Name}”."
                : $"Đã tạo sản phẩm “{product.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            DeleteUploadedFiles(uploadedUrls);
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, GetDatabaseMessage(exception));
        }
        catch (Exception exception)
        {
            DeleteUploadedFiles(uploadedUrls);
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, $"Không thể lưu sản phẩm: {exception.Message}");
        }

        await LoadEditorStateAsync(model);
        return View("Editor", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var product = await BuildProductEditorQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product is null)
            return NotFound();

        if (product.Status == ProductStatus.Active)
        {
            product.Status = ProductStatus.Inactive;
            product.UpdatedBy = CurrentUserName();
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã tạm ẩn sản phẩm “{product.Name}”.";
            return RedirectToAction(nameof(Index));
        }

        var defaultMarketId = await GetDefaultMarketIdAsync();
        var issues = GetListingIssues(product, defaultMarketId, DateTime.Now);
        if (issues.Count > 0)
        {
            TempData["Error"] = "Chưa thể bật bán: " + string.Join("; ", issues.Take(5));
            return RedirectToAction(nameof(Edit), new { id });
        }

        var totalStock = product.HasVariants
            ? product.Variants.Where(x => x.IsActive).Sum(x => x.StockQuantity)
            : product.StockQuantity;
        product.Status = totalStock > 0 ? ProductStatus.Active : ProductStatus.OutOfStock;
        product.UpdatedBy = CurrentUserName();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái bán cho “{product.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id)
    {
        var source = await BuildProductEditorQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (source is null)
            return NotFound();

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var copyName = source.Name + " - Bản sao";
        var copyCodes = await GenerateUniqueProductCodesAsync(
            copyName,
            source.CategoryId,
            source.BrandId,
            null);

        var copy = new Product
        {
            CategoryId = source.CategoryId,
            BrandId = source.BrandId,
            Name = copyName,
            Slug = await CreateUniqueSlugAsync(copyName, null),
            Sku = copyCodes.Sku,
            ModelNumber = copyCodes.ModelNumber,
            Unit = source.Unit,
            ShortDescription = source.ShortDescription,
            Description = source.Description,
            CountryOfOrigin = source.CountryOfOrigin,
            ManufacturerName = source.ManufacturerName,
            ManufacturerAddress = source.ManufacturerAddress,
            WarrantyMonths = source.WarrantyMonths,
            Status = ProductStatus.Draft,
            IsFeatured = false,
            HasVariants = source.HasVariants,
            StockQuantity = 0,
            LowStockThreshold = source.LowStockThreshold,
            MinPurchaseQuantity = source.MinPurchaseQuantity,
            MaxPurchaseQuantity = source.MaxPurchaseQuantity,
            Weight = source.Weight,
            PackageLengthCm = source.PackageLengthCm,
            PackageWidthCm = source.PackageWidthCm,
            PackageHeightCm = source.PackageHeightCm,
            CreatedBy = CurrentUserName()
        };
        _db.Products.Add(copy);
        await _db.SaveChangesAsync();

        foreach (var image in source.Images.OrderBy(x => x.DisplayOrder))
        {
            copy.Images.Add(new ProductImage
            {
                ImageUrl = image.ImageUrl,
                AltText = copy.Name,
                DisplayOrder = image.DisplayOrder,
                IsPrimary = image.IsPrimary,
                CreatedBy = CurrentUserName()
            });
        }

        foreach (var specification in source.Specifications.OrderBy(x => x.DisplayOrder))
        {
            copy.Specifications.Add(new ProductSpecification
            {
                Name = specification.Name,
                Value = specification.Value,
                DisplayOrder = specification.DisplayOrder,
                CreatedBy = CurrentUserName()
            });
        }

        var optionMap = new Dictionary<int, ProductOptionValue>();
        foreach (var option in source.Options.OrderBy(x => x.DisplayOrder))
        {
            var newOption = new ProductOption
            {
                Name = option.Name,
                DisplayOrder = option.DisplayOrder,
                CreatedBy = CurrentUserName()
            };
            copy.Options.Add(newOption);

            foreach (var value in option.Values.OrderBy(x => x.DisplayOrder))
            {
                var newValue = new ProductOptionValue
                {
                    Value = value.Value,
                    ColorCode = value.ColorCode,
                    DisplayOrder = value.DisplayOrder,
                    CreatedBy = CurrentUserName()
                };
                newOption.Values.Add(newValue);
                optionMap[value.Id] = newValue;
            }
        }

        await _db.SaveChangesAsync();

        foreach (var variant in source.Variants.Where(x => x.IsActive).OrderBy(x => x.SortOrder))
        {
            var newVariant = new ProductVariant
            {
                ProductId = copy.Id,
                Name = variant.Name,
                CombinationKey = variant.CombinationKey,
                Sku = await CreateUniqueVariantSkuAsync(variant.Sku + "-COPY"),
                Barcode = null,
                StockQuantity = 0,
                LowStockThreshold = variant.LowStockThreshold,
                SortOrder = variant.SortOrder,
                Weight = variant.Weight,
                IsDefault = variant.IsDefault,
                IsActive = true,
                CreatedBy = CurrentUserName()
            };

            foreach (var link in variant.VariantValues)
            {
                if (optionMap.TryGetValue(link.ProductOptionValueId, out var newValue))
                {
                    newVariant.VariantValues.Add(new ProductVariantValue
                    {
                        ProductOptionValue = newValue
                    });
                }
            }
            _db.ProductVariants.Add(newVariant);
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã nhân bản “{source.Name}” thành bản nháp.";
        return RedirectToAction(nameof(Edit), new { id = copy.Id });
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
        TempData["Success"] = $"Đã xóa mềm sản phẩm “{product.Name}”.";
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<Product> BuildProductEditorQuery()
    {
        return _db.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Images)
            .Include(x => x.Specifications)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Options)
                .ThenInclude(x => x.Values)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.VariantValues)
                    .ThenInclude(x => x.ProductOptionValue)
                        .ThenInclude(x => x.ProductOption)
            .AsSplitQuery();
    }

    private ProductEditorViewModel BuildEditorModel(Product product, int? marketId)
    {
        var options = product.Options.OrderBy(x => x.DisplayOrder).Take(2).ToList();
        var productPrice = marketId.HasValue
            ? SelectPreferredSchedule(product.PriceSchedules, marketId.Value)
            : null;

        var model = new ProductEditorViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            ModelNumber = product.ModelNumber,
            Unit = product.Unit,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            CountryOfOrigin = product.CountryOfOrigin,
            ManufacturerName = product.ManufacturerName,
            ManufacturerAddress = product.ManufacturerAddress,
            WarrantyMonths = product.WarrantyMonths,
            Status = product.Status,
            IsFeatured = product.IsFeatured,
            HasVariants = product.HasVariants,
            StockQuantity = product.StockQuantity,
            LowStockThreshold = product.LowStockThreshold,
            MinPurchaseQuantity = product.MinPurchaseQuantity,
            MaxPurchaseQuantity = product.MaxPurchaseQuantity,
            Weight = product.Weight,
            PackageLengthCm = product.PackageLengthCm,
            PackageWidthCm = product.PackageWidthCm,
            PackageHeightCm = product.PackageHeightCm,
            OptionName1 = options.ElementAtOrDefault(0)?.Name,
            OptionValues1 = JoinOptionValues(options.ElementAtOrDefault(0)),
            OptionName2 = options.ElementAtOrDefault(1)?.Name,
            OptionValues2 = JoinOptionValues(options.ElementAtOrDefault(1)),
            MarketId = marketId,
            ProductPriceScheduleId = productPrice?.Id,
            CostPrice = productPrice?.CostPrice ?? 0,
            ListPrice = productPrice?.ListPrice ?? 0,
            SalePrice = productPrice?.SalePrice ?? 0,
            ValidFrom = productPrice?.ValidFrom ?? DateTime.Now,
            ValidTo = productPrice?.ValidTo,
            PriceNote = productPrice?.Note,
            ExistingImages = product.Images
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ProductImageEditorItem
                {
                    Id = x.Id,
                    ImageUrl = x.ImageUrl,
                    IsPrimary = x.IsPrimary,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList(),
            PrimaryImageId = product.Images.FirstOrDefault(x => x.IsPrimary)?.Id,
            Specifications = product.Specifications
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ProductSpecificationEditorItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Value = x.Value
                })
                .ToList()
        };

        if (model.Specifications.Count == 0)
            model.Specifications.Add(new ProductSpecificationEditorItem());

        foreach (var variant in product.Variants.Where(x => x.IsActive).OrderBy(x => x.SortOrder))
        {
            var orderedValues = variant.VariantValues
                .OrderBy(x => x.ProductOptionValue.ProductOption.DisplayOrder)
                .Select(x => x.ProductOptionValue.Value)
                .ToList();
            var price = marketId.HasValue ? SelectPreferredSchedule(variant.PriceSchedules, marketId.Value) : null;
            model.Variants.Add(new ProductVariantEditorItem
            {
                Id = variant.Id,
                PriceScheduleId = price?.Id,
                CombinationKey = variant.CombinationKey,
                Value1 = orderedValues.ElementAtOrDefault(0) ?? string.Empty,
                Value2 = orderedValues.ElementAtOrDefault(1),
                Name = variant.Name,
                Sku = variant.Sku,
                Barcode = variant.Barcode,
                StockQuantity = variant.StockQuantity,
                LowStockThreshold = variant.LowStockThreshold,
                Weight = variant.Weight,
                CostPrice = price?.CostPrice ?? 0,
                ListPrice = price?.ListPrice ?? 0,
                SalePrice = price?.SalePrice ?? 0,
                IsDefault = variant.IsDefault,
                IsActive = variant.IsActive
            });
        }

        return model;
    }

    private async Task ValidateProductAsync(ProductEditorViewModel model, bool saveAsDraft)
    {
        var publishing = !saveAsDraft && model.Status != ProductStatus.Draft;
        await ValidateReferencesAsync(model, publishing);
        await ValidateImagesAsync(model, publishing);

        if (model.MaxPurchaseQuantity.HasValue && model.MaxPurchaseQuantity.Value < model.MinPurchaseQuantity)
        {
            ModelState.AddModelError(nameof(model.MaxPurchaseQuantity),
                "Số lượng mua tối đa phải lớn hơn hoặc bằng số lượng mua tối thiểu.");
        }

        if (model.ValidTo.HasValue && model.ValidTo.Value <= model.ValidFrom)
        {
            ModelState.AddModelError(nameof(model.ValidTo),
                "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
        }

        if (publishing)
        {
            var lowerTitle = model.Name.ToLowerInvariant();
            var forbidden = ForbiddenTitleTerms.FirstOrDefault(lowerTitle.Contains);
            if (forbidden is not null)
                ModelState.AddModelError(nameof(model.Name), $"Tên sản phẩm không nên chứa nội dung quảng cáo hoặc tên nền tảng: “{forbidden}”.");

            if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Trim().Length < 110)
                ModelState.AddModelError(nameof(model.Description), "Sản phẩm đang bán cần mô tả chi tiết tối thiểu 110 ký tự.");
            if (string.IsNullOrWhiteSpace(model.CountryOfOrigin))
                ModelState.AddModelError(nameof(model.CountryOfOrigin), "Vui lòng nhập xuất xứ sản phẩm.");
            if (string.IsNullOrWhiteSpace(model.ManufacturerName))
                ModelState.AddModelError(nameof(model.ManufacturerName), "Vui lòng nhập tên nhà sản xuất hoặc đơn vị chịu trách nhiệm.");
            if (string.IsNullOrWhiteSpace(model.ManufacturerAddress))
                ModelState.AddModelError(nameof(model.ManufacturerAddress), "Vui lòng nhập địa chỉ nhà sản xuất hoặc đơn vị chịu trách nhiệm.");
            if (!model.Weight.HasValue || model.Weight.Value <= 0)
                ModelState.AddModelError(nameof(model.Weight), "Sản phẩm đang bán cần khối lượng đóng gói lớn hơn 0.");
            if (!model.PackageLengthCm.HasValue || model.PackageLengthCm.Value <= 0 ||
                !model.PackageWidthCm.HasValue || model.PackageWidthCm.Value <= 0 ||
                !model.PackageHeightCm.HasValue || model.PackageHeightCm.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.PackageLengthCm), "Vui lòng nhập đủ kích thước kiện hàng lớn hơn 0.");
            }
        }

        if (!model.HasVariants)
        {
            ValidateSimplePrice(model, publishing);
            if (model.MarketId.HasValue && model.Id.HasValue && HasCompletePrice(model.CostPrice, model.ListPrice, model.SalePrice))
            {
                if (await HasPriceOverlapAsync(model.Id.Value, null, model.MarketId.Value, model.ValidFrom, model.ValidTo, model.ProductPriceScheduleId))
                    ModelState.AddModelError(nameof(model.ValidFrom), "Khoảng thời gian giá bị chồng lấn với lịch giá hiện có của sản phẩm.");
            }
            return;
        }

        ValidateOptionDefinitions(model);
        if (model.Variants.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Variants), "Vui lòng tạo ít nhất một biến thể.");
            return;
        }

        var activeVariants = model.Variants.Where(x => x.IsActive).ToList();
        if (publishing && activeVariants.Count == 0)
            ModelState.AddModelError(nameof(model.Variants), "Sản phẩm đang bán cần ít nhất một biến thể hoạt động.");

        var defaultCount = activeVariants.Count(x => x.IsDefault);
        if (defaultCount == 0 && activeVariants.Count > 0)
            activeVariants[0].IsDefault = true;
        else if (defaultCount > 1)
            ModelState.AddModelError(nameof(model.Variants), "Chỉ được chọn một biến thể mặc định.");

        for (var index = 0; index < model.Variants.Count; index++)
        {
            var variant = model.Variants[index];
            if (string.IsNullOrWhiteSpace(variant.Sku))
                variant.Sku = BuildVariantSku(model.Sku, variant.Value1, variant.Value2, index);
            variant.Sku = variant.Sku.Trim().ToUpperInvariant();
            variant.Barcode = NullIfWhiteSpace(variant.Barcode);

            if (!variant.IsActive)
                continue;

            if (publishing || HasPrice(variant.CostPrice, variant.ListPrice, variant.SalePrice))
                ValidateVariantPrice(variant, index);
            if (publishing && (!variant.Weight.HasValue || variant.Weight.Value <= 0))
                ModelState.AddModelError($"Variants[{index}].Weight", "Khối lượng biến thể phải lớn hơn 0.");

            if (model.MarketId.HasValue && variant.Id.HasValue && HasCompletePrice(variant.CostPrice, variant.ListPrice, variant.SalePrice))
            {
                if (await HasPriceOverlapAsync(null, variant.Id.Value, model.MarketId.Value, model.ValidFrom, model.ValidTo, variant.PriceScheduleId))
                {
                    ModelState.AddModelError($"Variants[{index}].SalePrice",
                        $"Lịch giá của biến thể “{variant.Name}” bị chồng lấn.");
                }
            }
        }
    }

    private async Task ValidateReferencesAsync(ProductEditorViewModel model, bool publishing)
    {
        var categoryExists = model.CategoryId.HasValue && await _db.Categories
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.CategoryId.Value && x.IsActive);
        var brandExists = model.BrandId.HasValue && await _db.Brands
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.BrandId.Value && x.IsActive);

        if (!categoryExists)
            ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại hoặc đã bị ẩn.");
        if (!brandExists)
            ModelState.AddModelError(nameof(model.BrandId), "Thương hiệu không tồn tại hoặc đã bị ẩn.");

        var anyPrice = model.HasVariants
            ? model.Variants.Any(x => HasPrice(x.CostPrice, x.ListPrice, x.SalePrice))
            : HasPrice(model.CostPrice, model.ListPrice, model.SalePrice);

        if (!publishing && !anyPrice && !model.MarketId.HasValue)
            return;

        var marketExists = model.MarketId.HasValue && await _db.Markets
            .AsNoTracking()
            .AnyAsync(x => x.Id == model.MarketId.Value && x.IsActive);
        if (!marketExists)
            ModelState.AddModelError(nameof(model.MarketId), "Vui lòng chọn một thị trường đang hoạt động.");
    }

    private async Task ValidateImagesAsync(ProductEditorViewModel model, bool publishing)
    {
        var currentImages = model.Id.HasValue
            ? await _db.ProductImages.AsNoTracking().Where(x => x.ProductId == model.Id.Value).Select(x => x.Id).ToListAsync()
            : new List<int>();
        var removeIds = model.RemoveImageIds.Distinct().ToHashSet();
        var remainingCount = currentImages.Count(x => !removeIds.Contains(x)) + model.ImageFiles.Count(x => x.Length > 0);

        if (remainingCount > MaxImages)
            ModelState.AddModelError(nameof(model.ImageFiles), $"Mỗi sản phẩm được tải tối đa {MaxImages} hình ảnh.");
        if (publishing && remainingCount == 0)
            ModelState.AddModelError(nameof(model.ImageFiles), "Sản phẩm đang bán cần ít nhất một hình ảnh.");

        for (var index = 0; index < model.ImageFiles.Count; index++)
        {
            var file = model.ImageFiles[index];
            if (file.Length <= 0)
                continue;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                ModelState.AddModelError(nameof(model.ImageFiles), $"Ảnh “{file.FileName}” không đúng định dạng JPG, PNG hoặc WEBP.");
            if (file.Length > MaxImageSize)
                ModelState.AddModelError(nameof(model.ImageFiles), $"Ảnh “{file.FileName}” vượt quá 5 MB.");
        }
    }

    private void ValidateOptionDefinitions(ProductEditorViewModel model)
    {
        var values1 = SplitValues(model.OptionValues1);
        var values2 = SplitValues(model.OptionValues2);

        if (string.IsNullOrWhiteSpace(model.OptionName1))
            ModelState.AddModelError(nameof(model.OptionName1), "Vui lòng nhập tên phân loại thứ nhất.");
        if (values1.Count == 0)
            ModelState.AddModelError(nameof(model.OptionValues1), "Phân loại thứ nhất cần ít nhất một giá trị.");
        if (values1.Count > MaxOptionValues)
            ModelState.AddModelError(nameof(model.OptionValues1), $"Mỗi phân loại chỉ nên có tối đa {MaxOptionValues} giá trị.");

        if (!string.IsNullOrWhiteSpace(model.OptionName2))
        {
            if (string.Equals(model.OptionName1?.Trim(), model.OptionName2.Trim(), StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.OptionName2), "Hai phân loại phải có tên khác nhau.");
            if (values2.Count == 0)
                ModelState.AddModelError(nameof(model.OptionValues2), "Phân loại thứ hai đã có tên nên cần giá trị.");
            if (values2.Count > MaxOptionValues)
                ModelState.AddModelError(nameof(model.OptionValues2), $"Mỗi phân loại chỉ nên có tối đa {MaxOptionValues} giá trị.");
        }
        else if (values2.Count > 0)
        {
            ModelState.AddModelError(nameof(model.OptionName2), "Vui lòng nhập tên cho phân loại thứ hai.");
        }

        var combinationCount = values1.Count * Math.Max(1, values2.Count);
        if (combinationCount > MaxVariants)
            ModelState.AddModelError(nameof(model.OptionValues2), $"Tổng số tổ hợp không được vượt quá {MaxVariants} SKU.");
    }

    private void ValidateSimplePrice(ProductEditorViewModel model, bool publishing)
    {
        if (!publishing && !HasPrice(model.CostPrice, model.ListPrice, model.SalePrice))
            return;
        if (model.ListPrice <= 0)
            ModelState.AddModelError(nameof(model.ListPrice), "Giá niêm yết phải lớn hơn 0.");
        if (model.SalePrice <= 0)
            ModelState.AddModelError(nameof(model.SalePrice), "Giá bán phải lớn hơn 0.");
        if (model.SalePrice > model.ListPrice)
            ModelState.AddModelError(nameof(model.SalePrice), "Giá bán không được lớn hơn giá niêm yết.");
        if (model.CostPrice < 0)
            ModelState.AddModelError(nameof(model.CostPrice), "Giá vốn không được âm.");
    }

    private void ValidateVariantPrice(ProductVariantEditorItem variant, int index)
    {
        if (variant.ListPrice <= 0)
            ModelState.AddModelError($"Variants[{index}].ListPrice", "Giá niêm yết phải lớn hơn 0.");
        if (variant.SalePrice <= 0)
            ModelState.AddModelError($"Variants[{index}].SalePrice", "Giá bán phải lớn hơn 0.");
        if (variant.SalePrice > variant.ListPrice)
            ModelState.AddModelError($"Variants[{index}].SalePrice", "Giá bán không được lớn hơn giá niêm yết.");
        if (variant.CostPrice < 0)
            ModelState.AddModelError($"Variants[{index}].CostPrice", "Giá vốn không được âm.");
    }

    private async Task ValidateSkuUniquenessAsync(ProductEditorViewModel model)
    {
        if (await _db.Products.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Id != model.Id && x.Sku == model.Sku))
            ModelState.AddModelError(nameof(model.Sku), "Mã sản phẩm đã tồn tại.");

        if (await _db.ProductVariants.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Sku == model.Sku))
            ModelState.AddModelError(nameof(model.Sku), "Mã sản phẩm đang trùng với SKU của một biến thể khác.");

        if (!model.HasVariants)
            return;

        var skuGroups = model.Variants
            .Where(x => x.IsActive)
            .GroupBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();
        foreach (var group in skuGroups)
            ModelState.AddModelError(nameof(model.Variants), $"SKU biến thể “{group.Key}” đang bị trùng trong biểu mẫu.");

        var barcodeGroups = model.Variants
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Barcode))
            .GroupBy(x => x.Barcode!, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();
        foreach (var group in barcodeGroups)
            ModelState.AddModelError(nameof(model.Variants), $"Barcode “{group.Key}” đang bị trùng trong biểu mẫu.");

        for (var index = 0; index < model.Variants.Count; index++)
        {
            var variant = model.Variants[index];
            if (!variant.IsActive)
                continue;
            if (string.Equals(variant.Sku, model.Sku, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError($"Variants[{index}].Sku", "SKU biến thể phải khác SKU sản phẩm cha.");
            }
            if (await _db.ProductVariants.IgnoreQueryFilters().AnyAsync(x =>
                    !x.IsDeleted && x.Id != variant.Id && x.Sku == variant.Sku))
            {
                ModelState.AddModelError($"Variants[{index}].Sku", "SKU biến thể đã tồn tại.");
            }
            if (await _db.Products.IgnoreQueryFilters().AnyAsync(x =>
                    !x.IsDeleted && x.Id != model.Id && x.Sku == variant.Sku))
            {
                ModelState.AddModelError($"Variants[{index}].Sku", "SKU biến thể đang trùng với mã của một sản phẩm khác.");
            }
            if (!string.IsNullOrWhiteSpace(variant.Barcode) && await _db.ProductVariants.IgnoreQueryFilters().AnyAsync(x =>
                    !x.IsDeleted && x.Id != variant.Id && x.Barcode == variant.Barcode))
            {
                ModelState.AddModelError($"Variants[{index}].Barcode", "Barcode đã tồn tại.");
            }
        }
    }

    private async Task ApplyProductFieldsAsync(Product product, ProductEditorViewModel model)
    {
        product.Name = model.Name.Trim();
        product.Slug = await CreateUniqueSlugAsync(model.Name, model.Id);
        product.Sku = model.Sku;
        product.ModelNumber = NullIfWhiteSpace(model.ModelNumber);
        product.Unit = model.Unit.Trim();
        product.CategoryId = model.CategoryId!.Value;
        product.BrandId = model.BrandId!.Value;
        product.ShortDescription = NullIfWhiteSpace(model.ShortDescription);
        product.Description = NullIfWhiteSpace(model.Description);
        product.CountryOfOrigin = NullIfWhiteSpace(model.CountryOfOrigin);
        product.ManufacturerName = NullIfWhiteSpace(model.ManufacturerName);
        product.ManufacturerAddress = NullIfWhiteSpace(model.ManufacturerAddress);
        product.WarrantyMonths = model.WarrantyMonths;
        product.Status = model.Status;
        product.IsFeatured = model.IsFeatured;
        product.HasVariants = model.HasVariants;
        product.StockQuantity = model.HasVariants ? 0 : model.StockQuantity;
        product.LowStockThreshold = model.LowStockThreshold;
        product.MinPurchaseQuantity = model.MinPurchaseQuantity;
        product.MaxPurchaseQuantity = model.MaxPurchaseQuantity;
        product.Weight = model.Weight;
        product.PackageLengthCm = model.PackageLengthCm;
        product.PackageWidthCm = model.PackageWidthCm;
        product.PackageHeightCm = model.PackageHeightCm;
        product.UpdatedBy = CurrentUserName();
    }

    private async Task SyncImagesAsync(
        Product product,
        ProductEditorViewModel model,
        List<string> uploadedUrls,
        List<string> filesToDeleteAfterCommit)
    {
        var existing = await _db.ProductImages
            .Where(x => x.ProductId == product.Id)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        var removeIds = model.RemoveImageIds.Distinct().ToHashSet();

        foreach (var image in existing.Where(x => removeIds.Contains(x.Id)).ToList())
        {
            filesToDeleteAfterCommit.Add(image.ImageUrl);
            _db.ProductImages.Remove(image);
            existing.Remove(image);
        }

        foreach (var file in model.ImageFiles.Where(x => x.Length > 0))
        {
            var url = await SaveImageAsync(file);
            uploadedUrls.Add(url);
            var image = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = url,
                AltText = product.Name,
                DisplayOrder = existing.Count,
                IsPrimary = false,
                CreatedBy = CurrentUserName()
            };
            _db.ProductImages.Add(image);
            existing.Add(image);
        }

        ProductImage? primary = null;
        if (model.PrimaryImageId.HasValue)
            primary = existing.FirstOrDefault(x => x.Id == model.PrimaryImageId.Value);
        primary ??= existing.FirstOrDefault(x => x.IsPrimary);
        primary ??= existing.FirstOrDefault();

        for (var index = 0; index < existing.Count; index++)
        {
            existing[index].DisplayOrder = index;
            existing[index].IsPrimary = ReferenceEquals(existing[index], primary);
            existing[index].AltText = product.Name;
        }
    }

    private async Task SyncSpecificationsAsync(Product product, ProductEditorViewModel model)
    {
        var existing = await _db.ProductSpecifications.Where(x => x.ProductId == product.Id).ToListAsync();
        _db.ProductSpecifications.RemoveRange(existing);

        var rows = model.Specifications
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(x => x.Name!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(30)
            .ToList();

        for (var index = 0; index < rows.Count; index++)
        {
            _db.ProductSpecifications.Add(new ProductSpecification
            {
                ProductId = product.Id,
                Name = rows[index].Name!.Trim(),
                Value = rows[index].Value!.Trim(),
                DisplayOrder = index,
                CreatedBy = CurrentUserName()
            });
        }
    }

    private async Task<List<VariantSyncResult>> SyncOptionsAndVariantsAsync(Product product, ProductEditorViewModel model)
    {
        var existingVariants = await _db.ProductVariants
            .IgnoreQueryFilters()
            .Where(x => x.ProductId == product.Id)
            .Include(x => x.VariantValues)
            .Include(x => x.PriceSchedules)
            .ToListAsync();
        var variantIds = existingVariants.Select(x => x.Id).ToList();

        if (variantIds.Count > 0)
        {
            var links = await _db.ProductVariantValues.Where(x => variantIds.Contains(x.ProductVariantId)).ToListAsync();
            _db.ProductVariantValues.RemoveRange(links);

            // Tạm giải phóng các khóa duy nhất để cho phép đổi chéo SKU, barcode
            // hoặc chuyển biến thể mặc định trong cùng một transaction.
            foreach (var existing in existingVariants.Where(x => !x.IsDeleted))
            {
                existing.IsDefault = false;
                var temporarySku = $"TMP-{existing.Id}-{Guid.NewGuid():N}";
                existing.Sku = temporarySku[..Math.Min(100, temporarySku.Length)];
                existing.Barcode = null;
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
            foreach (var variant in existingVariants)
            {
                variant.IsDeleted = true;
                variant.IsActive = false;
                variant.IsDefault = false;
                variant.UpdatedBy = CurrentUserName();
                foreach (var price in variant.PriceSchedules)
                    price.IsActive = false;
            }
            product.StockQuantity = model.StockQuantity;
            return new List<VariantSyncResult>();
        }

        var definitions = BuildOptionDefinitions(model);
        var optionEntities = new List<ProductOption>();
        for (var optionIndex = 0; optionIndex < definitions.Count; optionIndex++)
        {
            var option = new ProductOption
            {
                ProductId = product.Id,
                Name = definitions[optionIndex].Name,
                DisplayOrder = optionIndex,
                CreatedBy = CurrentUserName()
            };
            for (var valueIndex = 0; valueIndex < definitions[optionIndex].Values.Count; valueIndex++)
            {
                option.Values.Add(new ProductOptionValue
                {
                    Value = definitions[optionIndex].Values[valueIndex],
                    DisplayOrder = valueIndex,
                    CreatedBy = CurrentUserName()
                });
            }
            optionEntities.Add(option);
        }

        _db.ProductOptions.AddRange(optionEntities);
        await _db.SaveChangesAsync();

        var valueMaps = optionEntities
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Values.ToDictionary(v => v.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var usedIds = new HashSet<int>();
        var results = new List<VariantSyncResult>();

        for (var index = 0; index < model.Variants.Count; index++)
        {
            var row = model.Variants[index];
            var key = BuildCombinationKey(row.Value1, row.Value2);
            row.CombinationKey = key;
            var entity = row.Id.HasValue
                ? existingVariants.FirstOrDefault(x => x.Id == row.Id.Value)
                : null;
            entity ??= existingVariants.FirstOrDefault(x =>
                !usedIds.Contains(x.Id) && string.Equals(x.CombinationKey, key, StringComparison.OrdinalIgnoreCase));

            if (entity is null)
            {
                entity = new ProductVariant
                {
                    ProductId = product.Id,
                    CreatedBy = CurrentUserName()
                };
                _db.ProductVariants.Add(entity);
            }

            if (entity.Id > 0)
                usedIds.Add(entity.Id);
            entity.Name = string.Join(" / ", new[] { row.Value1, row.Value2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
            entity.CombinationKey = key;
            entity.Sku = row.Sku.Trim().ToUpperInvariant();
            entity.Barcode = NullIfWhiteSpace(row.Barcode);
            entity.StockQuantity = row.StockQuantity;
            entity.LowStockThreshold = row.LowStockThreshold;
            entity.SortOrder = index;
            entity.Weight = row.Weight ?? model.Weight;
            entity.IsDefault = row.IsDefault && row.IsActive;
            entity.IsActive = row.IsActive;
            entity.IsDeleted = false;
            entity.UpdatedBy = CurrentUserName();

            if (valueMaps.Count > 0 && valueMaps[0].TryGetValue(row.Value1, out var value1))
                entity.VariantValues.Add(new ProductVariantValue { ProductOptionValueId = value1.Id });
            if (valueMaps.Count > 1 && !string.IsNullOrWhiteSpace(row.Value2) && valueMaps[1].TryGetValue(row.Value2, out var value2))
                entity.VariantValues.Add(new ProductVariantValue { ProductOptionValueId = value2.Id });

            results.Add(new VariantSyncResult(entity, row));
        }

        foreach (var oldVariant in existingVariants.Where(x => !results.Any(r => ReferenceEquals(r.Entity, x))))
        {
            oldVariant.IsDeleted = true;
            oldVariant.IsActive = false;
            oldVariant.IsDefault = false;
            oldVariant.UpdatedBy = CurrentUserName();
            foreach (var price in oldVariant.PriceSchedules)
                price.IsActive = false;
        }

        var activeResults = results.Where(x => x.Entity.IsActive).ToList();
        if (activeResults.Count > 0 && activeResults.All(x => !x.Entity.IsDefault))
            activeResults[0].Entity.IsDefault = true;
        var firstDefault = activeResults.FirstOrDefault(x => x.Entity.IsDefault);
        foreach (var result in activeResults.Where(x => !ReferenceEquals(x, firstDefault)))
            result.Entity.IsDefault = false;

        product.StockQuantity = activeResults.Sum(x => x.Entity.StockQuantity);
        await _db.SaveChangesAsync();
        return results;
    }

    private async Task SyncPricesAsync(Product product, ProductEditorViewModel model, List<VariantSyncResult> variants)
    {
        if (model.HasVariants)
        {
            var productSchedules = await _db.PriceSchedules.Where(x => x.ProductId == product.Id && x.IsActive).ToListAsync();
            foreach (var schedule in productSchedules)
                schedule.IsActive = false;
        }
        else
        {
            var variantIds = await _db.ProductVariants.IgnoreQueryFilters().Where(x => x.ProductId == product.Id).Select(x => x.Id).ToListAsync();
            var variantSchedules = await _db.PriceSchedules.Where(x => x.ProductVariantId.HasValue && variantIds.Contains(x.ProductVariantId.Value) && x.IsActive).ToListAsync();
            foreach (var schedule in variantSchedules)
                schedule.IsActive = false;
        }

        if (!model.MarketId.HasValue)
            return;

        if (!model.HasVariants)
        {
            if (!HasCompletePrice(model.CostPrice, model.ListPrice, model.SalePrice))
            {
                await DisableScheduleAsync(model.ProductPriceScheduleId);
                return;
            }
            var schedule = await FindOrCreateScheduleAsync(
                product.Id,
                null,
                model.MarketId.Value,
                model.ProductPriceScheduleId,
                model.ValidFrom,
                model.ValidTo);
            ApplySchedule(schedule, model.CostPrice, model.ListPrice, model.SalePrice, model.ValidFrom, model.ValidTo, model.PriceNote);
            return;
        }

        foreach (var result in variants)
        {
            if (!result.Entity.IsActive)
            {
                if (result.Row.PriceScheduleId.HasValue)
                {
                    var disabled = await _db.PriceSchedules.FirstOrDefaultAsync(x => x.Id == result.Row.PriceScheduleId.Value);
                    if (disabled is not null)
                        disabled.IsActive = false;
                }
                continue;
            }

            if (!HasCompletePrice(result.Row.CostPrice, result.Row.ListPrice, result.Row.SalePrice))
            {
                await DisableScheduleAsync(result.Row.PriceScheduleId);
                continue;
            }
            var schedule = await FindOrCreateScheduleAsync(
                null,
                result.Entity.Id,
                model.MarketId.Value,
                result.Row.PriceScheduleId,
                model.ValidFrom,
                model.ValidTo);
            ApplySchedule(schedule, result.Row.CostPrice, result.Row.ListPrice, result.Row.SalePrice, model.ValidFrom, model.ValidTo, model.PriceNote);
        }
    }

    private async Task DisableScheduleAsync(int? scheduleId)
    {
        if (!scheduleId.HasValue)
            return;

        var schedule = await _db.PriceSchedules.FirstOrDefaultAsync(x => x.Id == scheduleId.Value);
        if (schedule is null)
            return;

        schedule.IsActive = false;
        schedule.UpdatedBy = CurrentUserName();
    }

    private async Task<PriceSchedule> FindOrCreateScheduleAsync(
        int? productId,
        int? variantId,
        int marketId,
        int? requestedId,
        DateTime validFrom,
        DateTime? validTo)
    {
        PriceSchedule? schedule = null;
        if (requestedId.HasValue)
        {
            schedule = await _db.PriceSchedules.FirstOrDefaultAsync(x =>
                x.Id == requestedId.Value && x.MarketId == marketId &&
                x.ProductId == productId && x.ProductVariantId == variantId);
        }

        schedule ??= await _db.PriceSchedules
            .Where(x => x.MarketId == marketId && x.ProductId == productId && x.ProductVariantId == variantId && x.IsActive)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefaultAsync(x =>
                x.ValidFrom < (validTo ?? DateTime.MaxValue) && validFrom < (x.ValidTo ?? DateTime.MaxValue));

        if (schedule is not null)
            return schedule;

        schedule = new PriceSchedule
        {
            ProductId = productId,
            ProductVariantId = variantId,
            MarketId = marketId,
            IsActive = true,
            CreatedBy = CurrentUserName()
        };
        _db.PriceSchedules.Add(schedule);
        return schedule;
    }

    private void ApplySchedule(
        PriceSchedule schedule,
        decimal costPrice,
        decimal listPrice,
        decimal salePrice,
        DateTime validFrom,
        DateTime? validTo,
        string? note)
    {
        schedule.CostPrice = costPrice;
        schedule.ListPrice = listPrice;
        schedule.SalePrice = salePrice;
        schedule.ValidFrom = validFrom;
        schedule.ValidTo = validTo;
        schedule.Note = NullIfWhiteSpace(note);
        schedule.IsActive = true;
        schedule.UpdatedBy = CurrentUserName();
    }

    private async Task<bool> HasPriceOverlapAsync(
        int? productId,
        int? variantId,
        int marketId,
        DateTime validFrom,
        DateTime? validTo,
        int? excludeId)
    {
        var end = validTo ?? DateTime.MaxValue;
        return await _db.PriceSchedules.AsNoTracking().AnyAsync(x =>
            x.IsActive && (!excludeId.HasValue || x.Id != excludeId.Value) && x.MarketId == marketId &&
            x.ProductId == productId && x.ProductVariantId == variantId &&
            x.ValidFrom < end && validFrom < (x.ValidTo ?? DateTime.MaxValue));
    }

    private void EnsureVariantRows(ProductEditorViewModel model)
    {
        if (!model.HasVariants)
        {
            model.Variants.Clear();
            return;
        }

        var values1 = SplitValues(model.OptionValues1);
        var values2 = SplitValues(model.OptionValues2);
        if (values1.Count == 0)
            return;

        var existing = model.Variants
            .GroupBy(x => string.IsNullOrWhiteSpace(x.CombinationKey)
                ? BuildCombinationKey(x.Value1, x.Value2)
                : x.CombinationKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var rows = new List<ProductVariantEditorItem>();
        var secondGroup = values2.Count == 0 ? new List<string?> { null } : values2.Cast<string?>().ToList();
        var index = 0;

        foreach (var value1 in values1)
        {
            foreach (var value2 in secondGroup)
            {
                var key = BuildCombinationKey(value1, value2);
                if (!existing.TryGetValue(key, out var row))
                {
                    row = new ProductVariantEditorItem
                    {
                        CombinationKey = key,
                        Value1 = value1,
                        Value2 = value2,
                        Name = string.Join(" / ", new[] { value1, value2 }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Sku = BuildVariantSku(model.Sku, value1, value2, index),
                        StockQuantity = 0,
                        LowStockThreshold = model.LowStockThreshold,
                        Weight = model.Weight,
                        CostPrice = model.CostPrice,
                        ListPrice = model.ListPrice,
                        SalePrice = model.SalePrice,
                        IsDefault = index == 0,
                        IsActive = true
                    };
                }
                else
                {
                    row.Value1 = value1;
                    row.Value2 = value2;
                    row.Name = string.Join(" / ", new[] { value1, value2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    row.CombinationKey = key;
                }
                rows.Add(row);
                index++;
            }
        }

        model.Variants = rows;
    }

    private static void NormalizeEditorModel(ProductEditorViewModel model)
    {
        model.ImageFiles ??= new List<IFormFile>();
        model.RemoveImageIds ??= new List<int>();
        model.Variants ??= new List<ProductVariantEditorItem>();
        model.Specifications ??= new List<ProductSpecificationEditorItem>();
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Sku = model.Sku?.Trim().ToUpperInvariant() ?? string.Empty;
        model.Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Cái" : model.Unit.Trim();
        model.OptionName1 = NullIfWhiteSpace(model.OptionName1);
        model.OptionName2 = NullIfWhiteSpace(model.OptionName2);
    }

    private async Task LoadEditorStateAsync(ProductEditorViewModel model)
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

        if (model.Id.HasValue)
        {
            model.ExistingImages = await _db.ProductImages
                .AsNoTracking()
                .Where(x => x.ProductId == model.Id.Value)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ProductImageEditorItem
                {
                    Id = x.Id,
                    ImageUrl = x.ImageUrl,
                    IsPrimary = x.IsPrimary,
                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync();
        }
        else
        {
            model.ExistingImages = Array.Empty<ProductImageEditorItem>();
        }

        if (model.Specifications.Count == 0)
            model.Specifications.Add(new ProductSpecificationEditorItem());
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

    private async Task<int?> GetDefaultMarketIdAsync()
    {
        return await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private static PriceSchedule? SelectPreferredSchedule(IEnumerable<PriceSchedule> schedules, int marketId)
    {
        var now = DateTime.Now;
        return schedules
            .Where(x => x.MarketId == marketId && x.IsActive && x.ValidFrom <= now && (!x.ValidTo.HasValue || x.ValidTo.Value >= now))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault()
            ?? schedules
                .Where(x => x.MarketId == marketId && x.IsActive)
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefault();
    }

    private List<string> GetListingIssues(Product product, int? marketId, DateTime now)
    {
        var issues = new List<string>();
        if (product.Name.Trim().Length < 10)
            issues.Add("Tên sản phẩm quá ngắn");
        if (string.IsNullOrWhiteSpace(product.Description) || product.Description.Trim().Length < 110)
            issues.Add("Mô tả chi tiết chưa đủ 110 ký tự");
        if (product.Images.Count == 0)
            issues.Add("Chưa có hình ảnh");
        if (string.IsNullOrWhiteSpace(product.CountryOfOrigin))
            issues.Add("Thiếu xuất xứ");
        if (string.IsNullOrWhiteSpace(product.ManufacturerName) || string.IsNullOrWhiteSpace(product.ManufacturerAddress))
            issues.Add("Thiếu thông tin nhà sản xuất");
        if (!product.Weight.HasValue || product.Weight.Value <= 0 ||
            !product.PackageLengthCm.HasValue || product.PackageLengthCm.Value <= 0 ||
            !product.PackageWidthCm.HasValue || product.PackageWidthCm.Value <= 0 ||
            !product.PackageHeightCm.HasValue || product.PackageHeightCm.Value <= 0)
            issues.Add("Thiếu khối lượng hoặc kích thước kiện hàng");

        if (product.HasVariants)
        {
            var activeVariants = product.Variants.Where(x => x.IsActive && !x.IsDeleted).ToList();
            if (activeVariants.Count == 0)
                issues.Add("Chưa có biến thể hoạt động");
            if (activeVariants.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
                issues.Add("Có biến thể thiếu SKU");
            if (marketId.HasValue && activeVariants.Any(v => !v.PriceSchedules.Any(p =>
                    p.MarketId == marketId.Value && p.IsActive && p.ValidFrom <= now && (!p.ValidTo.HasValue || p.ValidTo.Value >= now))))
                issues.Add("Có biến thể chưa có giá đang áp dụng");
        }
        else if (marketId.HasValue && !product.PriceSchedules.Any(p =>
                     p.MarketId == marketId.Value && p.IsActive && p.ValidFrom <= now && (!p.ValidTo.HasValue || p.ValidTo.Value >= now)))
        {
            issues.Add("Chưa có giá đang áp dụng");
        }

        return issues;
    }

    private int CalculateListingScore(Product product, int? marketId, DateTime now)
    {
        var checks = new[]
        {
            product.Name.Trim().Length >= 10,
            !string.IsNullOrWhiteSpace(product.ShortDescription),
            !string.IsNullOrWhiteSpace(product.Description) && product.Description.Trim().Length >= 110,
            product.Images.Count >= 1,
            product.Images.Count >= 3,
            !string.IsNullOrWhiteSpace(product.CountryOfOrigin),
            !string.IsNullOrWhiteSpace(product.ManufacturerName),
            product.Weight.HasValue && product.Weight.Value > 0,
            product.PackageLengthCm.HasValue && product.PackageLengthCm.Value > 0 && product.PackageWidthCm.HasValue && product.PackageWidthCm.Value > 0 && product.PackageHeightCm.HasValue && product.PackageHeightCm.Value > 0,
            GetListingIssues(product, marketId, now).All(x => !x.Contains("giá", StringComparison.OrdinalIgnoreCase))
        };
        return checks.Count(x => x) * 10;
    }

    private async Task EnsureSystemManagedProductCodesAsync(ProductEditorViewModel model)
    {
        if (model.Id.HasValue)
        {
            var existing = await _db.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Id == model.Id.Value)
                .Select(x => new
                {
                    x.Name,
                    x.CategoryId,
                    x.BrandId,
                    x.Sku,
                    x.ModelNumber
                })
                .FirstOrDefaultAsync();

            if (existing is null)
                throw new InvalidOperationException("Sản phẩm không còn tồn tại.");

            // Không nhận SKU/model từ request khi chỉnh sửa.
            model.Sku = existing.Sku;

            if (!string.IsNullOrWhiteSpace(existing.ModelNumber))
            {
                model.ModelNumber = existing.ModelNumber;
                return;
            }

            var missingModelSuggestion = await GenerateUniqueProductCodesAsync(
                existing.Name,
                existing.CategoryId,
                existing.BrandId,
                model.Id);

            model.ModelNumber = missingModelSuggestion.ModelNumber;
            return;
        }

        if (!model.CategoryId.HasValue || !model.BrandId.HasValue)
        {
            model.Sku = string.Empty;
            model.ModelNumber = null;
            return;
        }

        // Khi tạo mới, luôn bỏ qua mã do client gửi và sinh lại từ database.
        var suggestion = await GenerateUniqueProductCodesAsync(
            model.Name,
            model.CategoryId.Value,
            model.BrandId.Value,
            null);

        model.Sku = suggestion.Sku;
        model.ModelNumber = suggestion.ModelNumber;
    }

    private async Task<ProductCodeSuggestion> GenerateUniqueProductCodesAsync(
        string productName,
        int categoryId,
        int brandId,
        int? currentProductId)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(x => x.Id == categoryId && x.IsActive)
            .Select(x => new { x.Name, x.Slug })
            .FirstOrDefaultAsync();

        if (category is null)
            throw new InvalidOperationException("Danh mục không tồn tại hoặc đã bị ẩn.");

        var brand = await _db.Brands
            .AsNoTracking()
            .Where(x => x.Id == brandId && x.IsActive)
            .Select(x => new { x.Name, x.Slug })
            .FirstOrDefaultAsync();

        if (brand is null)
            throw new InvalidOperationException("Thương hiệu không tồn tại hoặc đã bị ẩn.");

        var brandToken = BuildCodeToken(brand.Slug, brand.Name, 4, "BR");
        var categoryToken = BuildCodeToken(category.Slug, category.Name, 5, "DM");
        var productToken = BuildCodeToken(null, productName, 8, "SP");

        var skuPrefix = $"HOME-{brandToken}-{categoryToken}-{productToken}";
        var modelPrefix = $"{brandToken}-{categoryToken}-{DateTime.Now:yyMM}";

        var usedProductSkus = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.Id != currentProductId &&
                        x.Sku.StartsWith(skuPrefix))
            .Select(x => x.Sku)
            .ToListAsync();

        var usedVariantSkus = await _db.ProductVariants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Sku.StartsWith(skuPrefix))
            .Select(x => x.Sku)
            .ToListAsync();

        var usedModels = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.Id != currentProductId &&
                        x.BrandId == brandId &&
                        x.ModelNumber != null &&
                        x.ModelNumber.StartsWith(modelPrefix))
            .Select(x => x.ModelNumber!)
            .ToListAsync();

        var usedSkus = usedProductSkus
            .Concat(usedVariantSkus)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var usedModelNumbers = usedModels
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var sequence = 1; sequence <= 9999; sequence++)
        {
            var suffix = sequence.ToString("000");
            var sku = $"{skuPrefix}-{suffix}";
            var modelNumber = $"{modelPrefix}-{suffix}";

            if (!usedSkus.Contains(sku) &&
                !usedModelNumbers.Contains(modelNumber))
            {
                return new ProductCodeSuggestion(sku, modelNumber);
            }
        }

        throw new InvalidOperationException(
            "Không thể sinh thêm SKU và mã model tự động vì dải số đã hết.");
    }

    private async Task ValidateModelNumberUniquenessAsync(ProductEditorViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ModelNumber) ||
            !model.BrandId.HasValue)
        {
            return;
        }

        var exists = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted &&
                           x.Id != model.Id &&
                           x.BrandId == model.BrandId.Value &&
                           x.ModelNumber == model.ModelNumber);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.ModelNumber),
                "Mã model hệ thống sinh đang bị trùng trong cùng thương hiệu.");
        }
    }

    private static string BuildCodeToken(
        string? slug,
        string source,
        int maxLength,
        string fallback)
    {
        var tokenSource = string.IsNullOrWhiteSpace(slug)
            ? SlugHelper.Generate(source)
            : slug;

        var token = Regex.Replace(
                tokenSource.ToUpperInvariant(),
                "[^A-Z0-9]",
                string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            token = fallback;

        return token[..Math.Min(token.Length, maxLength)];
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

    private async Task<string> CreateUniqueProductSkuAsync(string candidate)
    {
        var cleaned = Regex.Replace(candidate.Trim().ToUpperInvariant(), "[^A-Z0-9._-]", "-").Trim('-');
        var baseSku = cleaned[..Math.Min(cleaned.Length, 68)];
        if (string.IsNullOrWhiteSpace(baseSku))
            baseSku = "HOME-COPY";
        var sku = baseSku;
        var suffix = 2;
        while (await _db.Products.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Sku == sku) ||
               await _db.ProductVariants.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Sku == sku))
        {
            sku = $"{baseSku}-{suffix++}";
        }
        return sku;
    }

    private async Task<string> CreateUniqueVariantSkuAsync(string candidate)
    {
        var cleaned = Regex.Replace(candidate.Trim().ToUpperInvariant(), "[^A-Z0-9._-]", "-").Trim('-');
        var baseSku = cleaned[..Math.Min(cleaned.Length, 88)];
        if (string.IsNullOrWhiteSpace(baseSku))
            baseSku = "HOME-VARIANT-COPY";
        var sku = baseSku;
        var suffix = 2;
        while (await _db.ProductVariants.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Sku == sku))
            sku = $"{baseSku}-{suffix++}";
        return sku;
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

    private async Task DeleteUploadedFileIfUnusedAsync(string imageUrl)
    {
        var stillUsed = await _db.ProductImages.AsNoTracking().AnyAsync(x => x.ImageUrl == imageUrl);
        if (!stillUsed)
            DeleteUploadedFile(imageUrl);
    }

    private void DeleteUploadedFiles(IEnumerable<string> urls)
    {
        foreach (var url in urls)
            DeleteUploadedFile(url);
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
        var values1 = SplitValues(model.OptionValues1);
        if (!string.IsNullOrWhiteSpace(model.OptionName1) && values1.Count > 0)
            result.Add(new OptionDefinition(model.OptionName1.Trim(), values1));
        var values2 = SplitValues(model.OptionValues2);
        if (!string.IsNullOrWhiteSpace(model.OptionName2) && values2.Count > 0)
            result.Add(new OptionDefinition(model.OptionName2.Trim(), values2));
        return result;
    }

    private static IReadOnlyList<string> SplitValues(string? values)
    {
        return (values ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildCombinationKey(string value1, string? value2)
    {
        var key1 = ToSkuToken(value1);
        return string.IsNullOrWhiteSpace(value2) ? $"1={key1}" : $"1={key1}|2={ToSkuToken(value2)}";
    }

    private static string BuildVariantSku(string baseSku, string value1, string? value2, int index)
    {
        var safeBase = string.IsNullOrWhiteSpace(baseSku) ? "HOME-SP" : baseSku.Trim().ToUpperInvariant();
        var suffix = string.Join("-", new[] { value1, value2 }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => ToSkuToken(x!)));
        var ending = $"-{index + 1:00}";
        var maxBaseLength = Math.Max(1, 100 - suffix.Length - ending.Length - 1);
        if (safeBase.Length > maxBaseLength)
            safeBase = safeBase[..maxBaseLength];
        return $"{safeBase}-{suffix}{ending}";
    }

    private static string ToSkuToken(string value)
    {
        var token = SlugHelper.Generate(value).Replace("-", string.Empty).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(token) ? "VAR" : token[..Math.Min(token.Length, 12)];
    }

    private static string NormalizeSku(string? sku, string productName)
    {
        if (!string.IsNullOrWhiteSpace(sku))
        {
            var cleaned = Regex.Replace(sku.Trim().ToUpperInvariant(), "[^A-Z0-9._-]", "-").Trim('-');
            if (!string.IsNullOrWhiteSpace(cleaned))
                return cleaned[..Math.Min(80, cleaned.Length)];
        }
        var token = SlugHelper.Generate(productName).Replace("-", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(token))
            token = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"HOME-{token[..Math.Min(token.Length, 24)]}";
    }

    private static bool HasPrice(decimal cost, decimal list, decimal sale) => cost > 0 || list > 0 || sale > 0;
    private static bool HasCompletePrice(decimal cost, decimal list, decimal sale) => cost >= 0 && list > 0 && sale > 0 && sale <= list;

    private static string FormatPriceRange(IReadOnlyList<decimal> prices, CultureInfo culture)
    {
        if (prices.Count == 0)
            return "Chưa có giá";
        var min = prices.Min();
        var max = prices.Max();
        return min == max
            ? $"{min.ToString("N0", culture)}đ"
            : $"{min.ToString("N0", culture)}đ – {max.ToString("N0", culture)}đ";
    }

    private static string GetStatusLabel(ProductStatus status, int stock)
    {
        if ((status == ProductStatus.Active || status == ProductStatus.OutOfStock) && stock <= 0)
            return "Hết hàng";
        if (status == ProductStatus.Active && stock <= 10)
            return "Sắp hết";
        return status switch
        {
            ProductStatus.Active => "Đang bán",
            ProductStatus.Inactive => "Tạm ẩn",
            ProductStatus.OutOfStock => "Hết hàng",
            ProductStatus.Discontinued => "Ngừng kinh doanh",
            _ => "Bản nháp"
        };
    }

    private static string GetInitials(string value)
    {
        return string.Concat((value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(x => char.ToUpperInvariant(x[0])));
    }

    private static string JoinOptionValues(ProductOption? option)
    {
        return option is null
            ? string.Empty
            : string.Join(", ", option.Values.OrderBy(x => x.DisplayOrder).Select(x => x.Value));
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private string CurrentUserName() => User.Identity?.Name ?? "Quốc Hưng";

    private static string GetDatabaseMessage(DbUpdateException exception)
    {
        var databaseMessage = exception.InnerException?.Message ?? exception.Message;
        if (databaseMessage.Contains(
                "UX_Products_BrandId_ModelNumber",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Mã model đã tồn tại trong cùng thương hiệu. Vui lòng tải lại trang và thử lưu lại.";
        }

        var message = exception.InnerException?.Message ?? exception.Message;
        if (message.Contains("IX_Products_Sku", StringComparison.OrdinalIgnoreCase))
            return "Mã sản phẩm đã tồn tại.";
        if (message.Contains("IX_ProductVariants_Sku", StringComparison.OrdinalIgnoreCase))
            return "Có SKU biến thể đã tồn tại.";
        if (message.Contains("IX_ProductVariants_Barcode", StringComparison.OrdinalIgnoreCase))
            return "Có barcode biến thể đã tồn tại.";
        if (message.Contains("TRG_PriceSchedules_Validate", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("chồng lấn", StringComparison.OrdinalIgnoreCase))
            return "Khoảng thời gian giá bị chồng lấn với lịch giá đang hoạt động.";
        return "Không thể lưu dữ liệu. Vui lòng kiểm tra SKU, barcode, giá và các ràng buộc sản phẩm.";
    }

    private sealed record ProductCodeSuggestion(string Sku, string ModelNumber);
    private sealed record OptionDefinition(string Name, IReadOnlyList<string> Values);
    private sealed record VariantSyncResult(ProductVariant Entity, ProductVariantEditorItem Row);
}

