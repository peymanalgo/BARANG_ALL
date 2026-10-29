using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record CustomDesignItem(Guid Id, string Title, string? ThumbnailUrl, string? PreviewUrl, DateTimeOffset CreatedAtUtc);

[Authorize(Policy = "LoggedIn")]
public sealed class CustomDesignsModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public IReadOnlyList<CustomDesignItem> Items { get; private set; } = [];
    public string? Error { get; private set; }

    [BindProperty] public IFormFile? UploadFile { get; set; }
    [BindProperty] public string? UploadTitle { get; set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = "/account/custom-designs" });
    }

    public async Task<IActionResult> OnGet(string? error)
    {
        Error = error;
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return Page();
        JsonElement? designs = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/custom-designs");
        if (ReauthGuard() is { } r) return r;
        if (designs is JsonElement d && d.ValueKind == JsonValueKind.Array)
        {
            Items = d.EnumerateArray().Select(x => new CustomDesignItem(
                x.TryGetProperty("id", out JsonElement did) && Guid.TryParse(did.GetString(), out Guid dg) ? dg : Guid.Empty,
                x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
                x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null,
                x.TryGetProperty("previewUrl", out JsonElement pr) ? pr.GetString() : null,
                x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), out DateTimeOffset dt) ? dt : default)).ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (string.IsNullOrWhiteSpace(UploadTitle)) return RedirectToPage(new { error = "title" });
        if (UploadFile is null || UploadFile.Length == 0) return RedirectToPage(new { error = "file" });
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        await using MemoryStream ms = new();
        await UploadFile.CopyToAsync(ms);
        ms.Position = 0;
        (bool ok, JsonElement? value, string? problem) = await core.PostMultipartAsync(
            $"api/v1/client/customers/{cid.GetString()}/custom-designs",
            UploadFile.FileName, UploadFile.ContentType, ms, ms.Length,
            idempotencyKey: Guid.NewGuid().ToString("N"),
            fields: new Dictionary<string, string> { ["title"] = UploadTitle.Trim() });
        if (!ok) return RedirectToPage(new { error = "upload" });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid designId)
    {
        if (designId == Guid.Empty || await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage();
        await core.DeleteAsync($"api/v1/client/customers/{cid.GetString()}/custom-designs/{designId}");
        return RedirectToPage();
    }
}
