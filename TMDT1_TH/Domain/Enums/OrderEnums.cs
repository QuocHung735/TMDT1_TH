namespace TMDT1_TH.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Shipping = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PaymentMethod
{
    CashOnDelivery = 1
}

public enum PaymentStatus
{
    Unpaid = 1,
    Paid = 2,
    Refunded = 3
}
