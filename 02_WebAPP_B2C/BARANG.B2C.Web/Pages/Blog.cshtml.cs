using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class BlogModel(SiteContext site) : PageModel
{
    public IReadOnlyList<PostCard> Posts { get; private set; } = [];
    public long Total { get; private set; }
    public int Page { get; private set; } = 1;
    public bool HasNext { get; private set; }

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        JsonElement? result = await site.BlogListAsync(Page, 12);
        if (result is not JsonElement r || r.ValueKind != JsonValueKind.Object) return;
        Total = r.TryGetProperty("totalCount", out JsonElement t) && t.TryGetInt64(out long n) ? n : 0;
        string culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        List<PostCard> posts = [];
        if (r.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
            foreach (JsonElement x in items.EnumerateArray())
            {
                // culture-first pick with fa fallback (matches Core's Pick policy)
                string title = culture switch
                {
                    "en" => Text(x, "titleEn") is { Length: > 0 } e ? e : Text(x, "titleFa"),
                    "ar" => Text(x, "titleAr") is { Length: > 0 } a ? a : Text(x, "titleFa"),
                    _ => Text(x, "titleFa"),
                };
                string rawExcerpt = culture switch
                {
                    "en" => Text(x, "excerptEn") is { Length: > 0 } e ? e : Text(x, "excerptFa"),
                    "ar" => Text(x, "excerptAr") is { Length: > 0 } a ? a : Text(x, "excerptFa"),
                    _ => Text(x, "excerptFa"),
                };
                if (rawExcerpt.Length == 0) rawExcerpt = Text(x, "excerptEn");
                posts.Add(new PostCard(
                    Text(x, "slug"), title,
                    rawExcerpt.Length > 140 ? rawExcerpt[..140] + "…" : rawExcerpt,
                    Text(x, "thumbnailUrl"),
                    x.TryGetProperty("publishedAtUtc", out JsonElement d) && d.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(d.GetString(), out DateTimeOffset dt) ? AppDate.Format(dt, culture) : ""));
            }
        Posts = posts;
        HasNext = Page * 12 < Total;
    }

    private static string Text(JsonElement x, string p) => x.TryGetProperty(p, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    public sealed record PostCard(string Slug, string Title, string Excerpt, string? Thumbnail, string Date);
}
