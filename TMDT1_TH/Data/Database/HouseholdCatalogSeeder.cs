using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Data.Database;

public static class HouseholdCatalogSeeder
{
    public static async Task TrySeedAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await db.Database.CanConnectAsync())
                return;

            if (await db.Products.AnyAsync(x => x.Sku.StartsWith("HOME-")))
                return;

            var kitchen = await GetOrCreateCategoryAsync(db, "Nhà bếp", "nha-bep", null, 10);
            var cookware = await GetOrCreateCategoryAsync(db, "Nồi & chảo", "noi-chao", kitchen.Id, 11);
            var foodStorage = await GetOrCreateCategoryAsync(db, "Lưu trữ thực phẩm", "luu-tru-thuc-pham", kitchen.Id, 12);
            var electric = await GetOrCreateCategoryAsync(db, "Điện gia dụng", "dien-gia-dung", null, 20);
            var cleaning = await GetOrCreateCategoryAsync(db, "Vệ sinh nhà cửa", "ve-sinh-nha-cua", null, 30);
            var organization = await GetOrCreateCategoryAsync(db, "Lưu trữ & sắp xếp", "luu-tru-sap-xep", null, 40);

            var mayHome = await GetOrCreateBrandAsync(db, "Mây Home", "may-home", "Việt Nam");
            var bepXinh = await GetOrCreateBrandAsync(db, "Bếp Xinh", "bep-xinh", "Việt Nam");
            var pureNest = await GetOrCreateBrandAsync(db, "PureNest", "purenest", "Việt Nam");
            var cleanJoy = await GetOrCreateBrandAsync(db, "CleanJoy", "cleanjoy", "Việt Nam");

            var online = await db.Markets.FirstAsync(x => x.Code == "ONLINE");
            var hcm = await db.Markets.FirstAsync(x => x.Code == "VN-HCM");
            var now = DateTime.UtcNow;

            var airFryer = new Product
            {
                Category = electric,
                Brand = mayHome,
                Name = "Nồi chiên không dầu AirCare",
                Slug = "noi-chien-khong-dau-aircare",
                Sku = "HOME-AF",
                ShortDescription = "Nồi chiên không dầu điều khiển cảm ứng, lòng nồi chống dính dễ vệ sinh.",
                Description = "Phù hợp cho gia đình, có nhiều chế độ nấu và hẹn giờ tiện lợi.",
                Status = ProductStatus.Active,
                IsFeatured = true,
                HasVariants = true,
                StockQuantity = 0,
                Weight = 5.2m
            };
            var capacityOption = new ProductOption { Product = airFryer, Name = "Dung tích", DisplayOrder = 1 };
            var cap4 = new ProductOptionValue { ProductOption = capacityOption, Value = "4 lít", DisplayOrder = 1 };
            var cap6 = new ProductOptionValue { ProductOption = capacityOption, Value = "6 lít", DisplayOrder = 2 };
            capacityOption.Values.Add(cap4);
            capacityOption.Values.Add(cap6);
            airFryer.Options.Add(capacityOption);
            var air4 = CreateVariant(airFryer, "HOME-AF-4L", "4 lít", "CAPACITY=4L", 18, true, 4.4m, cap4);
            var air6 = CreateVariant(airFryer, "HOME-AF-6L", "6 lít", "CAPACITY=6L", 36, false, 5.2m, cap6);
            air4.PriceSchedules.Add(CreateVariantPrice(air4, online, 920000, 1990000, 1590000, now.AddDays(-30), null, "Giá bán tiêu chuẩn"));
            air6.PriceSchedules.Add(CreateVariantPrice(air6, online, 1180000, 2490000, 1890000, now.AddDays(-30), null, "Giá bán tiêu chuẩn"));
            air6.PriceSchedules.Add(CreateVariantPrice(air6, hcm, 1180000, 2490000, 1790000, now.AddDays(7), now.AddDays(21), "Ưu đãi thị trường HCM"));

            var pan = new Product
            {
                Category = cookware,
                Brand = bepXinh,
                Name = "Chảo chống dính Ceramic Glow",
                Slug = "chao-chong-dinh-ceramic-glow",
                Sku = "HOME-PAN-CG",
                ShortDescription = "Chảo phủ ceramic, tay cầm cách nhiệt và dùng được trên nhiều loại bếp.",
                Status = ProductStatus.Active,
                HasVariants = true,
                StockQuantity = 0,
                Weight = 1.1m
            };
            var sizeOption = new ProductOption { Product = pan, Name = "Kích thước", DisplayOrder = 1 };
            var size24 = new ProductOptionValue { ProductOption = sizeOption, Value = "24 cm", DisplayOrder = 1 };
            var size28 = new ProductOptionValue { ProductOption = sizeOption, Value = "28 cm", DisplayOrder = 2 };
            sizeOption.Values.Add(size24);
            sizeOption.Values.Add(size28);
            pan.Options.Add(sizeOption);
            var pan24 = CreateVariant(pan, "HOME-PAN-CG-24", "24 cm", "SIZE=24CM", 42, true, 0.9m, size24);
            var pan28 = CreateVariant(pan, "HOME-PAN-CG-28", "28 cm", "SIZE=28CM", 30, false, 1.1m, size28);
            pan24.PriceSchedules.Add(CreateVariantPrice(pan24, online, 190000, 449000, 359000, now.AddDays(-20), null, "Giá bán tiêu chuẩn"));
            pan28.PriceSchedules.Add(CreateVariantPrice(pan28, online, 230000, 549000, 429000, now.AddDays(-20), null, "Giá bán tiêu chuẩn"));

            var freshBox = new Product
            {
                Category = foodStorage,
                Brand = pureNest,
                Name = "Bộ hộp bảo quản FreshBox 5 món",
                Slug = "bo-hop-bao-quan-freshbox-5-mon",
                Sku = "HOME-BOX-FB5",
                ShortDescription = "Bộ hộp kín khí nhiều kích thước, phù hợp bảo quản thực phẩm khô và thức ăn.",
                Status = ProductStatus.Active,
                HasVariants = false,
                StockQuantity = 9,
                Weight = 1.3m
            };
            freshBox.PriceSchedules.Add(CreateProductPrice(freshBox, online, 145000, 349000, 289000, now.AddDays(-25), null, "Giá bán tiêu chuẩn"));

            var mop = new Product
            {
                Category = cleaning,
                Brand = cleanJoy,
                Name = "Cây lau nhà xoay 360 CleanSpin",
                Slug = "cay-lau-nha-xoay-360-cleanspin",
                Sku = "HOME-MOP-CS",
                ShortDescription = "Bộ lau nhà xoay 360 độ, lồng vắt inox và bông lau microfiber.",
                Status = ProductStatus.OutOfStock,
                HasVariants = false,
                StockQuantity = 0,
                Weight = 2.4m
            };
            mop.PriceSchedules.Add(CreateProductPrice(mop, online, 185000, 449000, 359000, now.AddDays(-25), null, "Giá bán tiêu chuẩn"));

            var rack = new Product
            {
                Category = organization,
                Brand = mayHome,
                Name = "Kệ đa năng FlexiRack 4 tầng",
                Slug = "ke-da-nang-flexirack-4-tang",
                Sku = "HOME-RACK-F4",
                ShortDescription = "Kệ lắp ghép bốn tầng dùng cho nhà bếp, phòng tắm hoặc ban công.",
                Status = ProductStatus.Draft,
                HasVariants = false,
                StockQuantity = 24,
                Weight = 4.8m
            };
            rack.PriceSchedules.Add(CreateProductPrice(rack, online, 310000, 699000, 549000, now.AddDays(-10), null, "Giá dự kiến"));

            db.Products.AddRange(airFryer, pan, freshBox, mop, rack);
            await db.SaveChangesAsync();
            logger.LogInformation("Đã tạo dữ liệu mẫu website đồ gia dụng Mây Home.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bỏ qua seed đồ gia dụng. Hãy bảo đảm đã chạy Update-Database.");
        }
    }

    private static async Task<Category> GetOrCreateCategoryAsync(ApplicationDbContext db, string name, string slug, int? parentId, int order)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(x => x.Slug == slug);
        if (entity is not null) return entity;
        entity = new Category { Name = name, Slug = slug, ParentId = parentId, DisplayOrder = order, IsActive = true };
        db.Categories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static async Task<Brand> GetOrCreateBrandAsync(ApplicationDbContext db, string name, string slug, string country)
    {
        var entity = await db.Brands.FirstOrDefaultAsync(x => x.Slug == slug);
        if (entity is not null) return entity;
        entity = new Brand { Name = name, Slug = slug, Country = country, IsActive = true };
        db.Brands.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static ProductVariant CreateVariant(Product product, string sku, string name, string key, int stock, bool isDefault, decimal weight, ProductOptionValue value)
    {
        var variant = new ProductVariant
        {
            Product = product,
            Sku = sku,
            Name = name,
            CombinationKey = key,
            StockQuantity = stock,
            IsDefault = isDefault,
            IsActive = true,
            Weight = weight
        };
        variant.VariantValues.Add(new ProductVariantValue { ProductVariant = variant, ProductOptionValue = value });
        product.Variants.Add(variant);
        return variant;
    }

    private static PriceSchedule CreateVariantPrice(ProductVariant variant, Market market, decimal cost, decimal list, decimal sale, DateTime from, DateTime? to, string note) => new()
    {
        ProductVariant = variant,
        Market = market,
        CostPrice = cost,
        ListPrice = list,
        SalePrice = sale,
        ValidFrom = from,
        ValidTo = to,
        IsActive = true,
        Note = note
    };

    private static PriceSchedule CreateProductPrice(Product product, Market market, decimal cost, decimal list, decimal sale, DateTime from, DateTime? to, string note) => new()
    {
        Product = product,
        Market = market,
        CostPrice = cost,
        ListPrice = list,
        SalePrice = sale,
        ValidFrom = from,
        ValidTo = to,
        IsActive = true,
        Note = note
    };
}
