using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Infrastructure.Orders;

public sealed class CustomerOrderCancellationService(
    ApplicationDbContext db,
    PromotionService promotionService)
{
    private readonly ApplicationDbContext _db = db;

    private readonly PromotionService _promotionService =
        promotionService;

    public async Task CancelAsync(
        Order order,
        string normalizedReason,
        string actor,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!CustomerOrderCancellationPolicy.CanCancel(
                order.Status,
                order.PaymentStatus))
        {
            throw new InvalidOperationException(
                "Đơn hàng không còn đủ điều kiện để khách tự hủy.");
        }

        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "Đơn hàng không có chi tiết sản phẩm để hoàn kho.");
        }

        actor = string.IsNullOrWhiteSpace(actor)
            ? "Customer"
            : actor.Trim();

        var storedReason =
            CustomerOrderCancellationPolicy
                .BuildStoredReason(
                    normalizedReason);

        await RestoreStockAsync(
            order.Items,
            actor,
            cancellationToken);

        await _promotionService
            .TryReleaseForOrderAsync(
                order.Id,
                cancelledAtUtc,
                storedReason,
                actor,
                cancellationToken);

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = cancelledAtUtc;
        order.CancellationReason = storedReason;
        order.PaymentStatus = PaymentStatus.Unpaid;
        order.UpdatedBy = actor;
    }

    private async Task RestoreStockAsync(
        ICollection<OrderItem> items,
        string actor,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            if (item.ProductVariantId.HasValue)
            {
                var affected =
                    await _db.ProductVariants
                        .IgnoreQueryFilters()
                        .Where(x =>
                            x.Id ==
                            item.ProductVariantId.Value &&
                            x.ProductId ==
                            item.ProductId)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    x => x.StockQuantity,
                                    x =>
                                        x.StockQuantity +
                                        item.Quantity)
                                .SetProperty(
                                    x => x.UpdatedAt,
                                    DateTime.UtcNow)
                                .SetProperty(
                                    x => x.UpdatedBy,
                                    actor),
                            cancellationToken);

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy biến thể SKU " +
                        $"{item.Sku} để hoàn kho.");
                }
            }
            else if (item.ProductId.HasValue)
            {
                var affected =
                    await _db.Products
                        .IgnoreQueryFilters()
                        .Where(x =>
                            x.Id ==
                            item.ProductId.Value)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    x => x.StockQuantity,
                                    x =>
                                        x.StockQuantity +
                                        item.Quantity)
                                .SetProperty(
                                    x => x.UpdatedAt,
                                    DateTime.UtcNow)
                                .SetProperty(
                                    x => x.UpdatedBy,
                                    actor),
                            cancellationToken);

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy sản phẩm SKU " +
                        $"{item.Sku} để hoàn kho.");
                }
            }
        }

        var productIds = items
            .Where(x => x.ProductId.HasValue)
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToList();

        foreach (var productId in productIds)
        {
            var product =
                await _db.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        x => x.Id == productId,
                        cancellationToken);

            if (product is null ||
                product.IsDeleted)
            {
                continue;
            }

            var stock = product.HasVariants
                ? await _db.ProductVariants
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ProductId == productId &&
                        x.IsActive &&
                        !x.IsDeleted)
                    .SumAsync(
                        x => x.StockQuantity,
                        cancellationToken)
                : product.StockQuantity;

            if (stock > 0 &&
                product.Status ==
                ProductStatus.OutOfStock)
            {
                product.Status =
                    ProductStatus.Active;

                product.UpdatedBy = actor;
            }
        }
    }
}
