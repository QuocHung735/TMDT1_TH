using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

[Authorize]
[Route("tai-khoan/don-hang")]
public sealed class MyOrdersController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly UserManager<ApplicationUser> _userManager =
        userManager;

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
                        order.Items.OrderBy(x => x.Id).FirstOrDefault();

                    return new CustomerOrderListItemViewModel
                    {
                        OrderNumber = order.OrderNumber,
                        CreatedAt = order.CreatedAt,
                        Status = order.Status,
                        StatusName =
                            OrderDisplay.StatusName(order.Status),
                        StatusClass =
                            OrderDisplay.StatusClass(order.Status),
                        CurrencyCode = order.CurrencyCode,
                        TotalAmount = order.TotalAmount,
                        TotalQuantity =
                            order.Items.Sum(x => x.Quantity),
                        FirstImageUrl = firstItem?.ImageUrl,
                        FirstProductName =
                            firstItem?.ProductName ?? "Đơn hàng",
                        AdditionalItemCount =
                            Math.Max(order.Items.Count - 1, 0)
                    };
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> Details(string orderNumber)
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

        var model = new CustomerOrderDetailsViewModel
        {
            OrderNumber = order.OrderNumber,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            StatusName =
                OrderDisplay.StatusName(order.Status),
            StatusClass =
                OrderDisplay.StatusClass(order.Status),
            ProgressStep =
                OrderDisplay.ProgressStep(order.Status),
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            CustomerEmail = order.CustomerEmail,
            ShippingAddress = string.Join(
                ", ",
                new[]
                {
                    order.AddressLine,
                    order.Ward,
                    order.District,
                    order.Province
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
            CustomerNote = order.CustomerNote,
            CancellationReason = order.CancellationReason,
            PaymentStatus = order.PaymentStatus,
            PaymentStatusName =
                order.PaymentStatus == PaymentStatus.Paid
                    ? "Đã thanh toán"
                    : order.PaymentStatus == PaymentStatus.Refunded
                        ? "Đã hoàn tiền"
                        : "Chưa thanh toán",
            PaymentMethodName =
                order.PaymentMethod ==
                PaymentMethod.CashOnDelivery
                    ? "Thanh toán khi nhận hàng"
                    : order.PaymentMethod.ToString(),
            CurrencyCode = order.CurrencyCode,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Items = order.Items
                .OrderBy(x => x.Id)
                .Select(x => new CustomerOrderItemViewModel
                {
                    ProductName = x.ProductName,
                    VariantName = x.VariantName,
                    Sku = x.Sku,
                    ImageUrl = x.ImageUrl,
                    Unit = x.Unit,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity,
                    LineTotal = x.LineTotal
                })
                .ToList()
        };

        return View(model);
    }

    private int CurrentUserId()
    {
        var value = _userManager.GetUserId(User);

        return int.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException(
                "Không xác định được tài khoản hiện tại.");
    }
}
