using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// Single entry point for switching the UI culture from anywhere (panel topbar,
// storefront header). Sets the persistent culture cookie and returns the caller.
public sealed class LanguageModel : PageModel
{
    public IActionResult OnGet(string? culture, string? returnUrl)
    {
        if (culture is "fa" or "en" or "ar")
        {
            Response.Cookies.Append("barang.store.culture", CookieRequestCultureProvider.MakeCookieValue(new(culture)));
        }
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
