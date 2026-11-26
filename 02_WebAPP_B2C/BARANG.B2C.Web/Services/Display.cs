using System.Globalization;
using System.Text.RegularExpressions;

namespace BARANG.B2C.Web.Services;

// نمایش لایه‌ای داده‌های خام: کدهای فنی، ارقام و مبالغ نباید به شکل خام به مشتری برسند.
// این هلپرها قاعده یکدست ارقام (فارسی + جداکننده ٬) و نام نمایشی طرح‌ها را در کل سایت اعمال می‌کنند.
public static partial class Display
{
    private static readonly Regex TitleWithNumber = new(@"^(?<base>.*?)\s*[—–-]+\s*طرح\s*(?<num>\d{4,})$", RegexOptions.Compiled);
    private static readonly Regex TrailingCode = new(@"[A-Z]{2,}-[A-Z]{2,}-\d{6,}", RegexOptions.Compiled);

    // ارقام فارسی/عربی برای fa/ar؛ لاتین برای en/es
    public static string Digits(string text, string? culture = null) => AppDate.Digits(text, culture);

    // مبلغ با جداکننده ٬ (U+066C) در fa/ar و ارقام بومی؛ en/es با کاما و رقم لاتین
    public static string Money(decimal value, string? culture = null)
    {
        string c = culture ?? AppDate.Culture();
        string s = value.ToString("N0", CultureInfo.InvariantCulture);
        if (c is "fa" or "ar") return Digits(s.Replace(",", "\u066C"), c);
        return s;
    }

    public static string Money(decimal? value, string? culture = null) => value is { } v ? Money(v, culture) : "—";

    // «سینما — طرح 18742651» → «سینما · شماره ۲۶۵۱» — نام توصیفی + شماره کوتاه قابل‌حفظ
    public static string Title(string? raw, string? culture = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string c = culture ?? AppDate.Culture();
        Match m = TitleWithNumber.Match(raw.Trim());
        if (m.Success)
        {
            string basename = m.Groups["base"].Value.Trim();
            string num = m.Groups["num"].Value;
            string shortNo = num.Length > 4 ? num[^4..] : num;
            return $"{basename} · {Digits("#" + shortNo, c)}";
        }
        return raw.Trim();
    }

    // کد فنی طرح (ABFA-SSTN-112811083) در متن‌های آزاد (یادداشت سفارش و…) → برچسب انسانی
    public static string FriendlyNotes(string? notes, string? culture = null)
    {
        if (string.IsNullOrWhiteSpace(notes)) return "";
        return TrailingCode.Replace(notes, m => $"طرح {Last4(m.Value, culture)}");
    }

    private static string Last4(string code, string? culture)
    {
        string digits = new([.. code.Where(char.IsDigit)]);
        string tail = digits.Length > 4 ? digits[^4..] : digits;
        return Digits("#" + tail, culture);
    }
}
