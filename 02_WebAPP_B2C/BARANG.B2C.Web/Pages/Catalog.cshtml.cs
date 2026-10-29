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
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public HashSet<string> FavoriteIds { get; private set; } = new(StringComparer.Ordinal);
    public string CurrentUrl => "/catalog" + QueryString.Create(new Dictionary<string, string?>
    {
        ["page"] = Page.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim()
    }).Value;

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        string search = (Search ?? "").Trim();
        string path = $"api/v1/public/catalog?page={Page}&pageSize={PageSize}" + (search.Length > 0 ? $"&search={Uri.EscapeDataString(search)}" : "") + (string.IsNullOrWhiteSpace(Category) ? "" : $"&categoryCode={Uri.EscapeDataString(Category.Trim())}");
        Task<JsonElement?> catalogTask = core.GetAsync(path);
        Task<JsonElement?> favoritesTask = IsAuthenticated
            ? LoadFavoriteIdsAsync()
            : Task.FromResult<JsonElement?>(null);
        JsonElement? result = await catalogTask;
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

        if (await favoritesTask is JsonElement f && f.ValueKind == JsonValueKind.Array)
        {
            FavoriteIds = f.EnumerateArray()
                .Select(x => x.TryGetProperty("artworkId", out JsonElement fid) ? fid.GetString() : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.Ordinal);
        }
    }

    private async Task<JsonElement?> LoadFavoriteIdsAsync()
    {
        string? username = User.FindFirst("barang:username")?.Value;
        if (string.IsNullOrWhiteSpace(username)) return null;
        JsonElement? customer = await core.GetAsync($"api/v1/client/customers/by-code/{Uri.EscapeDataString(username)}");
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return null;
        return await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/favorites");
    }

    public string PageLink(int pageNumber) => "/catalog" + QueryString.Create(new Dictionary<string, string?>
    {
        ["page"] = pageNumber.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim()
    });
}

public sealed record ConceptMatch(Guid Id, string Title, string? CategoryName, string? Thumbnail, int Score);
