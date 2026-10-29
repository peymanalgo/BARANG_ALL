using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class ChangePasswordModel(CoreApiClient core) : PageModel
{
    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? NewPassword { get; set; }

    public IActionResult OnGet() => RedirectToPage("/Account/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            return RedirectToPage("/Account/Index", new { error = "empty" });
        }
        (bool ok, string? problem) = await core.PostRawAsync("api/v1/auth/change-password", new { currentPassword = CurrentPassword, newPassword = NewPassword });
        if (ok)
        {
            await core.LogoutAsync();
            return RedirectToPage("/Account/Login", new { returnUrl = "/account", changed = 1 });
        }
        return RedirectToPage("/Account/Index", new { error = problem ?? "failed" });
    }
}
