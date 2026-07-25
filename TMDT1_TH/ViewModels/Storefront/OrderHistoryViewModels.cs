using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Orders;

namespace TMDT1_TH.ViewModels.Storefront;

public sealed class CustomerOrderListViewModel
{
    public IReadOnlyList<CustomerOrderListItemViewModel> Items { get; init; }
        = Array.Empty<CustomerOrderListItemViewModel>();
}

public sealed class CustomerOrderListItemViewModel
{
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "VND";
    public decimal TotalAmount { get; init; }
    public int TotalQuantity { get; init; }
    public string? FirstImageUrl { get; init; }
    public string FirstProductName { get; init; } = string.Empty;
    public int AdditionalItemCount { get; init; }

    public string? ShippingCarrierName { get; init; }
    public string? ShippingServiceName { get; init; }
    public string? TrackingNumber { get; init; }
}

public sealed class CustomerOrderDetailsViewModel
{
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public int ProgressStep { get; init; }

    public bool IsCancelled =>
        Status == OrderStatus.Cancelled;

    public bool CanCustomerCancel =>
        CustomerOrderCancellationPolicy
            .CanCancel(
                Status,
                PaymentStatus);

    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
    public string? CustomerNote { get; init; }
    public string? CancellationReason { get; init; }

    public string? ShippingCarrierName { get; init; }
    public string? ShippingServiceName { get; init; }
    public string? TrackingNumber { get; init; }
    public string? TrackingUrl { get; init; }
    public string? ShippingNote { get; init; }
    public DateTime? EstimatedDeliveryAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }

    public PaymentStatus PaymentStatus { get; init; }
    public string PaymentStatusName { get; init; } = string.Empty;
    public string PaymentMethodName { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = "VND";
    public decimal Subtotal { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }

    public IReadOnlyList<CustomerOrderItemViewModel> Items { get; init; }
        = Array.Empty<CustomerOrderItemViewModel>();
}

public sealed class CustomerOrderItemViewModel
{
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string Unit { get; init; } = "Cái";
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public static class OrderDisplay
{
    public static string StatusName(OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang chuẩn bị hàng",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

    public static string StatusClass(OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending => "is-pending",
            OrderStatus.Confirmed => "is-confirmed",
            OrderStatus.Processing => "is-processing",
            OrderStatus.Shipping => "is-shipping",
            OrderStatus.Completed => "is-completed",
            OrderStatus.Cancelled => "is-cancelled",
            _ => string.Empty
        };

    public static int ProgressStep(OrderStatus status) =>
        status switch
        {
            OrderStatus.Pending => 1,
            OrderStatus.Confirmed => 2,
            OrderStatus.Processing => 3,
            OrderStatus.Shipping => 4,
            OrderStatus.Completed => 5,
            _ => 0
        };
}
