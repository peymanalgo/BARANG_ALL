using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record FavoriteItem(Guid ArtworkId, string Title, string? ThumbnailUrl, DateTimeOffset CreatedAtUtc);

[Authorize(Policy = "LoggedIn")]
public sealed class FavoritesModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public IReadOnlyList<FavoriteItem> Items { get; private set; } = [];

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = "/account/favorites" });
    }

    public async Task<IActionResult> OnGet()
    {
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return Page();
        JsonElement? favs = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/favorites");
        if (ReauthGuard() is { } r) return r;
        if (favs is JsonElement f && f.ValueKind == JsonValueKind.Array)
        {
            Items = f.EnumerateArray().Select(x => new FavoriteItem(
                x.TryGetProperty("artworkId", out JsonElement fid) && Guid.TryParse(fid.GetString(), out Guid fg) ? fg : Guid.Empty,
                x.TryGetProperty("title", out JsonElement ft) ? ft.GetString() ?? "" : "",
                x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
                x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), out DateTimeOffset d) ? d : default)).ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid artworkId, string? returnUrl)
    {
        if (artworkId == Guid.Empty || await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid))
            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/account/favorites" : returnUrl);
        string customerId = cid.GetString() ?? "";
        JsonElement? favs = await core.GetAsync($"api/v1/client/customers/{customerId}/favorites");
        bool isFavorite = favs is JsonElement f && f.ValueKind == JsonValueKind.Array
            && f.EnumerateArray().Any(x => x.TryGetProperty("artworkId", out JsonElement fid) && fid.GetString() == artworkId.ToString());
        if (isFavorite)
        {
            await core.DeleteAsync($"api/v1/client/customers/{customerId}/favorites/{artworkId}");
        }
        else
        {
            await core.PutRawAsync($"api/v1/client/customers/{customerId}/favorites/{artworkId}", new { });
        }
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/account/favorites" : returnUrl);
    }
}
