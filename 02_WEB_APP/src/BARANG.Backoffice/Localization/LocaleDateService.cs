using System.Globalization;

namespace BARANG.Backoffice.Localization;

public interface ILocaleDateService
{
    string Format(DateOnly value,string? culture); string Format(DateTimeOffset value,string? culture);
    bool TryParse(string value,string? culture,out DateOnly date);
}

public sealed class LocaleDateService:ILocaleDateService
{
    private static readonly PersianCalendar Persian=new();
    public string Format(DateOnly value,string? culture)=>LocalePolicy.Resolve(culture).DateCalendar=="Jalali"?$"{Persian.GetYear(value.ToDateTime(TimeOnly.MinValue)):0000}/{Persian.GetMonth(value.ToDateTime(TimeOnly.MinValue)):00}/{Persian.GetDayOfMonth(value.ToDateTime(TimeOnly.MinValue)):00}":value.ToString("yyyy/MM/dd",CultureInfo.InvariantCulture);
    public string Format(DateTimeOffset value,string? culture)=>$"{Format(DateOnly.FromDateTime(value.UtcDateTime),culture)} {value.UtcDateTime:HH:mm}";
    public bool TryParse(string value,string? culture,out DateOnly date){date=default;string[]p=value.Trim().Split('/','-');if(p.Length!=3||!int.TryParse(p[0],out int y)||!int.TryParse(p[1],out int m)||!int.TryParse(p[2],out int d))return false;try{DateTime parsed=LocalePolicy.Resolve(culture).DateCalendar=="Jalali"?Persian.ToDateTime(y,m,d,0,0,0,0):new DateTime(y,m,d);date=DateOnly.FromDateTime(parsed);return true;}catch(ArgumentOutOfRangeException){return false;}}
}
