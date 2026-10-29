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

    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        string search = (Search ?? "").Trim();
        string path = $"api/v1/public/catalog?page={Page}&pageSize={PageSize}" + (search.Length > 0 ? $"&search={Uri.EscapeDataString(search)}" : "");
        JsonElement? result = await core.GetAsync(path);
        (Designs, Total) = DesignCard.FromPage(result);
    }

    public string PageLink(int pageNumber) => "/catalog" + QueryString.Create(new Dictionary<string, string?>
    {
        ["page"] = pageNumber.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()
    });
}
