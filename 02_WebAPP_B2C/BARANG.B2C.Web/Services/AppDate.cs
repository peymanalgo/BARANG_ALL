using System.Globalization;

namespace BARANG.B2C.Web.Services;

// Date engine mirroring the Core backoffice LocaleDateService: Jalali (Persian
// calendar) for fa, Gregorian for every other culture. Core returns ISO/ISO-like
// timestamps; the display side picks the calendar from the UI culture.
// fa output additionally uses Persian digits (۱۴۰۵/۰۸/۲۱), matching the
// client-side datepicker's digit policy.
public static class AppDate
{
    private static readonly PersianCalendar Persian = new();
    private const string PersianDigits = "۰۱۲۳۴۵۶۷۸۹";

    public static string Culture() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
    public static bool IsJalali(string culture) => culture is "fa";

    // "1405/08/21" → "۱۴۰۵/۰۸/۲۱" (Latin digits → Persian digits)
    public static string ToPersianDigits(string text) =>
        string.Create(text.Length, text, (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                span[i] = c is >= '0' and <= '9' ? PersianDigits[c - '0'] : c;
            }
        });

    // Digit policy shared with store.js/datepicker.js: fa/ar render Persian
    // digits, every other culture keeps Latin digits.
    public static string Digits(string text, string? culture = null) =>
        (culture ?? Culture()) is "fa" or "ar" ? ToPersianDigits(text) : text;

    // "85٪" in fa/ar vs "85%" in en
    public static string Percent(int value, string? culture = null)
    {
        string c = culture ?? Culture();
        return Digits(value.ToString(CultureInfo.InvariantCulture), c) + (c is "en" ? "%" : "٪");
    }

    public static string Format(DateTimeOffset value, string? culture = null)
    {
        string c = culture ?? Culture();
        DateTimeOffset local = value.ToLocalTime();
        if (!IsJalali(c)) return local.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        string s = $"{Persian.GetYear(local.DateTime):0000}/{Persian.GetMonth(local.DateTime):00}/{Persian.GetDayOfMonth(local.DateTime):00}";
        return ToPersianDigits(s);
    }

    public static string FormatDateTime(DateTimeOffset value, string? culture = null)
    {
        string c = culture ?? Culture();
        DateTimeOffset local = value.ToLocalTime();
        return $"{Format(local, c)} {(IsJalali(c) ? ToPersianDigits(local.ToString("HH:mm", CultureInfo.InvariantCulture)) : local.ToString("HH:mm", CultureInfo.InvariantCulture))}";
    }

    public static string Format(DateOnly value, string? culture = null)
    {
        return FormatDateOnly(value, culture ?? Culture());
    }

    private static string FormatDateOnly(DateOnly value, string c)
    {
        if (!IsJalali(c)) return value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        DateTime d = value.ToDateTime(TimeOnly.MinValue);
        return ToPersianDigits($"{Persian.GetYear(d):0000}/{Persian.GetMonth(d):00}/{Persian.GetDayOfMonth(d):00}");
    }

    // Canonical wire format the Core API accepts for DateOnly fields.
    public static string Canonical(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
