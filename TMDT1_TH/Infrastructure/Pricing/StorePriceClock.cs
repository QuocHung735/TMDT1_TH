namespace TMDT1_TH.Infrastructure.Pricing;

/// <summary>
/// PriceSchedules trong form Admin sử dụng giờ địa phương Việt Nam
/// từ input datetime-local. Storefront phải so sánh theo cùng múi giờ.
/// </summary>
public static class StorePriceClock
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        ResolveVietnamTimeZone();

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            VietnamTimeZone);

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var timeZoneId in new[]
                 {
                     "SE Asia Standard Time",
                     "Asia/Ho_Chi_Minh"
                 })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
