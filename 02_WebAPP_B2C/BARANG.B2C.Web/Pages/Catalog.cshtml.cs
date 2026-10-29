using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class CatalogModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Designs { get; private set; } = [];
    public long Total { get; private set; }
    public int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 24;
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    public IReadOnlyList<(string Code, int Count)> Categories { get; private set; } = [];

    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        string search = (Search ?? "").Trim();
        string path = $"api/v1/public/catalog?page={Page}&pageSize={PageSize}" + (search.Length > 0 ? $"&search={Uri.EscapeDataString(search)}" : "") + (string.IsNullOrWhiteSpace(Category) ? "" : $"&categoryCode={Uri.EscapeDataString(Category.Trim())}");
        JsonElement? result = await core.GetAsync(path);
        (Designs, Total) = DesignCard.FromPage(result);

        JsonElement? cats = await core.GetAsync("api/v1/public/catalog/categories");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => (
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Item1.Length > 0).ToList();
        }
    }

    public string PageLink(int pageNumber) => "/catalog" + QueryString.Create(new Dictionary<string, string?>
    {
        ["page"] = pageNumber.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim()
    });
}
