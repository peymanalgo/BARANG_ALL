using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class IndexModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Featured { get; private set; } = [];
    public long Total { get; private set; }
    public IReadOnlyList<CategoryRow> Categories { get; private set; } = [];

    public async Task OnGet()
    {
        JsonElement? page = await core.GetAsync("api/v1/public/catalog?page=1&pageSize=13");
        (Featured, Total) = DesignCard.FromPage(page);

        JsonElement? cats = await core.GetAsync("api/v1/public/catalog/categories");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => new CategoryRow(
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Code.Length > 0).ToList();
        }
    }

    public sealed record CategoryRow(string Code, string DisplayName, int Count);
}
