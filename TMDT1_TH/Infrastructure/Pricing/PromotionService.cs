using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;

using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Infrastructure.Pricing;

public sealed class PromotionService(
    ApplicationDbContext db)
{
    private readonly ApplicationDbContext _db = db;

    public async Task<PromotionResolution> ResolveAsync(
        string? code,
        IReadOnlyCollection<PromotionCartLine> lines,
        int marketId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);

        if (normalizedCode is null)
            return PromotionResolution.None;

        if (lines.Count == 0)
        {
            return PromotionResolution.Invalid(
                "Đơn hàng chưa có sản phẩm để áp dụng khuyến mãi.");
        }

        var subtotal = lines.Sum(x => x.LineTotal);

        if (subtotal <= 0)
        {
            return PromotionResolution.Invalid(
                "Đơn hàng chưa có giá trị để áp dụng khuyến mãi.");
        }

        var promotion = await _db.Promotions
            .AsNoTracking()
            .Include(x => x.Markets)
            .Include(x => x.Products)
            .Include(x => x.Categories)
            .Include(x => x.Brands)
            .FirstOrDefaultAsync(
                x => x.Code == normalizedCode,
                cancellationToken);

        if (promotion is null)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi không tồn tại.");
        }

        if (!promotion.IsActive)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi đang tạm ngừng.");
        }

        if (promotion.StartsAt > now)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi chưa đến thời gian áp dụng.");
        }

        if (promotion.EndsAt <= now)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi đã hết hạn.");
        }

        if (!promotion.Markets.Any(
                x => x.MarketId == marketId))
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi không áp dụng cho thị trường hiện tại.");
        }

        if (promotion.UsageLimit.HasValue &&
            promotion.UsedCount >= promotion.UsageLimit.Value)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi đã hết lượt sử dụng.");
        }

        if (subtotal < promotion.MinimumOrderAmount)
        {
            return PromotionResolution.Invalid(
                $"Tổng đơn hàng cần tối thiểu " +
                $"{promotion.MinimumOrderAmount:N0}đ " +
                "để sử dụng mã này.");
        }

        var eligibleProductIds =
            await ResolveEligibleProductIdsAsync(
                promotion.ScopeType,
                promotion.Products.Select(x => x.ProductId),
                promotion.Categories.Select(x => x.CategoryId),
                promotion.Brands.Select(x => x.BrandId),
                lines.Select(x => x.ProductId),
                cancellationToken);

        var eligibleSubtotal = lines
            .Where(x =>
                eligibleProductIds.Contains(x.ProductId))
            .Sum(x => x.LineTotal);

        if (eligibleSubtotal <= 0)
        {
            return PromotionResolution.Invalid(
                GetScopeError(promotion.ScopeType));
        }

        decimal discount;

        if (promotion.DiscountType ==
            PromotionDiscountType.Percentage)
        {
            discount =
                eligibleSubtotal *
                promotion.DiscountValue /
                100m;

            if (promotion.MaximumDiscountAmount.HasValue)
            {
                discount = Math.Min(
                    discount,
                    promotion.MaximumDiscountAmount.Value);
            }
        }
        else
        {
            discount = promotion.DiscountValue;
        }

        discount = Math.Min(
            discount,
            eligibleSubtotal);

        discount = decimal.Round(
            discount,
            0,
            MidpointRounding.AwayFromZero);

        if (discount <= 0)
        {
            return PromotionResolution.Invalid(
                "Mã khuyến mãi không tạo ra giá trị giảm hợp lệ.");
        }

        return PromotionResolution.Success(
            promotion.Id,
            promotion.Code,
            promotion.Name,
            discount,
            eligibleSubtotal,
            GetScopeName(promotion.ScopeType));
    }

    public async Task<bool> TryClaimAsync(
        int promotionId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(
                x => x.Id == promotionId,
                cancellationToken);

        if (promotion is null ||
            !promotion.IsActive ||
            promotion.StartsAt > now ||
            promotion.EndsAt <= now)
        {
            return false;
        }

        if (promotion.UsageLimit.HasValue &&
            promotion.UsedCount >= promotion.UsageLimit.Value)
        {
            return false;
        }

        promotion.UsedCount++;
        promotion.UpdatedBy = "Storefront";
        return true;
    }

    public PromotionRedemption CreateRedemption(
        Order order,
        PromotionResolution promotion,
        DateTime redeemedAtUtc,
        string actor)
    {
        if (!promotion.PromotionId.HasValue ||
            string.IsNullOrWhiteSpace(promotion.Code) ||
            string.IsNullOrWhiteSpace(promotion.Name) ||
            promotion.DiscountAmount <= 0)
        {
            throw new InvalidOperationException(
                "Không thể tạo lịch sử cho khuyến mãi không hợp lệ.");
        }

        return new PromotionRedemption
        {
            PromotionId = promotion.PromotionId.Value,
            Order = order,
            CustomerUserId = order.CustomerUserId,
            PromotionCode = promotion.Code,
            PromotionName = promotion.Name,
            DiscountAmount = promotion.DiscountAmount,
            RedeemedAt = redeemedAtUtc,
            IsReleased = false,
            CreatedBy = actor
        };
    }

    public async Task<bool> TryReleaseForOrderAsync(
        int orderId,
        DateTime releasedAtUtc,
        string releaseReason,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var redemption = await _db.PromotionRedemptions
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync(
                x => x.OrderId == orderId,
                cancellationToken);

        if (redemption is null ||
            redemption.IsReleased)
        {
            return false;
        }

        if (redemption.Promotion.UsedCount > 0)
        {
            redemption.Promotion.UsedCount--;
        }

        redemption.Promotion.UpdatedBy = actor;

        redemption.IsReleased = true;
        redemption.ReleasedAt = releasedAtUtc;
        redemption.ReleaseReason =
            string.IsNullOrWhiteSpace(releaseReason)
                ? "Đơn hàng đã bị hủy."
                : releaseReason.Trim()[
                    ..Math.Min(
                        releaseReason.Trim().Length,
                        500)];

        redemption.UpdatedBy = actor;

        return true;
    }
    public static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant();
    }

    private async Task<HashSet<int>>
        ResolveEligibleProductIdsAsync(
            PromotionScopeType scopeType,
            IEnumerable<int> configuredProductIds,
            IEnumerable<int> configuredCategoryIds,
            IEnumerable<int> configuredBrandIds,
            IEnumerable<int> cartProductIds,
            CancellationToken cancellationToken)
    {
        var productIds =
            cartProductIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        if (scopeType ==
            PromotionScopeType.AllProducts)
        {
            return productIds.ToHashSet();
        }

        if (scopeType ==
            PromotionScopeType.Products)
        {
            var allowed =
                configuredProductIds.ToHashSet();

            return productIds
                .Where(allowed.Contains)
                .ToHashSet();
        }

        var products = await _db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                productIds.Contains(x.Id) &&
                !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.CategoryId,
                x.BrandId
            })
            .ToListAsync(cancellationToken);

        if (scopeType ==
            PromotionScopeType.Categories)
        {
            var allowed =
                configuredCategoryIds.ToHashSet();

            return products
                .Where(x =>
                    allowed.Contains(x.CategoryId))
                .Select(x => x.Id)
                .ToHashSet();
        }

        var allowedBrands =
            configuredBrandIds.ToHashSet();

        return products
            .Where(x =>
                allowedBrands.Contains(x.BrandId))
            .Select(x => x.Id)
            .ToHashSet();
    }

    private static string GetScopeError(
        PromotionScopeType scopeType)
    {
        return scopeType switch
        {
            PromotionScopeType.Products =>
                "Giỏ hàng không có sản phẩm được áp dụng mã này.",
            PromotionScopeType.Categories =>
                "Giỏ hàng không có sản phẩm thuộc danh mục được áp dụng.",
            PromotionScopeType.Brands =>
                "Giỏ hàng không có sản phẩm thuộc thương hiệu được áp dụng.",
            _ =>
                "Giỏ hàng không có sản phẩm đủ điều kiện."
        };
    }

    private static string GetScopeName(
        PromotionScopeType scopeType)
    {
        return scopeType switch
        {
            PromotionScopeType.Products =>
                "Sản phẩm cụ thể",
            PromotionScopeType.Categories =>
                "Danh mục cụ thể",
            PromotionScopeType.Brands =>
                "Thương hiệu cụ thể",
            _ =>
                "Toàn bộ sản phẩm"
        };
    }
}

public sealed record PromotionCartLine(
    int ProductId,
    decimal LineTotal);

public sealed record PromotionResolution(
    bool IsValid,
    int? PromotionId,
    string? Code,
    string? Name,
    decimal DiscountAmount,
    decimal EligibleSubtotal,
    string? ScopeName,
    string? Error)
{
    public static PromotionResolution None =>
        new(
            true,
            null,
            null,
            null,
            0,
            0,
            null,
            null);

    public static PromotionResolution Invalid(
        string error) =>
        new(
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            error);

    public static PromotionResolution Success(
        int promotionId,
        string code,
        string name,
        decimal discountAmount,
        decimal eligibleSubtotal,
        string scopeName) =>
        new(
            true,
            promotionId,
            code,
            name,
            discountAmount,
            eligibleSubtotal,
            scopeName,
            null);
}




