using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;using TMDT1_TH.Infrastructure;


namespace TMDT1_TH.Data.Database;

public static class HouseholdCatalogSeeder
{
    private const string SeedSource = "CatalogSeeder";

    public static async Task TrySeedAsync(
        IServiceProvider services,
        ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            if (!await db.Database.CanConnectAsync())
                return;

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            var market = await GetOrCreateDefaultMarketAsync(db);
            var categories = await EnsureCategoriesAsync(db);
            var brands = await EnsureBrandsAsync(db);

            var addedCount = 0;
            var enrichedCount = 0;

            foreach (var seed in BuildProductSeeds())
            {
                var existing = await db.Products
                    .IgnoreQueryFilters()
                    .Include(x => x.Images)
                    .Include(x => x.Specifications)
                    .Include(x => x.PriceSchedules)
                    .FirstOrDefaultAsync(x => x.Sku == seed.Sku);

                if (existing is not null)
                {
                    if (!existing.IsDeleted &&
                        EnrichExistingProduct(
                            existing,
                            seed,
                            market))
                    {
                        enrichedCount++;
                    }

                    continue;
                }

                var product = CreateProduct(
                    seed,
                    categories,
                    brands,
                    market);

                db.Products.Add(product);
                addedCount++;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var visibleCount = await db.Products.CountAsync(x =>
                !x.IsDeleted &&
                x.Status == ProductStatus.Active);

            logger.LogInformation(
                "Catalog seeder completed: added {AddedCount}, " +
                "enriched {EnrichedCount}, active products {VisibleCount}.",
                addedCount,
                enrichedCount,
                visibleCount);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Skipped sample household catalog seeding. " +
                "Ensure the database migration has completed.");
        }
    }

    private static async Task<Market>
        GetOrCreateDefaultMarketAsync(ApplicationDbContext db)
    {
        var market = await db.Markets
            .OrderByDescending(x => x.IsActive && x.IsDefault)
            .ThenByDescending(x => x.IsActive)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (market is not null)
        {
            if (!market.IsActive)
                market.IsActive = true;

            if (string.IsNullOrWhiteSpace(market.CurrencyCode))
                market.CurrencyCode = "VND";

            return market;
        }

        market = new Market
        {
            Code = "ONLINE",
            Name = "Website",
            CurrencyCode = "VND",
            CountryCode = "VN",
            Description = "Thị trường mặc định của website.",
            IsDefault = true,
            IsActive = true,
            CreatedBy = SeedSource
        };

        db.Markets.Add(market);
        await db.SaveChangesAsync();
        return market;
    }

    private static async Task<Dictionary<string, Category>>
        EnsureCategoriesAsync(ApplicationDbContext db)
    {
        var result = new Dictionary<string, Category>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var seed in BuildCategorySeeds())
        {
            int? parentId = null;

            if (seed.ParentSlug is not null &&
                result.TryGetValue(seed.ParentSlug, out var parent))
            {
                parentId = parent.Id;
            }

            var category = await db.Categories
                .IgnoreQueryFilters()
                .Where(x => !x.IsDeleted && x.Slug == seed.Slug)
                .FirstOrDefaultAsync();

            if (category is null)
            {
                category = new Category
                {
                    Name = seed.Name,
                    Slug = seed.Slug,
                    ParentId = parentId,
                    Description = seed.Description,
                    ImageUrl = seed.ImagePath,
                    DisplayOrder = seed.DisplayOrder,
                    IsActive = true,
                    CreatedBy = SeedSource
                };

                db.Categories.Add(category);
                await db.SaveChangesAsync();
            }
            else
            {
                var changed = false;

                if (!category.IsActive)
                {
                    category.IsActive = true;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(category.Description))
                {
                    category.Description = seed.Description;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(category.ImageUrl))
                {
                    category.ImageUrl = seed.ImagePath;
                    changed = true;
                }

                if (category.ParentId is null && parentId.HasValue)
                {
                    category.ParentId = parentId;
                    changed = true;
                }

                if (changed)
                {
                    category.UpdatedBy = SeedSource;
                    await db.SaveChangesAsync();
                }
            }

            result[seed.Slug] = category;
        }

        return result;
    }

    private static async Task<Dictionary<string, Brand>>
        EnsureBrandsAsync(ApplicationDbContext db)
    {
        var result = new Dictionary<string, Brand>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var seed in BuildBrandSeeds())
        {
            var brand = await db.Brands
                .IgnoreQueryFilters()
                .Where(x => !x.IsDeleted && x.Slug == seed.Slug)
                .FirstOrDefaultAsync();

            if (brand is null)
            {
                brand = new Brand
                {
                    Name = seed.Name,
                    Slug = seed.Slug,
                    Country = seed.Country,
                    Description =
                        $"Thương hiệu mẫu {seed.Name} dành cho " +
                        "website đồ gia dụng Mây Home.",
                    IsActive = true,
                    CreatedBy = SeedSource
                };

                db.Brands.Add(brand);
                await db.SaveChangesAsync();
            }
            else if (!brand.IsActive)
            {
                brand.IsActive = true;
                brand.UpdatedBy = SeedSource;
                await db.SaveChangesAsync();
            }

            result[seed.Slug] = brand;
        }

        return result;
    }

    private static Product CreateProduct(
        ProductSeed seed,
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlyDictionary<string, Brand> brands,
        Market market)
    {
        var product = new Product
        {
            Category = categories[seed.CategorySlug],
            Brand = brands[seed.BrandSlug],
            Name = seed.Name,
            Slug = seed.Slug,
            Sku = seed.Sku,
            ModelNumber = seed.ModelNumber,
            Unit = seed.Unit,
            ShortDescription = seed.ShortDescription,
            Description = seed.Description,
            CountryOfOrigin = "Việt Nam",
            ManufacturerName = seed.ManufacturerName,
            ManufacturerAddress =
                "Khu công nghiệp Tân Bình, " +
                "Thành phố Hồ Chí Minh, Việt Nam",
            WarrantyMonths = seed.WarrantyMonths,
            Status = ProductStatus.Active,
            IsFeatured = seed.IsFeatured,
            HasVariants = seed.Variants.Count > 0,
            StockQuantity =
                seed.Variants.Count > 0
                    ? 0
                    : seed.StockQuantity,
            LowStockThreshold = 5,
            MinPurchaseQuantity = 1,
            MaxPurchaseQuantity = 10,
            Weight = seed.Weight,
            PackageLengthCm = seed.PackageLengthCm,
            PackageWidthCm = seed.PackageWidthCm,
            PackageHeightCm = seed.PackageHeightCm,
            CreatedBy = SeedSource
        };

        AddPresentationData(product, seed);

        if (seed.Variants.Count == 0)
        {
            product.PriceSchedules.Add(
                CreateProductPrice(
                    product,
                    market,
                    seed.CostPrice,
                    seed.ListPrice,
                    seed.SalePrice));
        }
        else
        {
            AddVariants(product, seed, market);
        }

        return product;
    }

    private static void AddPresentationData(
        Product product,
        ProductSeed seed)
    {
        product.Images.Add(new ProductImage
        {
            Product = product,
            ImageUrl = seed.ImagePath,
            AltText = seed.Name,
            DisplayOrder = 1,
            IsPrimary = true,
            CreatedBy = SeedSource
        });

        for (var index = 0;
             index < seed.Specifications.Count;
             index++)
        {
            var specification = seed.Specifications[index];

            product.Specifications.Add(
                new ProductSpecification
                {
                    Product = product,
                    Name = specification.Name,
                    Value = specification.Value,
                    DisplayOrder = index + 1,
                    CreatedBy = SeedSource
                });
        }
    }

    private static void AddVariants(
        Product product,
        ProductSeed seed,
        Market market)
    {
        var option = new ProductOption
        {
            Product = product,
            Name = seed.OptionName!,
            DisplayOrder = 1,
            CreatedBy = SeedSource
        };

        product.Options.Add(option);

        for (var index = 0;
             index < seed.Variants.Count;
             index++)
        {
            var variantSeed = seed.Variants[index];

            var optionValue = new ProductOptionValue
            {
                ProductOption = option,
                Value = variantSeed.Value,
                DisplayOrder = index + 1,
                CreatedBy = SeedSource
            };

            option.Values.Add(optionValue);

            var variant = new ProductVariant
            {
                Product = product,
                Sku = variantSeed.Sku,
                Name = variantSeed.Value,
                CombinationKey =
                    $"1={NormalizeToken(variantSeed.Value)}",
                StockQuantity = variantSeed.StockQuantity,
                LowStockThreshold = 5,
                SortOrder = index + 1,
                Weight = variantSeed.Weight,
                IsDefault = index == 0,
                IsActive = true,
                CreatedBy = SeedSource
            };

            variant.VariantValues.Add(
                new ProductVariantValue
                {
                    ProductVariant = variant,
                    ProductOptionValue = optionValue
                });

            variant.PriceSchedules.Add(
                CreateVariantPrice(
                    variant,
                    market,
                    variantSeed.CostPrice,
                    variantSeed.ListPrice,
                    variantSeed.SalePrice));

            product.Variants.Add(variant);
        }
    }

    private static bool EnrichExistingProduct(
        Product product,
        ProductSeed seed,
        Market market)
    {
        var changed = false;

        if (product.Images.Count == 0)
        {
            product.Images.Add(new ProductImage
            {
                Product = product,
                ImageUrl = seed.ImagePath,
                AltText = product.Name,
                DisplayOrder = 1,
                IsPrimary = true,
                CreatedBy = SeedSource
            });

            changed = true;
        }

        if (product.Specifications.Count == 0)
        {
            for (var index = 0;
                 index < seed.Specifications.Count;
                 index++)
            {
                var specification = seed.Specifications[index];

                product.Specifications.Add(
                    new ProductSpecification
                    {
                        Product = product,
                        Name = specification.Name,
                        Value = specification.Value,
                        DisplayOrder = index + 1,
                        CreatedBy = SeedSource
                    });
            }

            changed = true;
        }

        changed |= FillIfMissing(product, seed);

        if (!product.HasVariants &&
            !product.PriceSchedules.Any(x =>
                x.MarketId == market.Id &&
                x.IsActive))
        {
            product.PriceSchedules.Add(
                CreateProductPrice(
                    product,
                    market,
                    seed.CostPrice,
                    seed.ListPrice,
                    seed.SalePrice));

            changed = true;
        }

        if (changed)
            product.UpdatedBy = SeedSource;

        return changed;
    }

    private static bool FillIfMissing(
        Product product,
        ProductSeed seed)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(product.ShortDescription))
        {
            product.ShortDescription = seed.ShortDescription;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(product.Description))
        {
            product.Description = seed.Description;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(product.CountryOfOrigin))
        {
            product.CountryOfOrigin = "Việt Nam";
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(product.ManufacturerName))
        {
            product.ManufacturerName = seed.ManufacturerName;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(product.ManufacturerAddress))
        {
            product.ManufacturerAddress =
                "Khu công nghiệp Tân Bình, " +
                "Thành phố Hồ Chí Minh, Việt Nam";

            changed = true;
        }

        if (!product.WarrantyMonths.HasValue)
        {
            product.WarrantyMonths = seed.WarrantyMonths;
            changed = true;
        }

        if (!product.Weight.HasValue)
        {
            product.Weight = seed.Weight;
            changed = true;
        }

        if (!product.PackageLengthCm.HasValue)
        {
            product.PackageLengthCm = seed.PackageLengthCm;
            changed = true;
        }

        if (!product.PackageWidthCm.HasValue)
        {
            product.PackageWidthCm = seed.PackageWidthCm;
            changed = true;
        }

        if (!product.PackageHeightCm.HasValue)
        {
            product.PackageHeightCm = seed.PackageHeightCm;
            changed = true;
        }

        if (seed.IsFeatured && !product.IsFeatured)
        {
            product.IsFeatured = true;
            changed = true;
        }

        return changed;
    }

    private static PriceSchedule CreateProductPrice(
        Product product,
        Market market,
        decimal costPrice,
        decimal listPrice,
        decimal salePrice) =>
        new()
        {
            Product = product,
            Market = market,
            CostPrice = costPrice,
            ListPrice = listPrice,
            SalePrice = salePrice,
            ValidFrom = DateTime.Now.AddDays(-30),
            ValidTo = null,
            IsActive = true,
            Note = "Giá mẫu mặc định",
            CreatedBy = SeedSource
        };

    private static PriceSchedule CreateVariantPrice(
        ProductVariant variant,
        Market market,
        decimal costPrice,
        decimal listPrice,
        decimal salePrice) =>
        new()
        {
            ProductVariant = variant,
            Market = market,
            CostPrice = costPrice,
            ListPrice = listPrice,
            SalePrice = salePrice,
            ValidFrom = DateTime.Now.AddDays(-30),
            ValidTo = null,
            IsActive = true,
            Note = "Giá mẫu theo biến thể",
            CreatedBy = SeedSource
        };

    private static string NormalizeToken(string value)
    {
        var token = SlugHelper.Generate(value)
            .Replace("-", string.Empty)
            .ToUpperInvariant();

        return string.IsNullOrWhiteSpace(token)
            ? "VAR"
            : token;
    }

    private static IReadOnlyList<SeedCategory>
        BuildCategorySeeds() =>
        new SeedCategory[]
        {
        new SeedCategory(
            Slug: "nha-bep",
            Name: "Nhà bếp",
            ParentSlug: null,
            DisplayOrder: 10,
            Description: "Đồ dùng thiết yếu giúp việc chuẩn bị và bảo quản thực phẩm thuận tiện hơn.",
            ImagePath: "/images/catalog-seed/categories/nha-bep.svg"),
        new SeedCategory(
            Slug: "noi-chao",
            Name: "Nồi & chảo",
            ParentSlug: "nha-bep",
            DisplayOrder: 11,
            Description: "Nồi, chảo và dụng cụ nấu phù hợp cho nhiều loại bếp gia đình.",
            ImagePath: "/images/catalog-seed/categories/noi-chao.svg"),
        new SeedCategory(
            Slug: "dung-cu-nha-bep",
            Name: "Dụng cụ nhà bếp",
            ParentSlug: "nha-bep",
            DisplayOrder: 12,
            Description: "Dụng cụ sơ chế, sắp xếp và phục vụ bữa ăn hằng ngày.",
            ImagePath: "/images/catalog-seed/categories/dung-cu-nha-bep.svg"),
        new SeedCategory(
            Slug: "luu-tru-thuc-pham",
            Name: "Lưu trữ thực phẩm",
            ParentSlug: "nha-bep",
            DisplayOrder: 13,
            Description: "Hộp và bộ bảo quản giúp thực phẩm gọn gàng, sạch sẽ và dễ phân loại.",
            ImagePath: "/images/catalog-seed/categories/luu-tru-thuc-pham.svg"),
        new SeedCategory(
            Slug: "dien-gia-dung",
            Name: "Điện gia dụng",
            ParentSlug: null,
            DisplayOrder: 20,
            Description: "Thiết bị điện gia đình tiện lợi, dễ sử dụng và phù hợp không gian hiện đại.",
            ImagePath: "/images/catalog-seed/categories/dien-gia-dung.svg"),
        new SeedCategory(
            Slug: "cham-soc-khong-khi",
            Name: "Chăm sóc không khí",
            ParentSlug: null,
            DisplayOrder: 25,
            Description: "Thiết bị hỗ trợ cải thiện độ ẩm và chất lượng không khí trong phòng.",
            ImagePath: "/images/catalog-seed/categories/cham-soc-khong-khi.svg"),
        new SeedCategory(
            Slug: "ve-sinh-nha-cua",
            Name: "Vệ sinh nhà cửa",
            ParentSlug: null,
            DisplayOrder: 30,
            Description: "Dụng cụ vệ sinh giúp chăm sóc sàn nhà và không gian sống nhanh hơn.",
            ImagePath: "/images/catalog-seed/categories/ve-sinh-nha-cua.svg"),
        new SeedCategory(
            Slug: "luu-tru-sap-xep",
            Name: "Lưu trữ & sắp xếp",
            ParentSlug: null,
            DisplayOrder: 40,
            Description: "Kệ, tủ và giỏ giúp tối ưu diện tích sử dụng trong gia đình.",
            ImagePath: "/images/catalog-seed/categories/luu-tru-sap-xep.svg"),
        new SeedCategory(
            Slug: "phong-ngu",
            Name: "Phòng ngủ",
            ParentSlug: null,
            DisplayOrder: 50,
            Description: "Sản phẩm mềm mại và tiện nghi dành cho không gian nghỉ ngơi.",
            ImagePath: "/images/catalog-seed/categories/phong-ngu.svg"),
        new SeedCategory(
            Slug: "phong-tam",
            Name: "Phòng tắm",
            ParentSlug: null,
            DisplayOrder: 60,
            Description: "Đồ dùng phòng tắm gọn đẹp, chống ẩm và dễ vệ sinh.",
            ImagePath: "/images/catalog-seed/categories/phong-tam.svg")
        };

    private static IReadOnlyList<SeedBrand>
        BuildBrandSeeds() =>
        new SeedBrand[]
        {
        new SeedBrand("may-home", "Mây Home", "Việt Nam"),
        new SeedBrand("bep-xinh", "Bếp Xinh", "Việt Nam"),
        new SeedBrand("purenest", "PureNest", "Việt Nam"),
        new SeedBrand("cleanjoy", "CleanJoy", "Việt Nam"),
        new SeedBrand("lumihome", "LumiHome", "Việt Nam"),
        new SeedBrand("cozynest", "CozyNest", "Việt Nam"),
        new SeedBrand("aquasense", "AquaSense", "Việt Nam"),
        new SeedBrand("airleaf", "AirLeaf", "Việt Nam")
        };

    private static IReadOnlyList<ProductSeed>
        BuildProductSeeds() =>
        new ProductSeed[]
        {
        WithVariants(
            categorySlug: "dien-gia-dung",
            brandSlug: "may-home",
            name: "Nồi chiên không dầu AirCare",
            slug: "noi-chien-khong-dau-aircare",
            sku: "HOME-AF",
            modelNumber: "MH-AF2026",
            unit: "Cái",
            shortDescription: "Nồi chiên cảm ứng với nhiều chế độ nấu, lòng nồi chống dính dễ vệ sinh.",
            description: "Nồi chiên không dầu AirCare hỗ trợ chiên, nướng và làm nóng thực phẩm với lượng dầu ít hơn. Bảng điều khiển cảm ứng rõ ràng, khay chiên tháo rời và chế độ hẹn giờ phù hợp cho gia đình bận rộn.",
            manufacturerName: "Công ty TNHH Mây Home",
            warrantyMonths: 24,
            isFeatured: true,
            weight: 5.2m,
            packageLengthCm: 38m,
            packageWidthCm: 34m,
            packageHeightCm: 36m,
            imagePath: "/images/catalog-seed/products/noi-chien-khong-dau-aircare.svg",
            optionName: "Dung tích",
            variants: new[]
            {
                new SeedVariant(
                    Value: "4 lít",
                    Sku: "HOME-AF-4L",
                    StockQuantity: 18,
                    Weight: 4.4m,
                    CostPrice: 920000m,
                    ListPrice: 1990000m,
                    SalePrice: 1590000m),
                new SeedVariant(
                    Value: "6 lít",
                    Sku: "HOME-AF-6L",
                    StockQuantity: 36,
                    Weight: 5.2m,
                    CostPrice: 1180000m,
                    ListPrice: 2490000m,
                    SalePrice: 1890000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Công suất", "1.500–1.700 W"),
                new SeedSpecification("Điều khiển", "Cảm ứng"),
                new SeedSpecification("Chất liệu lòng nồi", "Hợp kim phủ chống dính")
            }),
        WithVariants(
            categorySlug: "noi-chao",
            brandSlug: "bep-xinh",
            name: "Chảo chống dính Ceramic Glow",
            slug: "chao-chong-dinh-ceramic-glow",
            sku: "HOME-PAN-CG",
            modelNumber: "BX-CG2026",
            unit: "Cái",
            shortDescription: "Chảo phủ ceramic, tay cầm cách nhiệt và dùng được trên nhiều loại bếp.",
            description: "Chảo Ceramic Glow có lớp phủ chống dính dễ làm sạch, thành chảo vừa phải và tay cầm chống nóng. Đáy truyền nhiệt ổn định, phù hợp chiên áp chảo và chế biến món ăn hằng ngày.",
            manufacturerName: "Công ty TNHH Bếp Xinh",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 1.1m,
            packageLengthCm: 48m,
            packageWidthCm: 30m,
            packageHeightCm: 9m,
            imagePath: "/images/catalog-seed/products/chao-chong-dinh-ceramic-glow.svg",
            optionName: "Kích thước",
            variants: new[]
            {
                new SeedVariant(
                    Value: "24 cm",
                    Sku: "HOME-PAN-CG-24",
                    StockQuantity: 42,
                    Weight: 0.9m,
                    CostPrice: 190000m,
                    ListPrice: 449000m,
                    SalePrice: 359000m),
                new SeedVariant(
                    Value: "28 cm",
                    Sku: "HOME-PAN-CG-28",
                    StockQuantity: 30,
                    Weight: 1.1m,
                    CostPrice: 230000m,
                    ListPrice: 549000m,
                    SalePrice: 429000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Lớp phủ", "Ceramic chống dính"),
                new SeedSpecification("Loại bếp", "Gas, điện, hồng ngoại"),
                new SeedSpecification("Tay cầm", "Cách nhiệt")
            }),
        Simple(
            categorySlug: "luu-tru-thuc-pham",
            brandSlug: "purenest",
            name: "Bộ hộp bảo quản FreshBox 5 món",
            slug: "bo-hop-bao-quan-freshbox-5-mon",
            sku: "HOME-BOX-FB5",
            modelNumber: "PN-FB5",
            unit: "Bộ",
            shortDescription: "Bộ hộp kín khí nhiều kích thước, phù hợp bảo quản thực phẩm khô và thức ăn.",
            description: "FreshBox gồm năm hộp có nắp kín, dễ xếp chồng và phân loại thực phẩm trong tủ lạnh. Chất liệu dùng cho thực phẩm, bề mặt trong suốt giúp quan sát nhanh và thuận tiện khi vệ sinh.",
            manufacturerName: "Công ty TNHH PureNest",
            warrantyMonths: 6,
            isFeatured: false,
            weight: 1.3m,
            packageLengthCm: 34m,
            packageWidthCm: 25m,
            packageHeightCm: 19m,
            imagePath: "/images/catalog-seed/products/bo-hop-bao-quan-freshbox-5-mon.svg",
            stockQuantity: 29,
            costPrice: 145000m,
            listPrice: 349000m,
            salePrice: 289000m,
            specifications: new[]
            {
                new SeedSpecification("Số lượng", "5 hộp"),
                new SeedSpecification("Chất liệu", "Nhựa dùng cho thực phẩm"),
                new SeedSpecification("Đặc điểm", "Nắp kín, xếp chồng")
            }),
        Simple(
            categorySlug: "ve-sinh-nha-cua",
            brandSlug: "cleanjoy",
            name: "Cây lau nhà xoay 360 CleanSpin",
            slug: "cay-lau-nha-xoay-360-cleanspin",
            sku: "HOME-MOP-CS",
            modelNumber: "CJ-CS360",
            unit: "Bộ",
            shortDescription: "Bộ lau nhà xoay 360 độ, lồng vắt inox và bông lau microfiber.",
            description: "CleanSpin hỗ trợ vắt khô nhanh bằng cơ chế xoay, thân cây điều chỉnh chiều dài và đầu lau linh hoạt. Bông microfiber thấm hút tốt, phù hợp sàn gạch, sàn gỗ và khu vực có nhiều góc hẹp.",
            manufacturerName: "Công ty TNHH CleanJoy",
            warrantyMonths: 6,
            isFeatured: false,
            weight: 2.4m,
            packageLengthCm: 48m,
            packageWidthCm: 29m,
            packageHeightCm: 31m,
            imagePath: "/images/catalog-seed/products/cay-lau-nha-xoay-360-cleanspin.svg",
            stockQuantity: 26,
            costPrice: 185000m,
            listPrice: 449000m,
            salePrice: 359000m,
            specifications: new[]
            {
                new SeedSpecification("Chất liệu lồng vắt", "Inox"),
                new SeedSpecification("Bông lau", "Microfiber"),
                new SeedSpecification("Chiều dài cán", "Điều chỉnh")
            }),
        Simple(
            categorySlug: "luu-tru-sap-xep",
            brandSlug: "may-home",
            name: "Kệ đa năng FlexiRack 4 tầng",
            slug: "ke-da-nang-flexirack-4-tang",
            sku: "HOME-RACK-F4",
            modelNumber: "MH-FR4",
            unit: "Cái",
            shortDescription: "Kệ lắp ghép bốn tầng dùng cho nhà bếp, phòng tắm hoặc ban công.",
            description: "FlexiRack có thiết kế bốn tầng thoáng, khung chắc chắn và chân đế ổn định. Kệ phù hợp sắp xếp gia vị, đồ dùng vệ sinh hoặc vật dụng nhỏ, giúp tận dụng không gian theo chiều dọc.",
            manufacturerName: "Công ty TNHH Mây Home",
            warrantyMonths: 12,
            isFeatured: false,
            weight: 4.8m,
            packageLengthCm: 69m,
            packageWidthCm: 35m,
            packageHeightCm: 18m,
            imagePath: "/images/catalog-seed/products/ke-da-nang-flexirack-4-tang.svg",
            stockQuantity: 24,
            costPrice: 310000m,
            listPrice: 699000m,
            salePrice: 549000m,
            specifications: new[]
            {
                new SeedSpecification("Số tầng", "4"),
                new SeedSpecification("Chất liệu", "Thép sơn tĩnh điện"),
                new SeedSpecification("Tải trọng khuyến nghị", "Tối đa 40 kg")
            }),
        WithVariants(
            categorySlug: "dien-gia-dung",
            brandSlug: "lumihome",
            name: "Nồi cơm điện LumiCook",
            slug: "noi-com-dien-lumicook",
            sku: "HOME-RICE-RC",
            modelNumber: "LH-RC26",
            unit: "Cái",
            shortDescription: "Nồi cơm điện giữ ấm ổn định, lòng nồi chống dính và thao tác đơn giản.",
            description: "LumiCook được thiết kế cho nhu cầu nấu cơm hằng ngày với mâm nhiệt ổn định, van thoát hơi dễ tháo và lòng nồi phủ chống dính. Tay xách chắc chắn, phù hợp căn hộ và gia đình nhỏ.",
            manufacturerName: "Công ty TNHH LumiHome",
            warrantyMonths: 18,
            isFeatured: true,
            weight: 3.1m,
            packageLengthCm: 31m,
            packageWidthCm: 31m,
            packageHeightCm: 29m,
            imagePath: "/images/catalog-seed/products/noi-com-dien-lumicook.svg",
            optionName: "Dung tích",
            variants: new[]
            {
                new SeedVariant(
                    Value: "1,2 lít",
                    Sku: "HOME-RICE-RC-12",
                    StockQuantity: 28,
                    Weight: 2.7m,
                    CostPrice: 430000m,
                    ListPrice: 899000m,
                    SalePrice: 729000m),
                new SeedVariant(
                    Value: "1,8 lít",
                    Sku: "HOME-RICE-RC-18",
                    StockQuantity: 35,
                    Weight: 3.1m,
                    CostPrice: 520000m,
                    ListPrice: 1099000m,
                    SalePrice: 859000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Chế độ", "Nấu và giữ ấm"),
                new SeedSpecification("Lòng nồi", "Chống dính"),
                new SeedSpecification("Phụ kiện", "Cốc đong và muỗng cơm")
            }),
        Simple(
            categorySlug: "dien-gia-dung",
            brandSlug: "lumihome",
            name: "Ấm siêu tốc LumiKettle 1,7 lít",
            slug: "am-sieu-toc-lumikettle-17-lit",
            sku: "HOME-KETTLE-K17",
            modelNumber: "LH-K17",
            unit: "Cái",
            shortDescription: "Ấm đun nước dung tích 1,7 lít, tự ngắt khi sôi và có đế xoay 360 độ.",
            description: "LumiKettle sử dụng mâm nhiệt phẳng, có đèn báo hoạt động và cơ chế tự ngắt khi nước sôi hoặc cạn. Thân ấm gọn, miệng rót thuận tiện và đế tiếp điện xoay linh hoạt.",
            manufacturerName: "Công ty TNHH LumiHome",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 1.15m,
            packageLengthCm: 22m,
            packageWidthCm: 19m,
            packageHeightCm: 25m,
            imagePath: "/images/catalog-seed/products/am-sieu-toc-lumikettle-17-lit.svg",
            stockQuantity: 41,
            costPrice: 220000m,
            listPrice: 499000m,
            salePrice: 389000m,
            specifications: new[]
            {
                new SeedSpecification("Dung tích", "1,7 lít"),
                new SeedSpecification("Công suất", "1.850 W"),
                new SeedSpecification("An toàn", "Tự ngắt khi sôi")
            }),
        WithVariants(
            categorySlug: "dien-gia-dung",
            brandSlug: "lumihome",
            name: "Máy xay sinh tố BlendMate",
            slug: "may-xay-sinh-to-blendmate",
            sku: "HOME-BLEND-BL",
            modelNumber: "LH-BL26",
            unit: "Cái",
            shortDescription: "Máy xay hai tốc độ, cối trong suốt và lưỡi dao thép không gỉ.",
            description: "BlendMate phù hợp xay sinh tố, sốt và thực phẩm mềm. Cối có vạch dung tích rõ ràng, nắp khóa chắc và chân đế chống trượt giúp sử dụng ổn định trên mặt bàn.",
            manufacturerName: "Công ty TNHH LumiHome",
            warrantyMonths: 18,
            isFeatured: false,
            weight: 2.6m,
            packageLengthCm: 24m,
            packageWidthCm: 22m,
            packageHeightCm: 39m,
            imagePath: "/images/catalog-seed/products/may-xay-sinh-to-blendmate.svg",
            optionName: "Dung tích cối",
            variants: new[]
            {
                new SeedVariant(
                    Value: "1,2 lít",
                    Sku: "HOME-BLEND-BL-12",
                    StockQuantity: 22,
                    Weight: 2.3m,
                    CostPrice: 520000m,
                    ListPrice: 1099000m,
                    SalePrice: 849000m),
                new SeedVariant(
                    Value: "1,5 lít",
                    Sku: "HOME-BLEND-BL-15",
                    StockQuantity: 26,
                    Weight: 2.6m,
                    CostPrice: 610000m,
                    ListPrice: 1299000m,
                    SalePrice: 979000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Tốc độ", "2 tốc độ và nhồi"),
                new SeedSpecification("Lưỡi dao", "Thép không gỉ"),
                new SeedSpecification("Chân đế", "Chống trượt")
            }),
        Simple(
            categorySlug: "ve-sinh-nha-cua",
            brandSlug: "cleanjoy",
            name: "Máy hút bụi cầm tay CleanVac V12",
            slug: "may-hut-bui-cam-tay-cleanvac-v12",
            sku: "HOME-VAC-V12",
            modelNumber: "CJ-V12",
            unit: "Cái",
            shortDescription: "Máy hút bụi cầm tay gọn nhẹ, đầu hút khe và hộp bụi dễ tháo.",
            description: "CleanVac V12 hỗ trợ vệ sinh bàn, ghế, xe hơi và các khe nhỏ. Thiết kế không dây thuận tiện di chuyển, bộ lọc có thể vệ sinh và hộp bụi trong suốt dễ theo dõi.",
            manufacturerName: "Công ty TNHH CleanJoy",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 1.4m,
            packageLengthCm: 42m,
            packageWidthCm: 14m,
            packageHeightCm: 16m,
            imagePath: "/images/catalog-seed/products/may-hut-bui-cam-tay-cleanvac-v12.svg",
            stockQuantity: 33,
            costPrice: 640000m,
            listPrice: 1399000m,
            salePrice: 1099000m,
            specifications: new[]
            {
                new SeedSpecification("Kiểu máy", "Không dây"),
                new SeedSpecification("Phụ kiện", "Đầu hút khe"),
                new SeedSpecification("Bộ lọc", "Có thể vệ sinh")
            }),
        Simple(
            categorySlug: "dien-gia-dung",
            brandSlug: "airleaf",
            name: "Quạt tuần hoàn AirFlow F8",
            slug: "quat-tuan-hoan-airflow-f8",
            sku: "HOME-FAN-F8",
            modelNumber: "AL-F8",
            unit: "Cái",
            shortDescription: "Quạt tuần hoàn ba tốc độ, góc xoay rộng và vận hành êm.",
            description: "AirFlow F8 tạo luồng gió tập trung để hỗ trợ lưu thông không khí trong phòng. Quạt có ba mức gió, điều chỉnh hướng linh hoạt và chân đế nhỏ gọn phù hợp bàn làm việc hoặc phòng ngủ.",
            manufacturerName: "Công ty TNHH AirLeaf",
            warrantyMonths: 18,
            isFeatured: false,
            weight: 2.2m,
            packageLengthCm: 29m,
            packageWidthCm: 24m,
            packageHeightCm: 38m,
            imagePath: "/images/catalog-seed/products/quat-tuan-hoan-airflow-f8.svg",
            stockQuantity: 37,
            costPrice: 480000m,
            listPrice: 999000m,
            salePrice: 789000m,
            specifications: new[]
            {
                new SeedSpecification("Tốc độ gió", "3 mức"),
                new SeedSpecification("Góc điều chỉnh", "Linh hoạt"),
                new SeedSpecification("Độ ồn", "Thấp")
            }),
        Simple(
            categorySlug: "cham-soc-khong-khi",
            brandSlug: "airleaf",
            name: "Máy lọc không khí AirLeaf Pure 20",
            slug: "may-loc-khong-khi-airleaf-pure-20",
            sku: "HOME-AIR-AP20",
            modelNumber: "AL-AP20",
            unit: "Cái",
            shortDescription: "Máy lọc không khí cho phòng nhỏ, có bộ lọc nhiều lớp và chế độ ngủ.",
            description: "AirLeaf Pure 20 hỗ trợ lọc bụi và mùi trong phòng ngủ hoặc phòng làm việc. Thiết bị có ba mức gió, chế độ ngủ giảm ánh sáng và cảnh báo thời điểm cần vệ sinh bộ lọc.",
            manufacturerName: "Công ty TNHH AirLeaf",
            warrantyMonths: 24,
            isFeatured: true,
            weight: 4.1m,
            packageLengthCm: 25m,
            packageWidthCm: 25m,
            packageHeightCm: 45m,
            imagePath: "/images/catalog-seed/products/may-loc-khong-khi-airleaf-pure-20.svg",
            stockQuantity: 19,
            costPrice: 1450000m,
            listPrice: 2999000m,
            salePrice: 2399000m,
            specifications: new[]
            {
                new SeedSpecification("Diện tích khuyến nghị", "Tối đa 25 m²"),
                new SeedSpecification("Bộ lọc", "Nhiều lớp"),
                new SeedSpecification("Chế độ", "Ngủ và tự động")
            }),
        Simple(
            categorySlug: "cham-soc-khong-khi",
            brandSlug: "airleaf",
            name: "Máy tạo ẩm AirLeaf Mist H3",
            slug: "may-tao-am-airleaf-mist-h3",
            sku: "HOME-HUM-H3",
            modelNumber: "AL-H3",
            unit: "Cái",
            shortDescription: "Máy tạo ẩm dung tích 3 lít, phun sương mịn và tự ngắt khi cạn nước.",
            description: "AirLeaf Mist H3 bổ sung độ ẩm cho phòng điều hòa với bình nước dễ châm và núm điều chỉnh lượng sương. Thiết bị tự ngắt khi hết nước và có đèn báo dịu nhẹ.",
            manufacturerName: "Công ty TNHH AirLeaf",
            warrantyMonths: 12,
            isFeatured: false,
            weight: 1.35m,
            packageLengthCm: 22m,
            packageWidthCm: 22m,
            packageHeightCm: 31m,
            imagePath: "/images/catalog-seed/products/may-tao-am-airleaf-mist-h3.svg",
            stockQuantity: 31,
            costPrice: 390000m,
            listPrice: 849000m,
            salePrice: 669000m,
            specifications: new[]
            {
                new SeedSpecification("Dung tích", "3 lít"),
                new SeedSpecification("Thời gian hoạt động", "Tối đa 12 giờ"),
                new SeedSpecification("An toàn", "Tự ngắt khi cạn nước")
            }),
        WithVariants(
            categorySlug: "luu-tru-thuc-pham",
            brandSlug: "purenest",
            name: "Bộ hộp thủy tinh GlassLock",
            slug: "bo-hop-thuy-tinh-glasslock",
            sku: "HOME-GLASS-G5",
            modelNumber: "PN-GL26",
            unit: "Bộ",
            shortDescription: "Hộp thủy tinh chịu nhiệt có nắp khóa, dùng bảo quản và hâm nóng thực phẩm.",
            description: "GlassLock sử dụng thân thủy tinh trong suốt, nắp khóa bốn cạnh và gioăng tháo rời để vệ sinh. Bộ hộp phù hợp chuẩn bị bữa ăn, bảo quản trong tủ lạnh và hâm nóng khi tháo nắp.",
            manufacturerName: "Công ty TNHH PureNest",
            warrantyMonths: 6,
            isFeatured: true,
            weight: 2.4m,
            packageLengthCm: 36m,
            packageWidthCm: 27m,
            packageHeightCm: 18m,
            imagePath: "/images/catalog-seed/products/bo-hop-thuy-tinh-glasslock.svg",
            optionName: "Quy cách",
            variants: new[]
            {
                new SeedVariant(
                    Value: "Bộ 3 hộp",
                    Sku: "HOME-GLASS-G3",
                    StockQuantity: 25,
                    Weight: 1.6m,
                    CostPrice: 260000m,
                    ListPrice: 599000m,
                    SalePrice: 469000m),
                new SeedVariant(
                    Value: "Bộ 5 hộp",
                    Sku: "HOME-GLASS-G5V",
                    StockQuantity: 20,
                    Weight: 2.4m,
                    CostPrice: 410000m,
                    ListPrice: 899000m,
                    SalePrice: 699000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Chất liệu thân", "Thủy tinh chịu nhiệt"),
                new SeedSpecification("Nắp", "Khóa bốn cạnh"),
                new SeedSpecification("Sử dụng", "Tủ lạnh và lò vi sóng khi tháo nắp")
            }),
        Simple(
            categorySlug: "dung-cu-nha-bep",
            brandSlug: "may-home",
            name: "Kệ úp chén DishDry 2 tầng",
            slug: "ke-up-chen-dishdry-2-tang",
            sku: "HOME-DISH-D2",
            modelNumber: "MH-DD2",
            unit: "Cái",
            shortDescription: "Kệ úp chén hai tầng có khay hứng nước và khu vực để muỗng đũa.",
            description: "DishDry bố trí hai tầng giúp phân loại chén, đĩa và ly gọn gàng. Khay hứng nước tháo rời, chân chống trượt và khung sơn chống ẩm phù hợp khu vực bồn rửa.",
            manufacturerName: "Công ty TNHH Mây Home",
            warrantyMonths: 12,
            isFeatured: false,
            weight: 3.2m,
            packageLengthCm: 45m,
            packageWidthCm: 30m,
            packageHeightCm: 24m,
            imagePath: "/images/catalog-seed/products/ke-up-chen-dishdry-2-tang.svg",
            stockQuantity: 27,
            costPrice: 420000m,
            listPrice: 899000m,
            salePrice: 699000m,
            specifications: new[]
            {
                new SeedSpecification("Số tầng", "2"),
                new SeedSpecification("Phụ kiện", "Khay hứng nước và ống đũa"),
                new SeedSpecification("Chất liệu", "Thép sơn chống ẩm")
            }),
        Simple(
            categorySlug: "dung-cu-nha-bep",
            brandSlug: "bep-xinh",
            name: "Bộ dao bếp SharpLine 6 món",
            slug: "bo-dao-bep-sharpline-6-mon",
            sku: "HOME-KNIFE-K6",
            modelNumber: "BX-SL6",
            unit: "Bộ",
            shortDescription: "Bộ dao sáu món kèm giá cắm, phù hợp sơ chế thực phẩm hằng ngày.",
            description: "SharpLine gồm các loại dao cơ bản cho thái, gọt và cắt thực phẩm. Lưỡi thép không gỉ, tay cầm vừa tay và giá cắm giúp bảo quản dao gọn, hạn chế tiếp xúc trực tiếp.",
            manufacturerName: "Công ty TNHH Bếp Xinh",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 2.1m,
            packageLengthCm: 24m,
            packageWidthCm: 16m,
            packageHeightCm: 36m,
            imagePath: "/images/catalog-seed/products/bo-dao-bep-sharpline-6-mon.svg",
            stockQuantity: 21,
            costPrice: 560000m,
            listPrice: 1199000m,
            salePrice: 929000m,
            specifications: new[]
            {
                new SeedSpecification("Số món", "6"),
                new SeedSpecification("Lưỡi dao", "Thép không gỉ"),
                new SeedSpecification("Phụ kiện", "Giá cắm dao")
            }),
        Simple(
            categorySlug: "dung-cu-nha-bep",
            brandSlug: "purenest",
            name: "Bộ thớt kháng khuẩn DuoBoard",
            slug: "bo-thot-khang-khuan-duoboard",
            sku: "HOME-BOARD-B2",
            modelNumber: "PN-DB2",
            unit: "Bộ",
            shortDescription: "Hai thớt riêng cho thực phẩm sống và chín, có rãnh chống tràn.",
            description: "DuoBoard giúp phân loại sơ chế bằng hai màu nhận diện khác nhau. Bề mặt ít bám mùi, viền có rãnh hứng nước và lỗ treo giúp thớt khô nhanh sau khi vệ sinh.",
            manufacturerName: "Công ty TNHH PureNest",
            warrantyMonths: 3,
            isFeatured: false,
            weight: 1.5m,
            packageLengthCm: 38m,
            packageWidthCm: 28m,
            packageHeightCm: 4m,
            imagePath: "/images/catalog-seed/products/bo-thot-khang-khuan-duoboard.svg",
            stockQuantity: 38,
            costPrice: 160000m,
            listPrice: 389000m,
            salePrice: 299000m,
            specifications: new[]
            {
                new SeedSpecification("Số lượng", "2 thớt"),
                new SeedSpecification("Đặc điểm", "Phân loại thực phẩm sống và chín"),
                new SeedSpecification("Bề mặt", "Ít bám mùi")
            }),
        Simple(
            categorySlug: "dung-cu-nha-bep",
            brandSlug: "may-home",
            name: "Kệ gia vị xoay SpiceTurn 12 hũ",
            slug: "ke-gia-vi-xoay-spiceturn-12-hu",
            sku: "HOME-SPICE-S12",
            modelNumber: "MH-ST12",
            unit: "Bộ",
            shortDescription: "Kệ xoay kèm mười hai hũ gia vị giúp mặt bếp gọn và dễ tìm.",
            description: "SpiceTurn có trục xoay nhẹ, hũ trong suốt và nắp kín để phân loại gia vị thường dùng. Thiết kế đặt bàn gọn, nhãn trống đi kèm giúp nhận biết nội dung nhanh hơn.",
            manufacturerName: "Công ty TNHH Mây Home",
            warrantyMonths: 6,
            isFeatured: false,
            weight: 2.8m,
            packageLengthCm: 24m,
            packageWidthCm: 24m,
            packageHeightCm: 31m,
            imagePath: "/images/catalog-seed/products/ke-gia-vi-xoay-spiceturn-12-hu.svg",
            stockQuantity: 18,
            costPrice: 370000m,
            listPrice: 799000m,
            salePrice: 629000m,
            specifications: new[]
            {
                new SeedSpecification("Số hũ", "12"),
                new SeedSpecification("Kiểu kệ", "Xoay 360 độ"),
                new SeedSpecification("Phụ kiện", "Nhãn phân loại")
            }),
        Simple(
            categorySlug: "luu-tru-sap-xep",
            brandSlug: "cozynest",
            name: "Giỏ đựng đồ LaundryFlex 45 lít",
            slug: "gio-dung-do-laundryflex-45-lit",
            sku: "HOME-LAUNDRY-L45",
            modelNumber: "CN-L45",
            unit: "Cái",
            shortDescription: "Giỏ đựng quần áo có nắp, tay cầm hai bên và khe thoáng.",
            description: "LaundryFlex có dung tích 45 lít, phù hợp đặt trong phòng ngủ hoặc phòng tắm. Thân giỏ nhẹ, có tay cầm để di chuyển và các khe thoáng hỗ trợ hạn chế bí mùi.",
            manufacturerName: "Công ty TNHH CozyNest",
            warrantyMonths: 3,
            isFeatured: false,
            weight: 1.1m,
            packageLengthCm: 43m,
            packageWidthCm: 34m,
            packageHeightCm: 56m,
            imagePath: "/images/catalog-seed/products/gio-dung-do-laundryflex-45-lit.svg",
            stockQuantity: 44,
            costPrice: 140000m,
            listPrice: 329000m,
            salePrice: 259000m,
            specifications: new[]
            {
                new SeedSpecification("Dung tích", "45 lít"),
                new SeedSpecification("Thiết kế", "Có nắp và tay cầm"),
                new SeedSpecification("Thông thoáng", "Khe thoáng hai bên")
            }),
        Simple(
            categorySlug: "luu-tru-sap-xep",
            brandSlug: "cozynest",
            name: "Tủ giày lật SlimShoe 3 tầng",
            slug: "tu-giay-lat-slimshoe-3-tang",
            sku: "HOME-SHOE-S3",
            modelNumber: "CN-SS3",
            unit: "Cái",
            shortDescription: "Tủ giày dạng lật ba tầng, chiều sâu gọn phù hợp lối vào căn hộ.",
            description: "SlimShoe tối ưu chiều sâu để đặt tại hành lang nhỏ. Ba ngăn lật giúp giày dép được che gọn, tay nắm dễ sử dụng và mặt trên có thể đặt vật dụng trang trí nhẹ.",
            manufacturerName: "Công ty TNHH CozyNest",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 18.5m,
            packageLengthCm: 68m,
            packageWidthCm: 28m,
            packageHeightCm: 19m,
            imagePath: "/images/catalog-seed/products/tu-giay-lat-slimshoe-3-tang.svg",
            stockQuantity: 13,
            costPrice: 1120000m,
            listPrice: 2399000m,
            salePrice: 1899000m,
            specifications: new[]
            {
                new SeedSpecification("Số tầng", "3"),
                new SeedSpecification("Kiểu cửa", "Ngăn lật"),
                new SeedSpecification("Sức chứa tham khảo", "12–18 đôi")
            }),
        Simple(
            categorySlug: "phong-tam",
            brandSlug: "cozynest",
            name: "Bộ khăn tắm CottonCloud 4 món",
            slug: "bo-khan-tam-cottoncloud-4-mon",
            sku: "HOME-TOWEL-T4",
            modelNumber: "CN-CT4",
            unit: "Bộ",
            shortDescription: "Bộ bốn khăn cotton mềm, thấm hút tốt và phù hợp sử dụng hằng ngày.",
            description: "CottonCloud gồm khăn tắm và khăn mặt đồng bộ, bề mặt mềm và đường viền chắc chắn. Khăn dễ giặt, nhanh khô trong điều kiện thông thoáng và phù hợp dùng cho gia đình.",
            manufacturerName: "Công ty TNHH CozyNest",
            warrantyMonths: 3,
            isFeatured: false,
            weight: 1.2m,
            packageLengthCm: 36m,
            packageWidthCm: 27m,
            packageHeightCm: 12m,
            imagePath: "/images/catalog-seed/products/bo-khan-tam-cottoncloud-4-mon.svg",
            stockQuantity: 34,
            costPrice: 260000m,
            listPrice: 599000m,
            salePrice: 459000m,
            specifications: new[]
            {
                new SeedSpecification("Số món", "4"),
                new SeedSpecification("Chất liệu", "Cotton"),
                new SeedSpecification("Màu sắc", "Trung tính")
            }),
        Simple(
            categorySlug: "phong-tam",
            brandSlug: "aquasense",
            name: "Kệ góc phòng tắm AquaCorner 2 tầng",
            slug: "ke-goc-phong-tam-aquacorner-2-tang",
            sku: "HOME-SHELF-B2",
            modelNumber: "AS-AC2",
            unit: "Cái",
            shortDescription: "Kệ góc hai tầng có lỗ thoát nước, phù hợp đựng chai lọ trong phòng tắm.",
            description: "AquaCorner tận dụng góc tường để sắp xếp dầu gội, sữa tắm và phụ kiện. Mặt kệ có lỗ thoát nước, thành chắn vừa phải và bề mặt dễ lau sạch.",
            manufacturerName: "Công ty TNHH AquaSense",
            warrantyMonths: 6,
            isFeatured: false,
            weight: 0.9m,
            packageLengthCm: 31m,
            packageWidthCm: 23m,
            packageHeightCm: 37m,
            imagePath: "/images/catalog-seed/products/ke-goc-phong-tam-aquacorner-2-tang.svg",
            stockQuantity: 39,
            costPrice: 210000m,
            listPrice: 499000m,
            salePrice: 379000m,
            specifications: new[]
            {
                new SeedSpecification("Số tầng", "2"),
                new SeedSpecification("Lắp đặt", "Góc tường"),
                new SeedSpecification("Thoát nước", "Mặt kệ có lỗ")
            }),
        Simple(
            categorySlug: "phong-tam",
            brandSlug: "aquasense",
            name: "Cân sức khỏe SmartStep",
            slug: "can-suc-khoe-smartstep",
            sku: "HOME-SCALE-S1",
            modelNumber: "AS-SS1",
            unit: "Cái",
            shortDescription: "Cân điện tử mặt kính, màn hình rõ và tự động bật khi bước lên.",
            description: "SmartStep sử dụng cảm biến điện tử và màn hình hiển thị dễ đọc. Mặt cân kính cường lực, chân đế chống trượt và tính năng tự tắt giúp tiết kiệm pin.",
            manufacturerName: "Công ty TNHH AquaSense",
            warrantyMonths: 12,
            isFeatured: true,
            weight: 1.75m,
            packageLengthCm: 31m,
            packageWidthCm: 31m,
            packageHeightCm: 4m,
            imagePath: "/images/catalog-seed/products/can-suc-khoe-smartstep.svg",
            stockQuantity: 28,
            costPrice: 290000m,
            listPrice: 649000m,
            salePrice: 499000m,
            specifications: new[]
            {
                new SeedSpecification("Tải trọng tối đa", "180 kg"),
                new SeedSpecification("Mặt cân", "Kính cường lực"),
                new SeedSpecification("Tính năng", "Tự bật và tự tắt")
            }),
        WithVariants(
            categorySlug: "phong-ngu",
            brandSlug: "cozynest",
            name: "Bộ chăn ga CottonSoft",
            slug: "bo-chan-ga-cottonsoft",
            sku: "HOME-BED-C2",
            modelNumber: "CN-CS26",
            unit: "Bộ",
            shortDescription: "Bộ chăn ga cotton màu trung tính, bề mặt mềm và dễ phối nội thất.",
            description: "CottonSoft gồm ga, vỏ chăn và vỏ gối đồng bộ cho phòng ngủ hiện đại. Chất vải thoáng, đường may chắc và màu sắc dịu giúp không gian nghỉ ngơi gọn gàng hơn.",
            manufacturerName: "Công ty TNHH CozyNest",
            warrantyMonths: 3,
            isFeatured: true,
            weight: 3.6m,
            packageLengthCm: 48m,
            packageWidthCm: 38m,
            packageHeightCm: 18m,
            imagePath: "/images/catalog-seed/products/bo-chan-ga-cottonsoft.svg",
            optionName: "Kích thước giường",
            variants: new[]
            {
                new SeedVariant(
                    Value: "1,6 m",
                    Sku: "HOME-BED-C2-16",
                    StockQuantity: 17,
                    Weight: 3.2m,
                    CostPrice: 680000m,
                    ListPrice: 1499000m,
                    SalePrice: 1199000m),
                new SeedVariant(
                    Value: "1,8 m",
                    Sku: "HOME-BED-C2-18",
                    StockQuantity: 15,
                    Weight: 3.6m,
                    CostPrice: 760000m,
                    ListPrice: 1699000m,
                    SalePrice: 1349000m)
            },
            specifications: new[]
            {
                new SeedSpecification("Chất liệu", "Cotton"),
                new SeedSpecification("Bộ sản phẩm", "Ga, vỏ chăn và vỏ gối"),
                new SeedSpecification("Phong cách", "Màu trung tính")
            }),
        Simple(
            categorySlug: "phong-ngu",
            brandSlug: "cozynest",
            name: "Bộ 2 gối ngủ CloudRest",
            slug: "bo-2-goi-ngu-cloudrest",
            sku: "HOME-PILLOW-P2",
            modelNumber: "CN-CR2",
            unit: "Bộ",
            shortDescription: "Hai gối ngủ độ cao vừa, vỏ tháo rời và ruột gối đàn hồi.",
            description: "CloudRest mang lại độ nâng đỡ vừa phải cho tư thế nằm phổ biến. Vỏ gối có khóa kéo để tháo giặt, ruột gối đàn hồi và đường may chắc chắn phù hợp sử dụng hằng ngày.",
            manufacturerName: "Công ty TNHH CozyNest",
            warrantyMonths: 3,
            isFeatured: false,
            weight: 2.2m,
            packageLengthCm: 54m,
            packageWidthCm: 38m,
            packageHeightCm: 24m,
            imagePath: "/images/catalog-seed/products/bo-2-goi-ngu-cloudrest.svg",
            stockQuantity: 36,
            costPrice: 270000m,
            listPrice: 649000m,
            salePrice: 499000m,
            specifications: new[]
            {
                new SeedSpecification("Số lượng", "2 gối"),
                new SeedSpecification("Vỏ gối", "Tháo rời"),
                new SeedSpecification("Độ cao", "Trung bình")
            })
        };

    private static ProductSeed Simple(
        string categorySlug,
        string brandSlug,
        string name,
        string slug,
        string sku,
        string modelNumber,
        string unit,
        string shortDescription,
        string description,
        string manufacturerName,
        int warrantyMonths,
        bool isFeatured,
        decimal weight,
        decimal packageLengthCm,
        decimal packageWidthCm,
        decimal packageHeightCm,
        string imagePath,
        int stockQuantity,
        decimal costPrice,
        decimal listPrice,
        decimal salePrice,
        IReadOnlyList<SeedSpecification> specifications) =>
        new(
            categorySlug,
            brandSlug,
            name,
            slug,
            sku,
            modelNumber,
            unit,
            shortDescription,
            description,
            manufacturerName,
            warrantyMonths,
            isFeatured,
            weight,
            packageLengthCm,
            packageWidthCm,
            packageHeightCm,
            imagePath,
            stockQuantity,
            costPrice,
            listPrice,
            salePrice,
            null,
            Array.Empty<SeedVariant>(),
            specifications);

    private static ProductSeed WithVariants(
        string categorySlug,
        string brandSlug,
        string name,
        string slug,
        string sku,
        string modelNumber,
        string unit,
        string shortDescription,
        string description,
        string manufacturerName,
        int warrantyMonths,
        bool isFeatured,
        decimal weight,
        decimal packageLengthCm,
        decimal packageWidthCm,
        decimal packageHeightCm,
        string imagePath,
        string optionName,
        IReadOnlyList<SeedVariant> variants,
        IReadOnlyList<SeedSpecification> specifications) =>
        new(
            categorySlug,
            brandSlug,
            name,
            slug,
            sku,
            modelNumber,
            unit,
            shortDescription,
            description,
            manufacturerName,
            warrantyMonths,
            isFeatured,
            weight,
            packageLengthCm,
            packageWidthCm,
            packageHeightCm,
            imagePath,
            0,
            0,
            0,
            0,
            optionName,
            variants,
            specifications);

    private sealed record SeedCategory(
        string Slug,
        string Name,
        string? ParentSlug,
        int DisplayOrder,
        string Description,
        string ImagePath);

    private sealed record SeedBrand(
        string Slug,
        string Name,
        string Country);

    private sealed record SeedSpecification(
        string Name,
        string Value);

    private sealed record SeedVariant(
        string Value,
        string Sku,
        int StockQuantity,
        decimal Weight,
        decimal CostPrice,
        decimal ListPrice,
        decimal SalePrice);

    private sealed record ProductSeed(
        string CategorySlug,
        string BrandSlug,
        string Name,
        string Slug,
        string Sku,
        string ModelNumber,
        string Unit,
        string ShortDescription,
        string Description,
        string ManufacturerName,
        int WarrantyMonths,
        bool IsFeatured,
        decimal Weight,
        decimal PackageLengthCm,
        decimal PackageWidthCm,
        decimal PackageHeightCm,
        string ImagePath,
        int StockQuantity,
        decimal CostPrice,
        decimal ListPrice,
        decimal SalePrice,
        string? OptionName,
        IReadOnlyList<SeedVariant> Variants,
        IReadOnlyList<SeedSpecification> Specifications);
}
