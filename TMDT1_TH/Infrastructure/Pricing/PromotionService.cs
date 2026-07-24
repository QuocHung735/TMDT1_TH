using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Infrastructure.Pricing;

public sealed class PromotionService(
    ApplicationDbContext db)
{
    private readonly ApplicationDbContext _db = db;

    public async Task<PromotionResolution> ResolveAsync(
        string? code,
        decimal subtotal,
        int marketId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);

        if (normalizedCode is null)
            return PromotionResolution.None;

        if (subtotal <= 0)
        {
            return PromotionResolution.Invalid(
                "Đơn hàng chưa có giá trị để áp dụng khuyến mãi.");
        }

        var promotion = await _db.Promotions
            .AsNoTracking()
            .Include(x => x.Markets)
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
                $"Đơn hàng cần tối thiểu " +
                $"{promotion.MinimumOrderAmount:N0}đ " +
                "để sử dụng mã này.");
        }

        decimal discount;

        if (promotion.DiscountType ==
            PromotionDiscountType.Percentage)
        {
            discount =
                subtotal *
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

        discount = Math.Min(discount, subtotal);
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
            discount);
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

    public static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant();
    }
}

public sealed record PromotionResolution(
    bool IsValid,
    int? PromotionId,
    string? Code,
    string? Name,
    decimal DiscountAmount,
    string? Error)
{
    public static PromotionResolution None =>
        new(
            true,
            null,
            null,
            null,
            0,
            null);

    public static PromotionResolution Invalid(
        string error) =>
        new(
            false,
            null,
            null,
            null,
            0,
            error);

    public static PromotionResolution Success(
        int promotionId,
        string code,
        string name,
        decimal discountAmount) =>
        new(
            true,
            promotionId,
            code,
            name,
            discountAmount,
            null);
}
