using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Infrastructure.Orders;

public static class CustomerOrderCancellationPolicy
{
    public const int MinimumReasonLength = 5;
    public const int MaximumReasonLength = 450;

    private const string CustomerPrefix =
        "Khách hàng hủy: ";

    public static bool CanCancel(
        OrderStatus status,
        PaymentStatus paymentStatus)
    {
        return status == OrderStatus.Pending &&
               paymentStatus == PaymentStatus.Unpaid;
    }

    public static string? NormalizeReason(
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        var normalized = reason.Trim();

        return normalized.Length <= MaximumReasonLength
            ? normalized
            : normalized[..MaximumReasonLength];
    }

    public static bool IsReasonValid(
        string? normalizedReason)
    {
        return !string.IsNullOrWhiteSpace(
                   normalizedReason) &&
               normalizedReason.Length >=
                   MinimumReasonLength;
    }

    public static string BuildStoredReason(
        string normalizedReason)
    {
        if (!IsReasonValid(normalizedReason))
        {
            throw new ArgumentException(
                $"Lý do hủy cần ít nhất " +
                $"{MinimumReasonLength} ký tự.",
                nameof(normalizedReason));
        }

        var result =
            CustomerPrefix +
            normalizedReason.Trim();

        return result.Length <= 500
            ? result
            : result[..500];
    }
}
