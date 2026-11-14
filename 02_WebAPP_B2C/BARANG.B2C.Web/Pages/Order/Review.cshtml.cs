using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// ویزارد قدیمی جایگزین شد — پرداخت و پیگیری از پنل کاربری انجام می‌شود.
public sealed class ReviewModel : PageModel
{
    public IActionResult OnGet(Guid orderId) => Redirect($"/account/order/{orderId}");
    public IActionResult OnPost(Guid orderId) => Redirect($"/account/order/{orderId}");
}
