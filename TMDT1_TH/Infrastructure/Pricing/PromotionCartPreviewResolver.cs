using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Cart;

namespace TMDT1_TH.Infrastructure.Pricing;

/// <summary>
/// Tính lại các dòng hàng dùng để xem trước khuyến mãi từ session
/// và database. Không tin ProductId hoặc thành tiền do trình duyệt gửi.
/// </summary>
public sealed class PromotionCartPreviewResolver(
    ApplicationDbContext db,
    CartSessionStore cartStore)
{
    private readonly ApplicationDbContext _db = db;
    private readonly CartSessionStore _cartStore = cartStore;

    public async Task<PromotionCartPreviewResolution>
        ResolveAsync(
            CancellationToken cancellationToken = default)
    {
        var sessionItems = _cartStore
            .GetItems()
            .Where(x =>
                x.ProductId > 0 &&
                x.Quantity > 0)
            .ToList();

        if (sessionItems.Count == 0)
        {
            return PromotionCartPreviewResolution.Failed(
                "Giỏ hàng đang trống.");
        }

        var market = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.CurrencyCode
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (market is null)
        {
            return PromotionCartPreviewResolution.Failed(
                "Chưa có thị trường đang hoạt động để xác định giá.");
        }

        var productIds = sessionItems
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(x =>
                productIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.Status == ProductStatus.Active)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .AsSplitQuery()
            .ToDictionaryAsync(
                x => x.Id,
                cancellationToken);

        var lines = new List<PromotionCartLine>();
        var errors = new List<string>();
        var now = StorePriceClock.Now;

        foreach (var sessionItem in sessionItems)
        {
            if (!products.TryGetValue(
                    sessionItem.ProductId,
                    out var product))
            {
                errors.Add(
                    "Một sản phẩm trong giỏ không còn được bán.");
                continue;
            }

            var line = ResolveLine(
                product,
                sessionItem,
                market.Id,
                now);

            if (line.Line is null)
            {
                errors.Add(
                    line.Error ??
                    $"Sản phẩm {product.Name} không còn đủ điều kiện áp dụng khuyến mãi.");
                continue;
            }

            lines.Add(line.Line);
        }

        if (errors.Count > 0)
        {
            return new PromotionCartPreviewResolution(
                market.Id,
                market.CurrencyCode,
                lines,
                errors);
        }

        return new PromotionCartPreviewResolution(
            market.Id,
            market.CurrencyCode,
            lines,
            Array.Empty<string>());
    }

    private static PreviewLineResolution ResolveLine(
        Product product,
        CartSessionItem sessionItem,
        int marketId,
        DateTime now)
    {
        PriceSchedule? price;
        int stockQuantity;
        string sku;

        if (product.HasVariants)
        {
            if (!sessionItem.ProductVariantId.HasValue)
            {
                return PreviewLineResolution.Failed(
                    $"Sản phẩm {product.Name} cần chọn phân loại.");
            }

            var variant = product.Variants
                .FirstOrDefault(x =>
                    x.Id ==
                    sessionItem.ProductVariantId.Value &&
                    x.IsActive &&
                    !x.IsDeleted);

            if (variant is null)
            {
                return PreviewLineResolution.Failed(
                    $"Phân loại của {product.Name} không còn được bán.");
            }

            price = GetCurrentPrice(
                variant.PriceSchedules,
                marketId,
                now);

            stockQuantity = variant.StockQuantity;
            sku = variant.Sku;
        }
        else
        {
            if (sessionItem.ProductVariantId.HasValue)
            {
                return PreviewLineResolution.Failed(
                    $"Sản phẩm {product.Name} không sử dụng phân loại.");
            }

            price = GetCurrentPrice(
                product.PriceSchedules,
                marketId,
                now);

            stockQuantity = product.StockQuantity;
            sku = product.Sku;
        }

        if (price is null ||
            price.SalePrice <= 0)
        {
            return PreviewLineResolution.Failed(
                $"SKU {sku} chưa có giá bán đang áp dụng.");
        }

        var minQuantity = Math.Max(
            product.MinPurchaseQuantity,
            1);

        var maxByPolicy =
            product.MaxPurchaseQuantity
            ?? stockQuantity;

        var maxQuantity = Math.Min(
            stockQuantity,
            maxByPolicy);

        if (sessionItem.Quantity < minQuantity ||
            sessionItem.Quantity > maxQuantity)
        {
            return PreviewLineResolution.Failed(
                $"Số lượng của SKU {sku} phải từ {minQuantity} đến {maxQuantity}.");
        }

        return PreviewLineResolution.Success(
            new PromotionCartLine(
                product.Id,
                price.SalePrice *
                sessionItem.Quantity));
    }

    private static PriceSchedule? GetCurrentPrice(
        IEnumerable<PriceSchedule> schedules,
        int marketId,
        DateTime now)
    {
        return schedules
            .Where(x =>
                x.IsActive &&
                x.MarketId == marketId &&
                x.ValidFrom <= now &&
                (!x.ValidTo.HasValue ||
                 x.ValidTo.Value > now))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    private sealed record PreviewLineResolution(
        PromotionCartLine? Line,
        string? Error)
    {
        public static PreviewLineResolution Success(
            PromotionCartLine line) =>
            new(line, null);

        public static PreviewLineResolution Failed(
            string error) =>
            new(null, error);
    }
}

public sealed record PromotionCartPreviewResolution(
    int MarketId,
    string CurrencyCode,
    IReadOnlyList<PromotionCartLine> Lines,
    IReadOnlyList<string> Errors)
{
    public bool IsValid =>
        Errors.Count == 0 &&
        Lines.Count > 0;

    public static PromotionCartPreviewResolution Failed(
        string error) =>
        new(
            0,
            "VND",
            Array.Empty<PromotionCartLine>(),
            new[] { error });
}
