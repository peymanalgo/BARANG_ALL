using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// Order wizard step 1 — choose the design. Selected design travels to the
// next steps via the designId route parameter.
public sealed class SelectModel(CoreApiClient core) : PageModel
{
    public Guid SelectedId { get; private set; }
    public bool HasSelection { get; private set; }
    public string? SelectedTitle { get; private set; }
    public string? SelectedCode { get; private set; }
    public string? SelectedPreview { get; private set; }

    public IReadOnlyList<DesignCard> Designs { get; private set; } = [];
    public IReadOnlyList<CategoryRow> Categories { get; private set; } = [];
    public long Total { get; private set; }
    public int Page { get; private set; } = 1;
    public int PageSize { get; } = 12;
    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }

    public async Task<IActionResult> OnGetAsync([FromQuery] int? page, [FromQuery] Guid? designId)
    {
        Page = Math.Max(1, page ?? 1);
        string search = (Search ?? "").Trim();
        string path = $"api/v1/public/catalog?page={Page}&pageSize={PageSize}" + (search.Length > 0 ? $"&search={Uri.EscapeDataString(search)}" : "") + (string.IsNullOrWhiteSpace(Category) ? "" : $"&categoryCode={Uri.EscapeDataString(Category.Trim())}");
        JsonElement? result = await core.GetAsync(path);
        (Designs, Total) = DesignCard.FromPage(result);

        JsonElement? cats = await core.GetAsync("api/v1/public/catalog/categories");
        if (cats is JsonElement c && c.ValueKind == JsonValueKind.Array)
        {
            Categories = c.EnumerateArray().Select(x => new CategoryRow(
                x.TryGetProperty("code", out JsonElement code) ? code.GetString() ?? "" : "",
                x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? "" : "",
                x.TryGetProperty("count", out JsonElement cnt) && cnt.TryGetInt32(out int n) ? n : 0))
                .Where(x => x.Code.Length > 0).ToList();
        }

        if (designId is Guid selected)
        {
            SelectedId = selected;
            JsonElement? detail = await core.GetAsync($"api/v1/public/catalog/{selected}");
            if (detail is JsonElement dd && dd.ValueKind == JsonValueKind.Object)
            {
                HasSelection = true;
                SelectedTitle = dd.TryGetProperty("title", out JsonElement ti) ? ti.GetString() : null;
                SelectedCode = dd.TryGetProperty("code", out JsonElement cd) ? cd.GetString() : null;
                SelectedPreview = dd.TryGetProperty("previewUrl", out JsonElement pv) ? pv.GetString() : null;
            }
            else
            {
                return RedirectToPage(new { search = Search, category = Category });
            }
        }
        else if (Designs.Count > 0)
        {
            return RedirectToPage(new { designId = Designs[0].Id, search = Search, category = Category });
        }
        ViewData["Title"] = "انتخاب طرح · پیرنگ خان";
        return Page();
    }

    public string PageLink(int pageNumber) => "/order/select" + QueryString.Create(new Dictionary<string, string?>
    {
        ["page"] = pageNumber.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
        ["designId"] = SelectedId == Guid.Empty ? null : SelectedId.ToString()
    });

    public string SelectLink(Guid designId) => "/order/select" + QueryString.Create(new Dictionary<string, string?>
    {
        ["designId"] = designId.ToString(),
        ["search"] = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        ["category"] = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
        ["page"] = Page.ToString()
    });

    public sealed record CategoryRow(string Code, string DisplayName, int Count);
}
