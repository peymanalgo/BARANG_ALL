using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class BlogPostModel(SiteContext site) : PageModel
{
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public string? Thumbnail { get; private set; }
    public string Date { get; private set; } = "";
    public string? SeoDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public bool NotFound { get; private set; }

    public async Task OnGet(string slug)
    {
        JsonElement? result = await site.BlogPostAsync(slug ?? "");
        if (result is not JsonElement r || r.ValueKind != JsonValueKind.Object) { NotFound = true; return; }
        Title = Text(r, "title");
        Body = Text(r, "body");
        Thumbnail = Text(r, "thumbnailUrl") is { Length: > 0 } t ? t : null;
        SeoDescription = Text(r, "seoDescription") is { Length: > 0 } sd ? sd : null;
        SeoKeywords = Text(r, "seoKeywords") is { Length: > 0 } sk ? sk : null;
        if (!string.IsNullOrEmpty(Title))
        {
            ViewData["Title"] = Title;
            if (SeoDescription is not null) ViewData["Description"] = SeoDescription;
            if (SeoKeywords is not null) ViewData["Keywords"] = SeoKeywords;
        }
        Date = r.TryGetProperty("publishedAtUtc", out JsonElement d) && d.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(d.GetString(), out DateTimeOffset dt) ? AppDate.Format(dt) : "";
    }

    private static string Text(JsonElement x, string p) => x.TryGetProperty(p, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
}
