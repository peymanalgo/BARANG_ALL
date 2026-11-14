using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// صفحات محتوایی CMS — بدنه HTML از ادیتور داخل ERP می‌آید + کاور و سئو
public sealed class CmsPageModel(SiteContext site) : PageModel
{
    public string Title { get; private set; } = "";
    public string Body { get; private set; } = "";
    public string? CoverUrl { get; private set; }
    public string? SeoDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public bool NotFound { get; private set; }

    public async Task OnGet(string slug)
    {
        string? json = await site.PageAsync(slug ?? "");
        if (json is null) { NotFound = true; return; }
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement r = doc.RootElement;
        Title = Text(r, "title");
        Body = Text(r, "body");
        CoverUrl = Text(r, "coverUrl") is { Length: > 0 } c ? c : null;
        SeoDescription = Text(r, "seoDescription") is { Length: > 0 } d ? d : null;
        SeoKeywords = Text(r, "seoKeywords") is { Length: > 0 } k ? k : null;
        if (Title.Length == 0) NotFound = true;
        if (!NotFound)
        {
            ViewData["Title"] = Title;
            if (SeoDescription is not null) ViewData["Description"] = SeoDescription;
            if (SeoKeywords is not null) ViewData["Keywords"] = SeoKeywords;
        }
    }

    private static string Text(JsonElement x, string p) => x.TryGetProperty(p, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
}
