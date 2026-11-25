using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class CatalogModel(CoreApiClient core, SiteContext site) : PageModel
{
    public IReadOnlyList<DesignCard> Designs { get; private set; } = [];
    public IReadOnlyList<ConceptMatch> ConceptResults { get; private set; } = [];
    public bool ConceptActive { get; private set; }
    public long Total { get; private set; }
    public int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 24;
    // فیچرهای store: جستجوی مفهومی/علاقه‌مندی‌ها (خطا = روشن؛ fail-open)
    public bool ConceptEnabled { get; private set; } = true;
    public bool FavoritesEnabled { get; private set; } = true;
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    public IReadOnlyList<CategoryChip> Categories { get; private set; } = [];
    public Dictionary<string, string> CategoryNames => Categories.ToDictionary(c => c.Code, c => c.DisplayName);

    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;
    // جستجو در کاتالوگ همیشه مفهومی است؛ حالت عادی فقط برای مرور دسته‌ها
    // فیچر «جستجوی مفهومی» خاموش باشد → حالت جستجو همان مسیر معمولی کاتالوگ (ILIKE) در نظر گرفته می‌شود
    public bool ConceptMode => !string.IsNullOrWhiteSpace(Search) && ConceptEnabled;
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
        string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        string search = (Search ?? "").Trim();
        string catCulture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        string path = $"api/v1/public/catalog?page={Page}&pageSize={PageSize}&culture={catCulture}" + (search.Length > 0 ? $"&search={Uri.EscapeDataString(search)}" : "") + (string.IsNullOrWhiteSpace(Category) ? "" : $"&categoryCode={Uri.EscapeDataString(Category.Trim())}");
        // فیچرهای خاموش: شاخه جستجوی مفهومی کلاً حذف می‌شود و همان مسیر معمولی کاتالوگ (ILIKE) جواب می‌دهد
        Task<bool> conceptFlagTask = site.FeatureEnabledAsync("concept.search");
        Task<bool> favoritesFlagTask = site.FeatureEnabledAsync("favorites");
        Task<JsonElement?> catalogTask = core.GetAsync(path);
        JsonElement? conceptTask = null;
        Task<JsonElement?> favoritesTask;
        await Task.WhenAll(conceptFlagTask, favoritesFlagTask, catalogTask);
        ConceptEnabled = conceptFlagTask.Result;
        FavoritesEnabled = favoritesFlagTask.Result;
        if (ConceptMode && search.Length >= 2 && ConceptEnabled)
            conceptTask = await core.GetAsync($"api/v1/public/catalog/concept-search?limit=60&culture={culture}&q={Uri.EscapeDataString(search)}");
        favoritesTask = IsAuthenticated && FavoritesEnabled
            ? LoadFavoriteIdsAsync()
            : Task.FromResult<JsonElement?>(null);
        JsonElement? result = await catalogTask;
        (Designs, Total) = DesignCard.FromPage(result);

        if (conceptTask is JsonElement ce && ce.ValueKind == JsonValueKind.Array)
        {
            ConceptActive = true;
            ConceptResults = ce.EnumerateArray().Select(x => new ConceptMatch(
                x.TryGetProperty("id", out JsonElement cid) && Guid.TryParse(cid.GetString(), out Guid cg) ? cg : Guid.Empty,
                x.TryGetProperty("title", out JsonElement ct) ? ct.GetString() ?? "" : "",
                x.TryGetProperty("categoryName", out JsonElement cn) ? cn.GetString() : null,
                x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
                x.TryGetProperty("score", out JsonElement sc) && sc.TryGetInt32(out int sv) ? sv : 0)).ToList();
        }

        JsonElement? cats = await core.GetAsync($"api/v1/public/catalog/categories?culture={culture}");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => new CategoryChip(
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Code.Length > 0).ToList();
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
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
    });
}

public sealed record ConceptMatch(Guid Id, string Title, string? CategoryName, string? Thumbnail, int Score);
public sealed record CategoryChip(string Code, string DisplayName, int Count);
