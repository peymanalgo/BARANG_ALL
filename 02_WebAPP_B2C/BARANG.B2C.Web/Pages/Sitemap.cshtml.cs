using System.Security;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

// Dynamic sitemap: static pages plus every published design of the public
// catalog (walked page by page). Response-cached to stay cheap.
public sealed class SitemapModel(CoreApiClient core, IConfiguration configuration) : PageModel
{
    [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> OnGetAsync()
    {
        string baseUrl = configuration["Store:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        List<string> urls =
        [
            $"{baseUrl}/",
            $"{baseUrl}/catalog",
            $"{baseUrl}/account/login"
        ];
        int page = 1;
        while (page <= 40)
        {
            JsonElement? result = await core.GetAsync($"api/v1/public/catalog?page={page}&pageSize=60");
            if (result is not JsonElement el || el.ValueKind != JsonValueKind.Object) break;
            int fetched = 0;
            if (el.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g))
                        urls.Add($"{baseUrl}/design/{g}");
                    fetched++;
                }
            }
            if (fetched == 0) break;
            page++;
        }

        string body = string.Join("\n", urls.Select(u => $"  <url><loc>{SecurityElement.Escape(u)}</loc></url>"));
        string xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n{body}\n</urlset>";
        return Content(xml, "application/xml; charset=utf-8");
    }
}
