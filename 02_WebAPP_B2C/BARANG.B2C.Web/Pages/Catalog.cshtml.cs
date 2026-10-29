using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class CatalogModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Designs { get; private set; } = [];
    public IReadOnlyList<ConceptMatch> ConceptResults { get; private set; } = [];
    public bool ConceptActive { get; private set; }
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

        if (search.Length >= 2)
        {
            JsonElement? concept = await core.GetAsync($"api/v1/public/catalog/concept-search?limit=24&q={Uri.EscapeDataString(search)}");
            if (concept is JsonElement ce && ce.ValueKind == JsonValueKind.Array)
            {
                ConceptActive = true;
                ConceptResults = ce.EnumerateArray().Select(x => new ConceptMatch(
                    x.TryGetProperty("id", out JsonElement cid) && Guid.TryParse(cid.GetString(), out Guid cg) ? cg : Guid.Empty,
                    x.TryGetProperty("title", out JsonElement ct) ? ct.GetString() ?? "" : "",
                    x.TryGetProperty("categoryName", out JsonElement cn) ? cn.GetString() : null,
                    x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
                    x.TryGetProperty("score", out JsonElement sc) && sc.TryGetInt32(out int sv) ? sv : 0)).ToList();
            }
        }

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

public sealed record ConceptMatch(Guid Id, string Title, string? CategoryName, string? Thumbnail, int Score);
