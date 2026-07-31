namespace TMDT1_TH.Infrastructure.Pricing;

public static class PromotionConstraintPolicy
{
    public static bool CanPermanentlyDelete(
        int usedCount,
        bool hasRedemptionHistory)
    {
        return usedCount == 0 &&
               !hasRedemptionHistory;
    }

    public static bool IsUsageLimitValid(
        int? usageLimit,
        int usedCount)
    {
        return !usageLimit.HasValue ||
               usageLimit.Value >= usedCount;
    }

    public static string DeleteBlockedMessage(
        int usedCount,
        bool hasRedemptionHistory)
    {
        if (hasRedemptionHistory)
        {
            return
                "Khuyến mãi đã có lịch sử sử dụng nên phải được giữ lại để đối soát. " +
                "Hệ thống đã tạm tắt thay vì xóa.";
        }

        if (usedCount > 0)
        {
            return
                "Khuyến mãi đang có lượt sử dụng nên chỉ được tạm tắt.";
        }

        return string.Empty;
    }
}
