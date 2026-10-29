using System.Globalization;

namespace BARANG.B2C.Web.Services;

// Date engine mirroring the Core backoffice LocaleDateService: Jalali (Persian
// calendar) for fa, Gregorian for every other culture. Core returns ISO/ISO-like
// timestamps; the display side picks the calendar from the UI culture.
public static class AppDate
{
    private static readonly PersianCalendar Persian = new();

    public static string Culture() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
    public static bool IsJalali(string culture) => culture is "fa";

    public static string Format(DateTimeOffset value, string? culture = null)
    {
        string c = culture ?? Culture();
        DateTimeOffset local = value.ToLocalTime();
        return IsJalali(c)
            ? $"{Persian.GetYear(local.DateTime):0000}/{Persian.GetMonth(local.DateTime):00}/{Persian.GetDayOfMonth(local.DateTime):00}"
            : local.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
    }

    public static string FormatDateTime(DateTimeOffset value, string? culture = null)
    {
        string c = culture ?? Culture();
        DateTimeOffset local = value.ToLocalTime();
        return $"{Format(local, c)} {local:HH:mm}";
    }

    public static string Format(DateOnly value, string? culture = null)
    {
        return FormatDateOnly(value, culture ?? Culture());
    }

    private static string FormatDateOnly(DateOnly value, string c)
    {
        if (!IsJalali(c)) return value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        DateTime d = value.ToDateTime(TimeOnly.MinValue);
        return $"{Persian.GetYear(d):0000}/{Persian.GetMonth(d):00}/{Persian.GetDayOfMonth(d):00}";
    }

    // Canonical wire format the Core API accepts for DateOnly fields.
    public static string Canonical(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
