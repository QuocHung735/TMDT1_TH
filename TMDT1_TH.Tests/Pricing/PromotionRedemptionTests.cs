using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Tests.Pricing;

public sealed class PromotionRedemptionTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly PromotionService _service;

    public PromotionRedemptionTests()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"promotion-redemption-{Guid.NewGuid():N}")
                .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();

        _service = new PromotionService(_db);
    }

    [Fact]
    public void CreateRedemption_CopiesPromotionAndOrderSnapshots()
    {
        var order = CreateOrder();

        var resolution =
            PromotionResolution.Success(
                25,
                "KM-260725-A7K9",
                "Khuyến mãi đồ chơi",
                120_000m,
                600_000m,
                "Danh mục cụ thể");

        var redeemedAt =
            new DateTime(
                2026,
                7,
                25,
                3,
                0,
                0,
                DateTimeKind.Utc);

        var redemption =
            _service.CreateRedemption(
                order,
                resolution,
                redeemedAt,
                "customer@example.com");

        Assert.Equal(25, redemption.PromotionId);
        Assert.Same(order, redemption.Order);
        Assert.Equal(
            order.CustomerUserId,
            redemption.CustomerUserId);

        Assert.Equal(
            "KM-260725-A7K9",
            redemption.PromotionCode);

        Assert.Equal(
            "Khuyến mãi đồ chơi",
            redemption.PromotionName);

        Assert.Equal(
            120_000m,
            redemption.DiscountAmount);

        Assert.Equal(redeemedAt, redemption.RedeemedAt);
        Assert.False(redemption.IsReleased);
        Assert.Null(redemption.ReleasedAt);
    }

    [Fact]
    public async Task TryReleaseForOrderAsync_ReleasesAndReturnsUsage()
    {
        var promotion =
            await AddPromotionAsync(usedCount: 1);

        var order =
            await AddOrderAsync(promotion);

        var releasedAt =
            new DateTime(
                2026,
                7,
                26,
                2,
                0,
                0,
                DateTimeKind.Utc);

        var released =
            await _service.TryReleaseForOrderAsync(
                order.Id,
                releasedAt,
                "Khách yêu cầu hủy đơn.",
                "admin@example.com");

        await _db.SaveChangesAsync();

        var savedPromotion =
            await _db.Promotions
                .AsNoTracking()
                .SingleAsync(x =>
                    x.Id == promotion.Id);

        var redemption =
            await _db.PromotionRedemptions
                .AsNoTracking()
                .SingleAsync(x =>
                    x.OrderId == order.Id);

        Assert.True(released);
        Assert.Equal(0, savedPromotion.UsedCount);
        Assert.True(redemption.IsReleased);
        Assert.Equal(releasedAt, redemption.ReleasedAt);
        Assert.Equal(
            "Khách yêu cầu hủy đơn.",
            redemption.ReleaseReason);
    }

    [Fact]
    public async Task TryReleaseForOrderAsync_SecondCallDoesNotDecreaseAgain()
    {
        var promotion =
            await AddPromotionAsync(usedCount: 1);

        var order =
            await AddOrderAsync(promotion);

        var first =
            await _service.TryReleaseForOrderAsync(
                order.Id,
                DateTime.UtcNow,
                "Hủy lần đầu.",
                "Admin");

        await _db.SaveChangesAsync();

        var second =
            await _service.TryReleaseForOrderAsync(
                order.Id,
                DateTime.UtcNow.AddMinutes(1),
                "Hủy lần hai.",
                "Admin");

        await _db.SaveChangesAsync();

        var savedPromotion =
            await _db.Promotions
                .AsNoTracking()
                .SingleAsync(x =>
                    x.Id == promotion.Id);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(0, savedPromotion.UsedCount);
    }

    [Fact]
    public async Task TryReleaseForOrderAsync_OrderWithoutCode_ReturnsFalse()
    {
        var order = CreateOrder();
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var released =
            await _service.TryReleaseForOrderAsync(
                order.Id,
                DateTime.UtcNow,
                "Không có khuyến mãi.",
                "Admin");

        Assert.False(released);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<Promotion> AddPromotionAsync(
        int usedCount)
    {
        var promotion = new Promotion
        {
            Name = "Khuyến mãi kiểm thử",
            Code = $"KM-TEST-{Guid.NewGuid():N}"[..20],
            DiscountType =
                PromotionDiscountType.Percentage,
            ScopeType =
                PromotionScopeType.AllProducts,
            DiscountValue = 10,
            StartsAt = DateTime.Now.AddDays(-1),
            EndsAt = DateTime.Now.AddDays(1),
            IsActive = true,
            UsedCount = usedCount
        };

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();

        return promotion;
    }

    private async Task<Order> AddOrderAsync(
        Promotion promotion)
    {
        var order = CreateOrder();

        order.PromotionCode = promotion.Code;
        order.PromotionName = promotion.Name;
        order.DiscountAmount = 100_000m;
        order.TotalAmount =
            order.Subtotal +
            order.ShippingFee -
            order.DiscountAmount;

        order.PromotionRedemption =
            new PromotionRedemption
            {
                PromotionId = promotion.Id,
                CustomerUserId = order.CustomerUserId,
                PromotionCode = promotion.Code,
                PromotionName = promotion.Name,
                DiscountAmount = order.DiscountAmount,
                RedeemedAt = DateTime.UtcNow,
                CreatedBy = "Test"
            };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }

    private static Order CreateOrder()
    {
        return new Order
        {
            OrderNumber =
                $"TEST-{Guid.NewGuid():N}"[..20],
            PublicToken = Guid.NewGuid(),
            CustomerUserId = 10,
            MarketId = 1,
            CurrencyCode = "VND",
            Status = OrderStatus.Pending,
            CustomerName = "Khách kiểm thử",
            CustomerPhone = "0900000000",
            Province = "TP. Hồ Chí Minh",
            District = string.Empty,
            Ward = "Phường Bến Thành",
            AddressLine = "1 Đường kiểm thử",
            Subtotal = 1_000_000m,
            ShippingFee = 30_000m,
            DiscountAmount = 0,
            TotalAmount = 1_030_000m,
            CreatedBy = "Test"
        };
    }
}
