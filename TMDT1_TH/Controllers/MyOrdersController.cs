using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;
using TMDT1_TH.Infrastructure.Orders;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

[Authorize]
[Route("tai-khoan/don-hang")]
public sealed class MyOrdersController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    CustomerOrderCancellationService cancellationService,
    ILogger<MyOrdersController> logger) : Controller
{
    private readonly ApplicationDbContext _db = db;

    private readonly UserManager<ApplicationUser> _userManager =
        userManager;

    private readonly CustomerOrderCancellationService
        _cancellationService = cancellationService;

    private readonly ILogger<MyOrdersController> _logger =
        logger;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(x => x.CustomerUserId == userId)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var model = new CustomerOrderListViewModel
        {
            Items = orders
                .Select(order =>
                {
                    var firstItem =
                        order.Items
                            .OrderBy(x => x.Id)
                            .FirstOrDefault();

                    return new CustomerOrderListItemViewModel
                    {
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
                        CurrencyCode =
                            order.CurrencyCode,
                        TotalAmount =
                            order.TotalAmount,
                        TotalQuantity =
                            order.Items.Sum(x =>
                                x.Quantity),
                        FirstImageUrl =
                            firstItem?.ImageUrl,
                        FirstProductName =
                            firstItem?.ProductName
                            ?? "Đơn hàng",
                        AdditionalItemCount =
                            Math.Max(
                                order.Items.Count - 1,
                                0),
                        ShippingCarrierName =
                            order.ShippingCarrierName,
                        ShippingServiceName =
                            order.ShippingServiceName,
                        TrackingNumber =
                            order.TrackingNumber
                    };
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> Details(
        string orderNumber)
    {
        var userId = CurrentUserId();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.CustomerUserId == userId &&
                x.OrderNumber == orderNumber);

        if (order is null)
            return NotFound();

        var model =
            new CustomerOrderDetailsViewModel
            {
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
                ProgressStep =
                    OrderDisplay.ProgressStep(
                        order.Status),
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
                PaymentMethodName =
                    order.PaymentMethod ==
                    PaymentMethod.CashOnDelivery
                        ? "Thanh toán khi nhận hàng"
                        : order.PaymentMethod
                            .ToString(),
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
                        new CustomerOrderItemViewModel
                        {
                            ProductName =
                                x.ProductName,
                            VariantName =
                                x.VariantName,
                            Sku = x.Sku,
                            ImageUrl =
                                x.ImageUrl,
                            Unit = x.Unit,
                            UnitPrice =
                                x.UnitPrice,
                            Quantity =
                                x.Quantity,
                            LineTotal =
                                x.LineTotal
                        })
                    .ToList()
            };

        return View(model);
    }

    [HttpPost("{orderNumber}/huy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        string orderNumber,
        string? cancellationReason,
        CancellationToken cancellationToken)
    {
        var normalizedReason =
            CustomerOrderCancellationPolicy
                .NormalizeReason(
                    cancellationReason);

        if (!CustomerOrderCancellationPolicy
                .IsReasonValid(
                    normalizedReason))
        {
            TempData["OrderError"] =
                $"Vui lòng nhập lý do hủy ít nhất " +
                $"{CustomerOrderCancellationPolicy.MinimumReasonLength} ký tự.";

            return RedirectToAction(
                nameof(Details),
                new { orderNumber });
        }

        var userId = CurrentUserId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var order = await _db.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x =>
                        x.CustomerUserId ==
                        userId &&
                        x.OrderNumber ==
                        orderNumber,
                    cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return NotFound();
            }

            if (!CustomerOrderCancellationPolicy
                    .CanCancel(
                        order.Status,
                        order.PaymentStatus))
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                TempData["OrderError"] =
                    "Đơn hàng không còn ở trạng thái Chờ xác nhận nên không thể tự hủy.";

                return RedirectToAction(
                    nameof(Details),
                    new { orderNumber });
            }

            var actor =
                User.Identity?.Name
                ?? $"Customer:{userId}";

            await _cancellationService.CancelAsync(
                order,
                normalizedReason!,
                actor,
                DateTime.UtcNow,
                cancellationToken);

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            TempData["OrderSuccess"] =
                $"Đã hủy đơn {order.OrderNumber}. " +
                "Số lượng sản phẩm và lượt khuyến mãi đã được hoàn lại.";

            return RedirectToAction(
                nameof(Details),
                new { orderNumber });
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();

            _logger.LogError(
                exception,
                "Khách hàng {UserId} không thể hủy đơn {OrderNumber}.",
                userId,
                orderNumber);

            TempData["OrderError"] =
                exception is InvalidOperationException
                    ? exception.Message
                    : "Không thể hủy đơn hàng lúc này. Vui lòng thử lại.";

            return RedirectToAction(
                nameof(Details),
                new { orderNumber });
        }
    }

    private int CurrentUserId()
    {
        var value =
            _userManager.GetUserId(User);

        return int.TryParse(
            value,
            out var userId)
            ? userId
            : throw new InvalidOperationException(
                "Không xác định được tài khoản hiện tại.");
    }
}
