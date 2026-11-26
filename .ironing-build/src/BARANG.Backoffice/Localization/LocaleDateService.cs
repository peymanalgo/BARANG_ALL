using System.Globalization;

namespace BARANG.Backoffice.Localization;

public interface ILocaleDateService
{
    string Format(DateOnly value,string? culture); string Format(DateTimeOffset value,string? culture);
    string Canonical(DateOnly value);
    bool TryParse(string value,string? culture,out DateOnly date);
    bool TryParseCanonical(string value,out DateOnly date);
    (int Year,int Month,int Day) CalendarDate(DateOnly value,string? culture);
    DateOnly FromCalendar(int year,int month,int day,string? culture);
    DateOnly StartOfMonth(DateOnly value,string? culture);
    DateOnly AddMonths(DateOnly value,int months,string? culture);
}

public sealed class LocaleDateService:ILocaleDateService
{
    private static readonly PersianCalendar Persian=new();
    public string Format(DateOnly value,string? culture){(int year,int month,int day)=CalendarDate(value,culture);return $"{year:0000}/{month:00}/{day:00}";}
    public string Canonical(DateOnly value)=>value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    public string Format(DateTimeOffset value,string? culture)=>$"{Format(DateOnly.FromDateTime(value.UtcDateTime),culture)} {value.UtcDateTime:HH:mm}";
    public bool TryParseCanonical(string value,out DateOnly date)=>DateOnly.TryParseExact(value.Trim(),"yyyy-MM-dd",DateTimeFormatInfo.InvariantInfo,DateTimeStyles.None,out date);
    public bool TryParse(string value,string? culture,out DateOnly date){date=default;string normalized=NormalizeDigits(value.Trim());string[]p=normalized.Split(['/','-'],StringSplitOptions.None);if(p.Length!=3||!int.TryParse(p[0],NumberStyles.None,CultureInfo.InvariantCulture,out int y)||!int.TryParse(p[1],NumberStyles.None,CultureInfo.InvariantCulture,out int m)||!int.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out int d))return false;try{DateTime parsed=LocalePolicy.Resolve(culture).DateCalendar=="Jalali"?Persian.ToDateTime(y,m,d,0,0,0,0):new DateTime(y,m,d);date=DateOnly.FromDateTime(parsed);return true;}catch(ArgumentOutOfRangeException){return false;}}
    public (int Year,int Month,int Day) CalendarDate(DateOnly value,string? culture){DateTime date=value.ToDateTime(TimeOnly.MinValue);return LocalePolicy.Resolve(culture).DateCalendar=="Jalali"?(Persian.GetYear(date),Persian.GetMonth(date),Persian.GetDayOfMonth(date)):(date.Year,date.Month,date.Day);}
    public DateOnly FromCalendar(int year,int month,int day,string? culture)=>DateOnly.FromDateTime(LocalePolicy.Resolve(culture).DateCalendar=="Jalali"?Persian.ToDateTime(year,month,day,0,0,0,0):new DateTime(year,month,day));
    public DateOnly StartOfMonth(DateOnly value,string? culture){(int year,int month,_)=CalendarDate(value,culture);return FromCalendar(year,month,1,culture);}
    public DateOnly AddMonths(DateOnly value,int months,string? culture){if(LocalePolicy.Resolve(culture).DateCalendar=="Jalali")return DateOnly.FromDateTime(Persian.AddMonths(value.ToDateTime(TimeOnly.MinValue),months));return DateOnly.FromDateTime(value.ToDateTime(TimeOnly.MinValue).AddMonths(months));}
    private static string NormalizeDigits(string value){if(string.IsNullOrEmpty(value))return value;Span<char> chars=stackalloc char[value.Length];for(int i=0;i<value.Length;i++){char c=value[i];chars[i]=c switch{>= '۰' and <= '۹'=>(char)('0'+c-'۰'),>= '٠' and <= '٩'=>(char)('0'+c-'٠'),_=>c};}return new string(chars);}
}
