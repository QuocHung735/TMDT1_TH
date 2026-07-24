using System.Globalization;

namespace TMDT1_TH.Infrastructure.Time;

/// <summary>
/// Chuẩn hóa các thời điểm được lưu bằng UTC trong database
/// và hiển thị theo múi giờ Việt Nam.
/// </summary>
public static class VietnamDateTime
{
    private static readonly CultureInfo VietnameseCulture =
        CultureInfo.GetCultureInfo("vi-VN");

    private static readonly TimeZoneInfo VietnamTimeZone =
        ResolveVietnamTimeZone();

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            VietnamTimeZone);

    public static DateTime FromUtc(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(
            utcValue,
            VietnamTimeZone);
    }

    public static DateTime? FromUtc(DateTime? value) =>
        value.HasValue
            ? FromUtc(value.Value)
            : null;

    public static string Format(
        DateTime value,
        string format = "dd/MM/yyyy HH:mm")
    {
        return FromUtc(value)
            .ToString(
                format,
                VietnameseCulture);
    }

    public static string Format(
        DateTime? value,
        string format = "dd/MM/yyyy HH:mm",
        string emptyText = "—")
    {
        return value.HasValue
            ? Format(value.Value, format)
            : emptyText;
    }

    public static DateTime ToUtc(
        DateTime vietnamLocalTime)
    {
        var unspecified =
            DateTime.SpecifyKind(
                vietnamLocalTime,
                DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            unspecified,
            VietnamTimeZone);
    }

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

        return TimeZoneInfo.CreateCustomTimeZone(
            "UTC+07",
            TimeSpan.FromHours(7),
            "UTC+07",
            "UTC+07");
    }
}
