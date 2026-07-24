using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Domain.Entities;

public sealed class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PublicToken { get; set; } = Guid.NewGuid();

    public int MarketId { get; set; }
    public string CurrencyCode { get; set; } = "VND";

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public string? CustomerIp { get; set; }
    public string? UserAgent { get; set; }

    public Market Market { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
