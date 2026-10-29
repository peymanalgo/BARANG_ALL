using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

public sealed class IndexModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<DesignCard> Featured { get; private set; } = [];
    public long Total { get; private set; }
    public IReadOnlyList<CategoryRow> Categories { get; private set; } = [];



    public async Task OnGet([FromQuery(Name = "ref")] string? refCode)
    {
        if (!string.IsNullOrWhiteSpace(refCode) && refCode.Length is >= 4 and <= 20)
        {
            Response.Cookies.Append("barang.ref", refCode.Trim().ToUpperInvariant(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true });
        }
        JsonElement? page = await core.GetAsync("api/v1/public/catalog?page=1&pageSize=13");
        (Featured, Total) = DesignCard.FromPage(page);

        string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        JsonElement? cats = await core.GetAsync($"api/v1/public/catalog/categories?culture={culture}");
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
