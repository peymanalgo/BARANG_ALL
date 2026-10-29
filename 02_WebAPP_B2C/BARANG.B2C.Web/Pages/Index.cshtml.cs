using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class IndexModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Featured { get; private set; } = [];
    public long Total { get; private set; }
    public IReadOnlyList<(string Code, int Count)> Categories { get; private set; } = [];

    public async Task OnGet()
    {
        JsonElement? page = await core.GetAsync("api/v1/public/catalog?page=1&pageSize=13");
        (Featured, Total) = DesignCard.FromPage(page);

        JsonElement? cats = await core.GetAsync("api/v1/public/catalog/categories");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => (
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Item1.Length > 0).ToList();
        }
    }
}

public sealed record DesignCard(Guid Id, string Code, string Title, string? CategoryCode, string? Thumbnail, string? Preview)
{
    public static (IReadOnlyList<DesignCard>, long) FromPage(JsonElement? page)
    {
        if (page is not JsonElement el || el.ValueKind != JsonValueKind.Object) return ([], 0);
        long total = el.TryGetProperty("total", out JsonElement t) && t.TryGetInt64(out long n) ? n : 0;
        if (!el.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array) return ([], total);
        List<DesignCard> cards = items.EnumerateArray().Select(x => new DesignCard(
            x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
            x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "",
            x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
            x.TryGetProperty("categoryCode", out JsonElement cc) ? cc.GetString() : null,
            x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
            x.TryGetProperty("previewUrl", out JsonElement pv) ? pv.GetString() : null)).ToList();
        return (cards, total);
    }
}
