using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// ویزارد قدیمی جایگزین شد — سفارش‌های نیمه‌کاره از پنل کاربری دنبال می‌شوند.
public sealed class PackageModel : PageModel
{
    public IActionResult OnGet(Guid orderId) => Redirect($"/account/order/{orderId}");
    public IActionResult OnPost(Guid orderId) => Redirect($"/account/order/{orderId}");
}
