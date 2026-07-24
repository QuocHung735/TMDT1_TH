using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class OrdersController(
    ApplicationDbContext db,
    ILogger<OrdersController> logger) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<OrdersController> _logger = logger;

    public async Task<IActionResult> Index(
        string? q,
        OrderStatus? status)
    {
        q = string.IsNullOrWhiteSpace(q)
            ? null
            : q.Trim();

        var query = _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (q is not null)
        {
            query = query.Where(x =>
                x.OrderNumber.Contains(q) ||
                x.CustomerName.Contains(q) ||
                x.CustomerPhone.Contains(q) ||
                (x.CustomerEmail != null &&
                 x.CustomerEmail.Contains(q)) ||
                (x.TrackingNumber != null &&
                 x.TrackingNumber.Contains(q)) ||
                (x.ShippingCarrierName != null &&
                 x.ShippingCarrierName.Contains(q)));
        }

        var allStatusCounts = await _db.Orders
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                x => x.Status,
                x => x.Count);

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync();

        var model = new AdminOrdersViewModel
        {
            Query = q,
            Status = status,
            TotalCount =
                allStatusCounts.Values.Sum(),
            PendingCount =
                allStatusCounts.GetValueOrDefault(
                    OrderStatus.Pending),
            ShippingCount =
                allStatusCounts.GetValueOrDefault(
                    OrderStatus.Shipping),
            CompletedCount =
                allStatusCounts.GetValueOrDefault(
                    OrderStatus.Completed),
            CancelledCount =
                allStatusCounts.GetValueOrDefault(
                    OrderStatus.Cancelled),
            Items = orders
                .Select(x =>
                    new AdminOrderListItemViewModel
                    {
                        Id = x.Id,
                        OrderNumber =
                            x.OrderNumber,
                        CreatedAt =
                            x.CreatedAt,
                        CustomerName =
                            x.CustomerName,
                        CustomerPhone =
                            x.CustomerPhone,
                        CustomerEmail =
                            x.CustomerEmail,
                        Status = x.Status,
                        StatusName =
                            OrderDisplay.StatusName(
                                x.Status),
                        StatusClass =
                            OrderDisplay.StatusClass(
                                x.Status),
                        ShippingCarrierName =
                            x.ShippingCarrierName,
                        ShippingServiceName =
                            x.ShippingServiceName,
                        TrackingNumber =
                            x.TrackingNumber,
                        TotalQuantity =
                            x.Items.Sum(item =>
                                item.Quantity),
                        CurrencyCode =
                            x.CurrencyCode,
                        TotalAmount =
                            x.TotalAmount
                    })
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.ShippingService)
                .ThenInclude(x =>
                    x!.ShippingCarrier)
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (order is null)
            return NotFound();

        return View(
            await BuildDetailsModelAsync(
                order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShipment(
        int id,
        int? shippingServiceId,
        string? trackingNumber,
        DateTime? estimatedDeliveryDate,
        string? shippingNote)
    {
        var order = await _db.Orders
            .Include(x => x.ShippingService)
                .ThenInclude(x =>
                    x!.ShippingCarrier)
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (order is null)
            return NotFound();

        if (order.Status == OrderStatus.Cancelled)
        {
            TempData["Error"] =
                "Không thể cập nhật vận chuyển cho đơn đã hủy.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        trackingNumber =
            Clean(trackingNumber);

        shippingNote =
            Clean(shippingNote);

        if (trackingNumber?.Length > 100)
            trackingNumber = trackingNumber[..100];

        if (shippingNote?.Length > 1000)
            shippingNote = shippingNote[..1000];

        var canChangeService =
            order.Status is
                OrderStatus.Pending or
                OrderStatus.Confirmed or
                OrderStatus.Processing;

        if (shippingServiceId.HasValue &&
            shippingServiceId !=
            order.ShippingServiceId)
        {
            if (!canChangeService)
            {
                TempData["Error"] =
                    "Không thể đổi dịch vụ sau khi đơn đã bắt đầu giao.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var service =
                await _db.ShippingServices
                    .AsNoTracking()
                    .Where(x =>
                        x.Id ==
                        shippingServiceId.Value &&
                        x.IsActive &&
                        x.ShippingCarrier.IsActive)
                    .Select(x =>
                        new
                        {
                            x.Id,
                            x.Name,
                            x.BaseFee,
                            x.EstimatedMaxDays,
                            CarrierName =
                                x.ShippingCarrier.Name,
                            TrackingUrlTemplate =
                                x.ShippingCarrier
                                    .TrackingUrlTemplate
                        })
                    .FirstOrDefaultAsync();

            if (service is null)
            {
                TempData["Error"] =
                    "Dịch vụ vận chuyển không còn hoạt động.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            order.ShippingServiceId =
                service.Id;
            order.ShippingCarrierName =
                service.CarrierName;
            order.ShippingServiceName =
                service.Name;
            order.ShippingFee =
                service.BaseFee;
            order.TotalAmount =
                order.Subtotal +
                order.ShippingFee -
                order.DiscountAmount;
            order.EstimatedDeliveryAt =
                estimatedDeliveryDate.HasValue
                    ? AsUtcDate(
                        estimatedDeliveryDate.Value)
                    : DateTime.UtcNow.AddDays(
                        service.EstimatedMaxDays);

            order.TrackingNumber =
                trackingNumber;
            order.TrackingUrl =
                BuildTrackingUrl(
                    service.TrackingUrlTemplate,
                    trackingNumber);
        }
        else
        {
            order.TrackingNumber =
                trackingNumber;

            order.TrackingUrl =
                BuildTrackingUrl(
                    order.ShippingService
                        ?.ShippingCarrier
                        .TrackingUrlTemplate,
                    trackingNumber);

            if (estimatedDeliveryDate.HasValue)
            {
                order.EstimatedDeliveryAt =
                    AsUtcDate(
                        estimatedDeliveryDate.Value);
            }
        }

        order.ShippingNote =
            shippingNote;
        order.UpdatedBy =
            User.Identity?.Name ?? "Admin";

        await _db.SaveChangesAsync();

        TempData["Success"] =
            "Đã cập nhật thông tin giao nhận.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        OrderStatus nextStatus,
        string? cancellationReason)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var order = await _db.Orders
                .Include(x => x.Items)
                .Include(x => x.ShippingService)
                    .ThenInclude(x =>
                        x!.ShippingCarrier)
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

            if (order is null)
                return NotFound();

            if (!CanTransition(
                    order.Status,
                    nextStatus))
            {
                TempData["Error"] =
                    $"Không thể chuyển từ {OrderDisplay.StatusName(order.Status)} " +
                    $"sang {OrderDisplay.StatusName(nextStatus)}.";

                await transaction.RollbackAsync();

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (nextStatus ==
                OrderStatus.Cancelled)
            {
                cancellationReason =
                    Clean(cancellationReason);

                if (string.IsNullOrWhiteSpace(
                        cancellationReason))
                {
                    TempData["Error"] =
                        "Vui lòng nhập lý do hủy đơn.";

                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                if (cancellationReason.Length > 500)
                {
                    cancellationReason =
                        cancellationReason[..500];
                }

                await RestoreStockAsync(
                    order.Items);

                order.CancelledAt =
                    DateTime.UtcNow;
                order.CancellationReason =
                    cancellationReason;
                order.PaymentStatus =
                    PaymentStatus.Unpaid;
            }
            else if (nextStatus ==
                     OrderStatus.Confirmed)
            {
                order.ConfirmedAt ??=
                    DateTime.UtcNow;
            }
            else if (nextStatus ==
                     OrderStatus.Shipping)
            {
                if (!order.ShippingServiceId.HasValue)
                {
                    TempData["Error"] =
                        "Hãy chọn dịch vụ vận chuyển trước khi bắt đầu giao hàng.";

                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                if (string.IsNullOrWhiteSpace(
                        order.TrackingNumber))
                {
                    TempData["Error"] =
                        "Hãy nhập mã vận đơn trước khi chuyển sang Đang giao.";

                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                order.ShippedAt ??=
                    DateTime.UtcNow;

                order.TrackingUrl =
                    BuildTrackingUrl(
                        order.ShippingService
                            ?.ShippingCarrier
                            .TrackingUrlTemplate,
                        order.TrackingNumber);
            }
            else if (nextStatus ==
                     OrderStatus.Completed)
            {
                order.CompletedAt =
                    DateTime.UtcNow;
                order.DeliveredAt =
                    DateTime.UtcNow;
                order.PaymentStatus =
                    PaymentStatus.Paid;
            }

            order.Status = nextStatus;
            order.UpdatedBy =
                User.Identity?.Name ?? "Admin";

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đơn {order.OrderNumber} đã chuyển sang " +
                $"{OrderDisplay.StatusName(order.Status)}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();

            _logger.LogError(
                exception,
                "Không thể cập nhật trạng thái đơn {OrderId}.",
                id);

            TempData["Error"] =
                "Không thể cập nhật trạng thái đơn hàng.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }

    private async Task RestoreStockAsync(
        ICollection<OrderItem> items)
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
                            item.ProductVariantId
                                .Value &&
                            x.ProductId ==
                            item.ProductId)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    x =>
                                        x.StockQuantity,
                                    x =>
                                        x.StockQuantity +
                                        item.Quantity)
                                .SetProperty(
                                    x => x.UpdatedAt,
                                    DateTime.UtcNow)
                                .SetProperty(
                                    x => x.UpdatedBy,
                                    User.Identity!.Name
                                    ?? "Admin"));

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy biến thể SKU {item.Sku} để hoàn kho.");
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
                                    x =>
                                        x.StockQuantity,
                                    x =>
                                        x.StockQuantity +
                                        item.Quantity)
                                .SetProperty(
                                    x => x.UpdatedAt,
                                    DateTime.UtcNow)
                                .SetProperty(
                                    x => x.UpdatedBy,
                                    User.Identity!.Name
                                    ?? "Admin"));

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy sản phẩm SKU {item.Sku} để hoàn kho.");
                }
            }
        }

        foreach (var productId in items
                     .Where(x =>
                         x.ProductId.HasValue)
                     .Select(x =>
                         x.ProductId!.Value)
                     .Distinct())
        {
            var product =
                await _db.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.Id == productId);

            if (product is null ||
                product.IsDeleted)
            {
                continue;
            }

            var stock = product.HasVariants
                ? await _db.ProductVariants
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ProductId ==
                        productId &&
                        x.IsActive &&
                        !x.IsDeleted)
                    .SumAsync(x =>
                        x.StockQuantity)
                : product.StockQuantity;

            if (stock > 0 &&
                product.Status ==
                ProductStatus.OutOfStock)
            {
                product.Status =
                    ProductStatus.Active;

                product.UpdatedBy =
                    User.Identity?.Name
                    ?? "Admin";
            }
        }
    }

    private static bool CanTransition(
        OrderStatus current,
        OrderStatus next)
    {
        return current switch
        {
            OrderStatus.Pending =>
                next is
                    OrderStatus.Confirmed or
                    OrderStatus.Cancelled,
            OrderStatus.Confirmed =>
                next is
                    OrderStatus.Processing or
                    OrderStatus.Cancelled,
            OrderStatus.Processing =>
                next is
                    OrderStatus.Shipping or
                    OrderStatus.Cancelled,
            OrderStatus.Shipping =>
                next ==
                OrderStatus.Completed,
            _ => false
        };
    }

    private static IReadOnlyList<OrderStatus>
        NextStatuses(OrderStatus current)
    {
        return current switch
        {
            OrderStatus.Pending =>
                new[]
                {
                    OrderStatus.Confirmed,
                    OrderStatus.Cancelled
                },
            OrderStatus.Confirmed =>
                new[]
                {
                    OrderStatus.Processing,
                    OrderStatus.Cancelled
                },
            OrderStatus.Processing =>
                new[]
                {
                    OrderStatus.Shipping,
                    OrderStatus.Cancelled
                },
            OrderStatus.Shipping =>
                new[]
                {
                    OrderStatus.Completed
                },
            _ => Array.Empty<OrderStatus>()
        };
    }

    private async Task<AdminOrderDetailsViewModel>
        BuildDetailsModelAsync(Order order)
    {
        var shippingRows =
            await _db.ShippingServices
                .AsNoTracking()
                .Where(x =>
                    (x.IsActive &&
                     x.ShippingCarrier.IsActive) ||
                    x.Id ==
                    order.ShippingServiceId)
                .OrderBy(x =>
                    x.ShippingCarrier.DisplayOrder)
                .ThenBy(x =>
                    x.ShippingCarrier.Name)
                .ThenBy(x =>
                    x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.BaseFee,
                    CarrierName =
                        x.ShippingCarrier.Name
                })
                .ToListAsync();

        var shippingOptions =
            shippingRows
                .Select(x =>
                    new SelectListItem
                    {
                        Value =
                            x.Id.ToString(),
                        Text =
                            $"{x.CarrierName} · {x.Name} · {x.BaseFee:N0} VND",
                        Selected =
                            x.Id ==
                            order.ShippingServiceId
                    })
                .ToList();

        return new AdminOrderDetailsViewModel
        {
            Id = order.Id,
            OrderNumber =
                order.OrderNumber,
            CreatedAt =
                order.CreatedAt,
            Status =
                order.Status,
            StatusName =
                OrderDisplay.StatusName(
                    order.Status),
            StatusClass =
                OrderDisplay.StatusClass(
                    order.Status),
            PaymentStatus =
                order.PaymentStatus,
            PaymentStatusName =
                order.PaymentStatus ==
                PaymentStatus.Paid
                    ? "Đã thanh toán"
                    : order.PaymentStatus ==
                      PaymentStatus.Refunded
                        ? "Đã hoàn tiền"
                        : "Chưa thanh toán",
            CustomerName =
                order.CustomerName,
            CustomerPhone =
                order.CustomerPhone,
            CustomerEmail =
                order.CustomerEmail,
            ShippingAddress =
                string.Join(
                    ", ",
                    new[]
                    {
                        order.AddressLine,
                        order.Ward,
                        order.District,
                        order.Province
                    }.Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x))),
            CustomerNote =
                order.CustomerNote,
            CancellationReason =
                order.CancellationReason,
            ShippingServiceId =
                order.ShippingServiceId,
            ShippingCarrierName =
                order.ShippingCarrierName,
            ShippingServiceName =
                order.ShippingServiceName,
            TrackingNumber =
                order.TrackingNumber,
            TrackingUrl =
                order.TrackingUrl,
            ShippingNote =
                order.ShippingNote,
            EstimatedDeliveryAt =
                order.EstimatedDeliveryAt,
            ShippedAt =
                order.ShippedAt,
            DeliveredAt =
                order.DeliveredAt,
            CanChangeShippingService =
                order.Status is
                    OrderStatus.Pending or
                    OrderStatus.Confirmed or
                    OrderStatus.Processing,
            CurrencyCode =
                order.CurrencyCode,
            Subtotal =
                order.Subtotal,
            ShippingFee =
                order.ShippingFee,
            DiscountAmount =
                order.DiscountAmount,
            TotalAmount =
                order.TotalAmount,
            Items = order.Items
                .OrderBy(x => x.Id)
                .Select(x =>
                    new AdminOrderItemViewModel
                    {
                        ProductName =
                            x.ProductName,
                        VariantName =
                            x.VariantName,
                        Sku = x.Sku,
                        ImageUrl =
                            x.ImageUrl,
                        UnitPrice =
                            x.UnitPrice,
                        Quantity =
                            x.Quantity,
                        LineTotal =
                            x.LineTotal
                    })
                .ToList(),
            NextStatusOptions =
                NextStatuses(order.Status)
                    .Select(status =>
                        new SelectListItem
                        {
                            Value =
                                ((int)status)
                                    .ToString(),
                            Text =
                                OrderDisplay
                                    .StatusName(
                                        status)
                        })
                    .ToList(),
            ShippingServiceOptions =
                shippingOptions
        };
    }

    private static string? BuildTrackingUrl(
        string? template,
        string? trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(template) ||
            string.IsNullOrWhiteSpace(trackingNumber) ||
            !template.Contains(
                "{trackingNumber}",
                StringComparison.Ordinal))
        {
            return null;
        }

        var candidate = template.Replace(
            "{trackingNumber}",
            Uri.EscapeDataString(
                trackingNumber),
            StringComparison.Ordinal);

        return IsSafeHttpUrl(candidate)
            ? candidate
            : null;
    }

    private static bool IsSafeHttpUrl(
        string value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out var uri) &&
               (uri.Scheme ==
                    Uri.UriSchemeHttp ||
                uri.Scheme ==
                    Uri.UriSchemeHttps);
    }

    private static DateTime AsUtcDate(
        DateTime value)
    {
        return DateTime.SpecifyKind(
            value.Date,
            DateTimeKind.Utc);
    }

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
