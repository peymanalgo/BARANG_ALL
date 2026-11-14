using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// ویزارد قدیمی جایگزین شد — این مسیر فقط برای سلامت لینک‌های قدیمی ریدایرکت می‌کند
// به ویزارد جدید (۴ فاز) با همان طرح.
public sealed class ShirtModel : PageModel
{
    public IActionResult OnGet(Guid designId) => Redirect($"/order/wizard?design={designId}");
    public IActionResult OnPost(Guid designId) => Redirect($"/order/wizard?design={designId}");
}
