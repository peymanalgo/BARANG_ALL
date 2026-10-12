namespace BARANG.Backoffice.Localization;

public sealed record LocaleDefinition(string Code, string Direction, string DateCalendar);

public static class LocalePolicy
{
    public const string FontStylesheetUrl = "fonts/barang-fonts.css";
    public static readonly IReadOnlyList<string> Supported = ["fa", "en", "ar"];
    public static LocaleDefinition Resolve(string? code) => code?.Split('-')[0].ToLowerInvariant() switch
    {
        "en" => new("en", "ltr", "Gregorian"),
        "ar" => new("ar", "rtl", "Gregorian"),
        _ => new("fa", "rtl", "Jalali")
    };
}
