using Microsoft.AspNetCore.Mvc.Rendering;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class AdminOrdersViewModel
{
    public string? Query { get; init; }
    public OrderStatus? Status { get; init; }
    public IReadOnlyList<AdminOrderListItemViewModel> Items { get; init; }
        = Array.Empty<AdminOrderListItemViewModel>();

    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
    public int ShippingCount { get; init; }
    public int CompletedCount { get; init; }
    public int CancelledCount { get; init; }
}

public sealed class AdminOrderListItemViewModel
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public decimal TotalAmount { get; init; }
}

public sealed class AdminOrderDetailsViewModel
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public PaymentStatus PaymentStatus { get; init; }
    public string PaymentStatusName { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
    public string? CustomerNote { get; init; }
    public string? CancellationReason { get; init; }

    public string CurrencyCode { get; init; } = "VND";
    public decimal Subtotal { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }

    public IReadOnlyList<AdminOrderItemViewModel> Items { get; init; }
        = Array.Empty<AdminOrderItemViewModel>();
    public IReadOnlyList<SelectListItem> NextStatusOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed class AdminOrderItemViewModel
{
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}
