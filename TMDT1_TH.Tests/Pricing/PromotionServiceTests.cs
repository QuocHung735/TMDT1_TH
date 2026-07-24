using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Tests.Pricing;

public sealed class PromotionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly PromotionService _service;

    private readonly Market _market;
    private readonly Market _otherMarket;

    private readonly Category _toyCategory;
    private readonly Category _homeCategory;

    private readonly Brand _legoBrand;
    private readonly Brand _otherBrand;

    private readonly Product _legoProduct;
    private readonly Product _homeProduct;

    private readonly DateTime _now =
        new(2026, 7, 25, 10, 0, 0);

    public PromotionServiceTests()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"promotion-tests-{Guid.NewGuid():N}")
                .EnableSensitiveDataLogging()
                .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        _market = new Market
        {
            Code = "ONLINE",
            Name = "Kênh trực tuyến",
            CurrencyCode = "VND",
            IsDefault = true,
            IsActive = true
        };

        _otherMarket = new Market
        {
            Code = "HCM",
            Name = "TP.HCM",
            CurrencyCode = "VND",
            IsDefault = false,
            IsActive = true
        };

        _toyCategory = new Category
        {
            Name = "Đồ chơi",
            Slug = "do-choi",
            IsActive = true
        };

        _homeCategory = new Category
        {
            Name = "Gia dụng",
            Slug = "gia-dung",
            IsActive = true
        };

        _legoBrand = new Brand
        {
            Name = "Lego",
            Slug = "lego",
            IsActive = true
        };

        _otherBrand = new Brand
        {
            Name = "Mây Home",
            Slug = "may-home",
            IsActive = true
        };

        _legoProduct = new Product
        {
            Name = "Bộ xếp hình Lego",
            Slug = "bo-xep-hinh-lego",
            Sku = "TOY-LEGO-001",
            Unit = "Bộ",
            Category = _toyCategory,
            Brand = _legoBrand,
            Status = ProductStatus.Active,
            StockQuantity = 20
        };

        _homeProduct = new Product
        {
            Name = "Hộp bảo quản",
            Slug = "hop-bao-quan",
            Sku = "HOME-BOX-001",
            Unit = "Bộ",
            Category = _homeCategory,
            Brand = _otherBrand,
            Status = ProductStatus.Active,
            StockQuantity = 20
        };

        _db.AddRange(
            _market,
            _otherMarket,
            _toyCategory,
            _homeCategory,
            _legoBrand,
            _otherBrand,
            _legoProduct,
            _homeProduct);

        _db.SaveChanges();

        _service = new PromotionService(_db);
    }

    [Fact]
    public async Task ResolveAsync_WithoutCode_ReturnsNoDiscount()
    {
        var result = await _service.ResolveAsync(
            null,
            CartLines(),
            _market.Id,
            _now);

        Assert.True(result.IsValid);
        Assert.Null(result.PromotionId);
        Assert.Null(result.Code);
        Assert.Equal(0m, result.DiscountAmount);
    }

    [Fact]
    public async Task ResolveAsync_UnknownCode_ReturnsInvalid()
    {
        var result = await _service.ResolveAsync(
            "KHONG-TON-TAI",
            CartLines(),
            _market.Id,
            _now);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi không tồn tại.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_InactivePromotion_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            isActive: false);

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi đang tạm ngừng.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_FuturePromotion_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            startsAt: _now.AddMinutes(1),
            endsAt: _now.AddDays(1));

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi chưa đến thời gian áp dụng.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_ExpiredPromotion_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            startsAt: _now.AddDays(-2),
            endsAt: _now);

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi đã hết hạn.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_WrongMarket_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            marketId: _otherMarket.Id);

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi không áp dụng cho thị trường hiện tại.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_ExhaustedUsageLimit_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            usageLimit: 5,
            usedCount: 5);

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Mã khuyến mãi đã hết lượt sử dụng.",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_SubtotalBelowMinimum_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            minimumOrderAmount: 1_500_000m);

        var result = await ResolveAsync(promotion);

        Assert.False(result.IsValid);
        Assert.Contains(
            "Tổng đơn hàng cần tối thiểu",
            result.Error);
    }

    [Fact]
    public async Task ResolveAsync_AllProductsPercentage_DiscountsWholeCart()
    {
        var promotion = await AddPromotionAsync(
            discountValue: 10m);

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(1_000_000m, result.EligibleSubtotal);
        Assert.Equal(100_000m, result.DiscountAmount);
        Assert.Equal("Toàn bộ sản phẩm", result.ScopeName);
    }

    [Fact]
    public async Task ResolveAsync_PercentageWithMaximum_UsesMaximum()
    {
        var promotion = await AddPromotionAsync(
            discountValue: 50m,
            maximumDiscountAmount: 200_000m);

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(200_000m, result.DiscountAmount);
    }

    [Fact]
    public async Task ResolveAsync_FixedAmount_NeverExceedsEligibleSubtotal()
    {
        var promotion = await AddPromotionAsync(
            discountType:
                PromotionDiscountType.FixedAmount,
            discountValue: 700_000m,
            scopeType:
                PromotionScopeType.Products,
            productIds: new[] { _legoProduct.Id });

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(400_000m, result.EligibleSubtotal);
        Assert.Equal(400_000m, result.DiscountAmount);
    }

    [Fact]
    public async Task ResolveAsync_ProductScope_DiscountsSelectedProductOnly()
    {
        var promotion = await AddPromotionAsync(
            discountValue: 20m,
            scopeType:
                PromotionScopeType.Products,
            productIds: new[] { _legoProduct.Id });

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(400_000m, result.EligibleSubtotal);
        Assert.Equal(80_000m, result.DiscountAmount);
        Assert.Equal("Sản phẩm cụ thể", result.ScopeName);
    }

    [Fact]
    public async Task ResolveAsync_CategoryScope_DiscountsMatchingCategoryOnly()
    {
        var promotion = await AddPromotionAsync(
            discountValue: 25m,
            scopeType:
                PromotionScopeType.Categories,
            categoryIds: new[] { _toyCategory.Id });

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(400_000m, result.EligibleSubtotal);
        Assert.Equal(100_000m, result.DiscountAmount);
        Assert.Equal("Danh mục cụ thể", result.ScopeName);
    }

    [Fact]
    public async Task ResolveAsync_BrandScope_DiscountsMatchingBrandOnly()
    {
        var promotion = await AddPromotionAsync(
            discountValue: 15m,
            scopeType:
                PromotionScopeType.Brands,
            brandIds: new[] { _legoBrand.Id });

        var result = await ResolveAsync(promotion);

        Assert.True(result.IsValid);
        Assert.Equal(400_000m, result.EligibleSubtotal);
        Assert.Equal(60_000m, result.DiscountAmount);
        Assert.Equal("Thương hiệu cụ thể", result.ScopeName);
    }

    [Fact]
    public async Task ResolveAsync_NoMatchingScopedProduct_ReturnsInvalid()
    {
        var promotion = await AddPromotionAsync(
            scopeType:
                PromotionScopeType.Products,
            productIds: new[] { _legoProduct.Id });

        var lines = new[]
        {
            new PromotionCartLine(
                _homeProduct.Id,
                600_000m)
        };

        var result = await _service.ResolveAsync(
            promotion.Code,
            lines,
            _market.Id,
            _now);

        Assert.False(result.IsValid);
        Assert.Equal(
            "Giỏ hàng không có sản phẩm được áp dụng mã này.",
            result.Error);
    }

    [Fact]
    public async Task TryClaimAsync_ValidPromotion_IncrementsUsedCount()
    {
        var promotion = await AddPromotionAsync(
            usageLimit: 3,
            usedCount: 1);

        var claimed = await _service.TryClaimAsync(
            promotion.Id,
            _now);

        await _db.SaveChangesAsync();

        var saved = await _db.Promotions
            .AsNoTracking()
            .SingleAsync(x => x.Id == promotion.Id);

        Assert.True(claimed);
        Assert.Equal(2, saved.UsedCount);
    }

    [Fact]
    public async Task TryClaimAsync_ExhaustedPromotion_ReturnsFalse()
    {
        var promotion = await AddPromotionAsync(
            usageLimit: 1,
            usedCount: 1);

        var claimed = await _service.TryClaimAsync(
            promotion.Id,
            _now);

        Assert.False(claimed);
    }

    [Theory]
    [InlineData(" km-260725-abcd ", "KM-260725-ABCD")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void NormalizeCode_NormalizesExpectedValue(
        string input,
        string? expected)
    {
        var result =
            PromotionService.NormalizeCode(input);

        Assert.Equal(expected, result);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<PromotionResolution> ResolveAsync(
        Promotion promotion)
    {
        return await _service.ResolveAsync(
            promotion.Code,
            CartLines(),
            _market.Id,
            _now);
    }

    private IReadOnlyList<PromotionCartLine> CartLines()
    {
        return new[]
        {
            new PromotionCartLine(
                _legoProduct.Id,
                400_000m),
            new PromotionCartLine(
                _homeProduct.Id,
                600_000m)
        };
    }

    private async Task<Promotion> AddPromotionAsync(
        PromotionDiscountType discountType =
            PromotionDiscountType.Percentage,
        PromotionScopeType scopeType =
            PromotionScopeType.AllProducts,
        decimal discountValue = 10m,
        decimal? maximumDiscountAmount = null,
        decimal minimumOrderAmount = 0m,
        int? usageLimit = null,
        int usedCount = 0,
        bool isActive = true,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        int? marketId = null,
        IEnumerable<int>? productIds = null,
        IEnumerable<int>? categoryIds = null,
        IEnumerable<int>? brandIds = null)
    {
        var promotion = new Promotion
        {
            Name = "Khuyến mãi kiểm thử",
            Code =
                $"KM-TEST-{Guid.NewGuid():N}"[..20]
                    .ToUpperInvariant(),
            DiscountType = discountType,
            ScopeType = scopeType,
            DiscountValue = discountValue,
            MaximumDiscountAmount =
                maximumDiscountAmount,
            MinimumOrderAmount =
                minimumOrderAmount,
            UsageLimit = usageLimit,
            UsedCount = usedCount,
            StartsAt =
                startsAt ?? _now.AddDays(-1),
            EndsAt =
                endsAt ?? _now.AddDays(1),
            IsActive = isActive
        };

        promotion.Markets.Add(
            new PromotionMarket
            {
                MarketId =
                    marketId ?? _market.Id
            });

        foreach (var productId in
                 productIds ?? Array.Empty<int>())
        {
            promotion.Products.Add(
                new PromotionProduct
                {
                    ProductId = productId
                });
        }

        foreach (var categoryId in
                 categoryIds ?? Array.Empty<int>())
        {
            promotion.Categories.Add(
                new PromotionCategory
                {
                    CategoryId = categoryId
                });
        }

        foreach (var brandId in
                 brandIds ?? Array.Empty<int>())
        {
            promotion.Brands.Add(
                new PromotionBrand
                {
                    BrandId = brandId
                });
        }

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();

        return promotion;
    }
}
