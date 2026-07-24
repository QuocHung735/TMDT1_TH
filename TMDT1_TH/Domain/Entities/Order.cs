using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;

namespace TMDT1_TH.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PublicToken { get; set; } = Guid.NewGuid();

    public int? CustomerUserId { get; set; }

    public int MarketId { get; set; }
    public string CurrencyCode { get; set; } = "VND";

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } =
        PaymentMethod.CashOnDelivery;
    public PaymentStatus PaymentStatus { get; set; } =
        PaymentStatus.Unpaid;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }

    public int? ShippingServiceId { get; set; }
    public string? ShippingCarrierName { get; set; }
    public string? ShippingServiceName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    public string? ShippingNote { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PromotionCode { get; set; }
    public string? PromotionName { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public string? CustomerIp { get; set; }
    public string? UserAgent { get; set; }

    public ApplicationUser? CustomerUser { get; set; }
    public Market Market { get; set; } = null!;
    public ShippingService? ShippingService { get; set; }
    public PromotionRedemption? PromotionRedemption { get; set; }
    public ICollection<OrderItem> Items { get; set; } =
        new List<OrderItem>();
}




