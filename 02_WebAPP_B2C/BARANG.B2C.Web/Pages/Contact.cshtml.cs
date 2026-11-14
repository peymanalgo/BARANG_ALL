using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// «تماس با ما» اکنون از CMS مدیریت می‌شود
public sealed class ContactModel : PageModel
{
    public IActionResult OnGet() => RedirectPermanent("/page/contact");
}
