using System.Text.Json;

namespace BARANG.B2C.Web.Services;

// محتوای CMS سایت (منو/فوتر/تنظیمات/وبلاگ) از ERP/Core می‌آید — B2C دیتابیس ندارد.
// پاسخ‌ها ۶۰ ثانیه در حافظه کش می‌شوند و در نبود API به‌آرامی به حالت خالی برمی‌گردند.
public sealed class SiteContext(CoreApiClient core)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    // کش به تفکیک فرهنگ: منو/فوتر/تنظیمات هر زبان جدا کش می‌شوند تا درخواست fa
    // کشِ en را بازنویسی نکند (کش تک‌نمونه‌ای قبلی منوی فارسی را در en نشان می‌داد).
    private static readonly Dictionary<string, CacheBox> _caches = new();
    public sealed record CacheBox(List<MenuLink> Menu, List<FooterWidgetLink> Footer, SiteSettingsBox? Settings, DateTimeOffset LoadedAt);

    public sealed record MenuLink(string Href, string Title, bool Cta, bool Pinned);
    public sealed record FooterWidgetLink(int Column, string Type, string? Title, string? Text, string? Html, string? ImageUrl, string? Href, string? PageSlug);
    public sealed record SiteSettingsBox(string? SiteName, string? LogoUrl, string? FaviconUrl, string? Slogan, string? Email, string? Phone);

    public string Culture => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    private static bool Fresh(CacheBox? box) => box is not null && box.LoadedAt > DateTimeOffset.UtcNow.AddSeconds(-60);

    public async Task<CacheBox> LoadAsync(bool force = false)
    {
        string c = Culture;
        if (!force && _caches.TryGetValue(c, out CacheBox? cached) && Fresh(cached)) return cached;
        await Gate.WaitAsync();
        try
        {
            if (!force && _caches.TryGetValue(c, out cached) && Fresh(cached)) return cached;
            Task<JsonElement?> menu = core.GetAsync($"api/v1/public/site/menu?culture={c}");
            Task<JsonElement?> footer = core.GetAsync($"api/v1/public/site/footer?culture={c}");
            Task<JsonElement?> settings = core.GetAsync($"api/v1/public/site/settings");
            JsonElement? m = await menu, f = await footer, s = await settings;
            List<MenuLink> menuLinks = [];
            if (m is JsonElement ma && ma.ValueKind == JsonValueKind.Array)
                foreach (JsonElement x in ma.EnumerateArray())
                {
                    string type = Text(x, "itemType");
                    string? url = TextOrNull(x, "url");
                    string? pageSlug = TextOrNull(x, "pageSlug");
                    string href = type == "blog" ? "/blog" : url is { Length: > 0 } ? url : pageSlug is { Length: > 0 } ? $"/page/{pageSlug}" : "/";
                    string title = Pick(x, "titleFa", "titleEn", "titleAr", c);
                    if (title.Length == 0) title = Pick(x, "pageTitleFa", "pageTitleEn", "pageTitleAr", c);
                    if (title.Length == 0 && pageSlug is { Length: > 0 }) title = pageSlug;
                    bool cta = href == "/order/wizard";
                    menuLinks.Add(new MenuLink(href, title, cta, x.TryGetProperty("isPinned", out JsonElement p) && p.GetBoolean()));
                }
            List<FooterWidgetLink> widgets = [];
            if (f is JsonElement fa && fa.ValueKind == JsonValueKind.Array)
                foreach (JsonElement x in fa.EnumerateArray())
                {
                    string type = Text(x, "widgetType");
                    string? pageSlug = TextOrNull(x, "pageSlug");
                    string? href = type switch
                    {
                        "page" => pageSlug is { Length: > 0 } ? $"/page/{pageSlug}" : null,
                        "menu" => TextOrNull(x, "url"),
                        _ => null,
                    };
                    widgets.Add(new FooterWidgetLink(
                        x.TryGetProperty("columnIndex", out JsonElement ci) ? ci.GetInt32() : 1,
                        type,
                        type == "page" && pageSlug is { Length: > 0 } ? null : PickOrNull(x, "titleFa", "titleEn", "titleAr", c),
                        TextOrNull(x, "text"), TextOrNull(x, "html"), TextOrNull(x, "imageUrl"), href, pageSlug));
                }
            SiteSettingsBox? box = null;
            if (s is JsonElement sa && sa.ValueKind == JsonValueKind.Object)
                box = new SiteSettingsBox(PickOrNull(sa, "siteNameFa", "siteNameEn", "siteNameAr", c), TextOrNull(sa, "logoUrl"), TextOrNull(sa, "faviconUrl"), PickOrNull(sa, "sloganFa", "sloganEn", "sloganAr", c), TextOrNull(sa, "contactEmail"), TextOrNull(sa, "contactPhone"));
            _caches[c] = new CacheBox(menuLinks, widgets, box, DateTimeOffset.UtcNow);
        }
        catch { /* در نبود Core، حالت قبلی/خالی */ }
        finally { Gate.Release(); }
        return _caches.TryGetValue(c, out CacheBox? current) ? current : new([], [], null, DateTimeOffset.MinValue);
    }

    public async Task<string?> PageAsync(string slug)
    {
        try
        {
            JsonElement? p = await core.GetAsync($"api/v1/public/site/pages/{Uri.EscapeDataString(slug)}?culture={Culture}");
            if (p is not JsonElement e || e.ValueKind != JsonValueKind.Object) return null;
            return JsonSerializer.Serialize(e);
        }
        catch { return null; }
    }

    public async Task<JsonElement?> BlogListAsync(int page = 1, int pageSize = 12)
    {
        try { return await core.GetAsync($"api/v1/public/site/blog?page={page}&pageSize={pageSize}&culture={Culture}"); }
        catch { return null; }
    }

    public async Task<JsonElement?> BlogPostAsync(string slug)
    {
        try { return await core.GetAsync($"api/v1/public/site/blog/{Uri.EscapeDataString(slug)}?culture={Culture}"); }
        catch { return null; }
    }

    private static string Text(JsonElement x, string p) => x.TryGetProperty(p, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static string? TextOrNull(JsonElement x, string p) { string v = Text(x, p); return v.Length == 0 ? null : v; }
    private static string Pick(JsonElement x, string fa, string en, string ar, string culture) => culture switch { "en" => Text(x, en) is { Length: > 0 } e ? e : Text(x, fa), "ar" => Text(x, ar) is { Length: > 0 } a ? a : Text(x, fa), _ => Text(x, fa) };
    private static string? PickOrNull(JsonElement x, string fa, string en, string ar, string culture) { string v = Pick(x, fa, en, ar, culture); return v.Length == 0 ? null : v; }
}
