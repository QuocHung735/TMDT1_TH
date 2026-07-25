using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Orders;

namespace TMDT1_TH.Tests.Orders;

public sealed class CustomerOrderCancellationPolicyTests
{
    [Fact]
    public void PendingAndUnpaid_CanBeCancelled()
    {
        var result =
            CustomerOrderCancellationPolicy.CanCancel(
                OrderStatus.Pending,
                PaymentStatus.Unpaid);

        Assert.True(result);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipping)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void NonPendingOrder_CannotBeCancelled(
        OrderStatus status)
    {
        var result =
            CustomerOrderCancellationPolicy.CanCancel(
                status,
                PaymentStatus.Unpaid);

        Assert.False(result);
    }

    [Fact]
    public void PaidOrder_CannotBeCancelledByCustomer()
    {
        var result =
            CustomerOrderCancellationPolicy.CanCancel(
                OrderStatus.Pending,
                PaymentStatus.Paid);

        Assert.False(result);
    }

    [Fact]
    public void NormalizeReason_TrimsAndLimitsLength()
    {
        var input =
            "  " +
            new string(
                'a',
                CustomerOrderCancellationPolicy
                    .MaximumReasonLength +
                20) +
            "  ";

        var normalized =
            CustomerOrderCancellationPolicy
                .NormalizeReason(input);

        Assert.NotNull(normalized);
        Assert.Equal(
            CustomerOrderCancellationPolicy
                .MaximumReasonLength,
            normalized!.Length);
    }

    [Fact]
    public void BuildStoredReason_AddsCustomerPrefix()
    {
        var result =
            CustomerOrderCancellationPolicy
                .BuildStoredReason(
                    "Tôi không còn nhu cầu mua.");

        Assert.Equal(
            "Khách hàng hủy: Tôi không còn nhu cầu mua.",
            result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void InvalidReason_IsRejected(
        string? reason)
    {
        var normalized =
            CustomerOrderCancellationPolicy
                .NormalizeReason(reason);

        Assert.False(
            CustomerOrderCancellationPolicy
                .IsReasonValid(
                    normalized));
    }
}
