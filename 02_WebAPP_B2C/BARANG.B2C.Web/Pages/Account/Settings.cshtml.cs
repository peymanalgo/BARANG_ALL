using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class SettingsModel : PageModel
{
    public string? Error { get; private set; }

    public IActionResult OnGet(string? error)
    {
        Error = error;
        if (Request.Query.TryGetValue("culture", out Microsoft.Extensions.Primitives.StringValues value))
        {
            string name = value.ToString();
            if (name is "fa" or "en" or "ar")
            {
                Response.Cookies.Append("barang.store.culture", CookieRequestCultureProvider.MakeCookieValue(new(name)));
                return RedirectToPage();
            }
        }
        return Page();
    }
}
