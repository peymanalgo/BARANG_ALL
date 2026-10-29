using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class IndexModel(CoreApiClient core) : PageModel
{
    public string? DisplayName { get; private set; }
    public string? Username { get; private set; }
    public string? Message { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? NewPassword { get; set; }

    public async Task OnGet(string? message, string? error)
    {
        Message = message;
        Error = error;
        JsonElement? me = await core.GetAsync("api/v1/auth/me");
        if (me is JsonElement x && x.ValueKind == JsonValueKind.Object)
        {
            DisplayName = x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() : null;
            Username = x.TryGetProperty("username", out JsonElement un) ? un.GetString() : null;
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            return RedirectToPage(new { error = "empty" });
        }
        (bool ok, string? problem) = await core.PostRawAsync("api/v1/auth/change-password", new { currentPassword = CurrentPassword, newPassword = NewPassword });
        if (ok)
        {
            await core.LogoutAsync();
            return RedirectToPage("/Account/Login", new { returnUrl = "/account" });
        }
        return RedirectToPage(new { error = problem ?? "failed" });
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await core.LogoutAsync();
        return RedirectToPage("/Index");
    }
}
