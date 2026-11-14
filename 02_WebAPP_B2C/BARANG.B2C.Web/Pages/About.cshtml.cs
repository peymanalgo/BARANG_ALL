using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// «درباره ما» اکنون از CMS مدیریت می‌شود
public sealed class AboutModel : PageModel
{
    public IActionResult OnGet() => RedirectPermanent("/page/about");
}
