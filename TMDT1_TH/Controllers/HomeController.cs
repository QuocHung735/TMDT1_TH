using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var market = await GetStoreMarketAsync();
        var now = StorePriceClock.Now;

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Name,
                x.Slug,
                x.Description,
                x.ImageUrl,
                ProductCount = x.Products.Count(p =>
                    !p.IsDeleted &&
                    p.Status == ProductStatus.Active)
            })
            .Where(x => x.ProductCount > 0)
            .Take(8)
            .ToListAsync();

        var featuredEntities = await StoreProductQuery()
            .Where(x => x.IsFeatured)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(8)
            .ToListAsync();

        var latestEntities = await StoreProductQuery()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .ToListAsync();

        var model = new StoreHomeViewModel
        {
            CurrencyCode = market.CurrencyCode,
            MarketName = market.Name,
            Categories = categories.Select(x => new StoreCategoryViewModel
            {
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                ProductCount = x.ProductCount
            }).ToList(),
            FeaturedProducts = featuredEntities
                .Select(x => BuildProductCard(x, market.Id, market.CurrencyCode, now))
                .ToList(),
            LatestProducts = latestEntities
                .Select(x => BuildProductCard(x, market.Id, market.CurrencyCode, now))
                .ToList()
        };

        return View(model);
    }

    [HttpGet("/san-pham")]
    public async Task<IActionResult> Catalog(
        string? q,
        string? category,
        string? brand,
        string? sort,
        int page = 1)
    {
        const int pageSize = 12;
        page = Math.Max(page, 1);

        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        sort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort.Trim().ToLowerInvariant();

        var market = await GetStoreMarketAsync();
        var now = StorePriceClock.Now;

        IQueryable<Product> query = StoreProductQuery();

        if (q is not null)
        {
            query = query.Where(x =>
                x.Name.Contains(q) ||
                x.Sku.Contains(q) ||
                (x.ModelNumber != null && x.ModelNumber.Contains(q)) ||
                x.Category.Name.Contains(q) ||
                x.Brand.Name.Contains(q));
        }

        if (category is not null)
            query = query.Where(x => x.Category.Slug == category);

        if (brand is not null)
            query = query.Where(x => x.Brand.Slug == brand);

        var entities = await query.ToListAsync();
        var cards = entities
            .Select(x => BuildProductCard(x, market.Id, market.CurrencyCode, now));

        cards = sort switch
        {
            "price-asc" => cards
                .OrderBy(x => x.PriceMin ?? decimal.MaxValue)
                .ThenBy(x => x.Name),
            "price-desc" => cards
                .OrderByDescending(x => x.PriceMax ?? decimal.MinValue)
                .ThenBy(x => x.Name),
            "name" => cards.OrderBy(x => x.Name),
            _ => cards.OrderByDescending(x => x.CreatedAt)
        };

        var materializedCards = cards.ToList();
        var totalItems = materializedCards.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, totalPages);

        var categoryOptions = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new StoreFilterOptionViewModel
            {
                Name = x.Name,
                Slug = x.Slug,
                Count = x.Products.Count(p =>
                    !p.IsDeleted &&
                    p.Status == ProductStatus.Active)
            })
            .Where(x => x.Count > 0)
            .ToListAsync();

        var brandOptions = await _db.Brands
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new StoreFilterOptionViewModel
            {
                Name = x.Name,
                Slug = x.Slug,
                Count = x.Products.Count(p =>
                    !p.IsDeleted &&
                    p.Status == ProductStatus.Active)
            })
            .Where(x => x.Count > 0)
            .ToListAsync();

        var model = new StoreCatalogViewModel
        {
            Query = q,
            CategorySlug = category,
            BrandSlug = brand,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrencyCode = market.CurrencyCode,
            Products = materializedCards
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            Categories = categoryOptions,
            Brands = brandOptions
        };

        return View(model);
    }

    [HttpGet("/san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var market = await GetStoreMarketAsync();
        var now = StorePriceClock.Now;

        var product = await StoreProductQuery()
            .Include(x => x.Options)
                .ThenInclude(x => x.Values)
            .Include(x => x.Variants)
                .ThenInclude(x => x.VariantValues)
                    .ThenInclude(x => x.ProductOptionValue)
            .Include(x => x.Specifications)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (product is null)
            return NotFound();

        var simplePrice = GetCurrentPrice(product.PriceSchedules, market.Id, now);
        var activeVariants = product.Variants
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var variantModels = activeVariants
            .Select(variant =>
            {
                var price = GetCurrentPrice(variant.PriceSchedules, market.Id, now);
                var image = variant.Images
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(x => x.ImageUrl)
                    .FirstOrDefault();

                return new StoreVariantViewModel
                {
                    Id = variant.Id,
                    Name = variant.Name,
                    Sku = variant.Sku,
                    StockQuantity = variant.StockQuantity,
                    Weight = variant.Weight,
                    IsDefault = variant.IsDefault,
                    SalePrice = price?.SalePrice,
                    ListPrice = price?.ListPrice,
                    ImageUrl = image,
                    OptionValueIds = variant.VariantValues
                        .Select(x => x.ProductOptionValueId)
                        .OrderBy(x => x)
                        .ToList()
                };
            })
            .ToList();

        var images = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => new StoreImageViewModel
            {
                Url = x.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(x.AltText)
                    ? product.Name
                    : x.AltText!,
                IsPrimary = x.IsPrimary
            })
            .ToList();

        var optionModels = product.Options
            .OrderBy(x => x.DisplayOrder)
            .Select(option => new StoreOptionViewModel
            {
                Id = option.Id,
                Name = option.Name,
                Values = option.Values
                    .OrderBy(x => x.DisplayOrder)
                    .Select(value => new StoreOptionValueViewModel
                    {
                        Id = value.Id,
                        Value = value.Value,
                        ColorCode = value.ColorCode,
                        IsAvailable = activeVariants.Any(v =>
                            v.VariantValues.Any(vv =>
                                vv.ProductOptionValueId == value.Id))
                    })
                    .ToList()
            })
            .ToList();

        var totalStock = product.HasVariants
            ? activeVariants.Sum(x => x.StockQuantity)
            : product.StockQuantity;

        var relatedEntities = await StoreProductQuery()
            .Where(x => x.CategoryId == product.CategoryId && x.Id != product.Id)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .Take(4)
            .ToListAsync();

        var model = new StoreProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            ModelNumber = product.ModelNumber,
            Unit = product.Unit,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            BrandName = product.Brand.Name,
            CountryOfOrigin = product.CountryOfOrigin,
            ManufacturerName = product.ManufacturerName,
            ManufacturerAddress = product.ManufacturerAddress,
            WarrantyMonths = product.WarrantyMonths,
            Weight = product.Weight,
            MinPurchaseQuantity = product.MinPurchaseQuantity,
            MaxPurchaseQuantity = product.MaxPurchaseQuantity,
            HasVariants = product.HasVariants,
            StockQuantity = totalStock,
            CurrencyCode = market.CurrencyCode,
            SalePrice = simplePrice?.SalePrice,
            ListPrice = simplePrice?.ListPrice,
            Images = images,
            Options = optionModels,
            Variants = variantModels,
            Specifications = product.Specifications
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new StoreSpecificationViewModel
                {
                    Name = x.Name,
                    Value = x.Value
                })
                .ToList(),
            RelatedProducts = relatedEntities
                .Select(x => BuildProductCard(x, market.Id, market.CurrencyCode, now))
                .ToList()
        };

        return View(model);
    }

    public IActionResult Error() => View();

    private IQueryable<Product> StoreProductQuery()
    {
        return _db.Products
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Status == ProductStatus.Active)
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Images)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.Images)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .AsSplitQuery();
    }

    private async Task<StoreMarketContext> GetStoreMarketAsync()
    {
        var market = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new StoreMarketContext(
                x.Id,
                x.Name,
                x.CurrencyCode))
            .FirstOrDefaultAsync();

        return market ?? new StoreMarketContext(0, "Thị trường mặc định", "VND");
    }

    private static StoreProductCardViewModel BuildProductCard(
        Product product,
        int marketId,
        string currencyCode,
        DateTime now)
    {
        var prices = new List<PriceSchedule>();

        if (product.HasVariants)
        {
            foreach (var variant in product.Variants.Where(x => x.IsActive && !x.IsDeleted))
            {
                var current = GetCurrentPrice(variant.PriceSchedules, marketId, now);
                if (current is not null)
                    prices.Add(current);
            }
        }
        else
        {
            var current = GetCurrentPrice(product.PriceSchedules, marketId, now);
            if (current is not null)
                prices.Add(current);
        }

        decimal? priceMin = prices.Count == 0
            ? null
            : prices.Min(x => x.SalePrice);
        decimal? priceMax = prices.Count == 0
            ? null
            : prices.Max(x => x.SalePrice);
        decimal? listPrice = prices.Count == 0
            ? null
            : prices.Max(x => x.ListPrice);
        var discountPercent = CalculateDiscount(listPrice, priceMin);

        var imageUrl = product.Images
            .Where(x => x.ProductVariantId == null)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.ImageUrl)
            .FirstOrDefault();

        var stock = product.HasVariants
            ? product.Variants
                .Where(x => x.IsActive && !x.IsDeleted)
                .Sum(x => x.StockQuantity)
            : product.StockQuantity;

        return new StoreProductCardViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            CategoryName = product.Category.Name,
            BrandName = product.Brand.Name,
            ImageUrl = imageUrl,
            PriceMin = priceMin,
            PriceMax = priceMax,
            ListPrice = listPrice,
            DiscountPercent = discountPercent,
            CurrencyCode = currencyCode,
            StockQuantity = stock,
            IsFeatured = product.IsFeatured,
            HasVariants = product.HasVariants,
            CreatedAt = product.CreatedAt
        };
    }

    private static PriceSchedule? GetCurrentPrice(
        IEnumerable<PriceSchedule> schedules,
        int marketId,
        DateTime now)
    {
        if (marketId <= 0)
            return null;

        return schedules
            .Where(x =>
                x.IsActive &&
                x.MarketId == marketId &&
                x.ValidFrom <= now &&
                (!x.ValidTo.HasValue || x.ValidTo.Value > now))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    private static int? CalculateDiscount(decimal? listPrice, decimal? salePrice)
    {
        if (!listPrice.HasValue ||
            !salePrice.HasValue ||
            listPrice.Value <= 0 ||
            salePrice.Value >= listPrice.Value)
        {
            return null;
        }

        return (int)Math.Round(
            (listPrice.Value - salePrice.Value) /
            listPrice.Value * 100,
            MidpointRounding.AwayFromZero);
    }

    private sealed record StoreMarketContext(
        int Id,
        string Name,
        string CurrencyCode);
}

