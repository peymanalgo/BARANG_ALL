using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class DesignModel(CoreApiClient core) : PageModel
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? CategoryCode { get; private set; }
    public string? Description { get; private set; }
    public string? Preview { get; private set; }
    public string? Thumbnail { get; private set; }
    public bool Found { get; private set; }
    public IReadOnlyList<SimilarDesign> Similar { get; private set; } = [];

    public async Task<IActionResult> OnGet(Guid id)
    {
        Id = id;
        JsonElement? result = await core.GetAsync($"api/v1/public/catalog/{id}");
        if (result is not JsonElement x || x.ValueKind != JsonValueKind.Object) return NotFound();
        Found = true;
        Code = x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "";
        Title = x.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : "";
        CategoryCode = x.TryGetProperty("categoryCode", out JsonElement cc) ? cc.GetString() : null;
        Description = x.TryGetProperty("description", out JsonElement de) ? de.GetString() : null;
        Preview = x.TryGetProperty("previewUrl", out JsonElement pv) ? pv.GetString() : null;
        Thumbnail = x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null;
        ViewData["Title"] = Title;
        ViewData["Description"] = Description ?? Title;

        if (!string.IsNullOrWhiteSpace(Title))
        {
            JsonElement? similar = await core.GetAsync($"api/v1/public/catalog/concept-search?limit=8&q={Uri.EscapeDataString(Title)}");
            if (similar is JsonElement sm && sm.ValueKind == JsonValueKind.Array)
            {
                Similar = sm.EnumerateArray()
                    .Select(x => new SimilarDesign(
                        x.TryGetProperty("id", out JsonElement sid) && Guid.TryParse(sid.GetString(), out Guid sg) ? sg : Guid.Empty,
                        x.TryGetProperty("title", out JsonElement sti) ? sti.GetString() ?? "" : "",
                        x.TryGetProperty("thumbnailUrl", out JsonElement sth) ? sth.GetString() : null,
                        x.TryGetProperty("score", out JsonElement ssc) && ssc.TryGetInt32(out int sv) ? sv : 0))
                    .Where(x => x.Id != id).ToList();
            }
        }
        return Page();
    }

public sealed record SimilarDesign(Guid Id, string Title, string? Thumbnail, int Score);
}
