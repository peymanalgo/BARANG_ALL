using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// Order wizard final step — pricing breakdown, validation issues and
// submit. Mirrors ERP steps 6+7.
[Authorize(Policy = "LoggedIn")]
public sealed class ReviewModel(CoreApiClient core) : PageModel
{
    public Guid OrderId { get; private set; }
    public Guid DesignId { get; set; }
    public string? OrderNumber { get; private set; }
    public string OrderStatus { get; private set; } = "";
    public long Version { get; private set; }
    public string? ShippingMethod { get; private set; }
    public string? PackagingType { get; private set; }
    public int ItemsCount { get; private set; }
    public decimal ItemsQty { get; private set; }
    public JsonElement? Quote { get; private set; }
    public List<(string Label, string Value)> QuoteRows { get; private set; } = [];
    public (string Label, string Value)? TotalRow { get; private set; }
    public IReadOnlyList<(string Code, string? Field)> Issues { get; private set; } = [];
    public bool Found { get; private set; }
    public string? Error { get; private set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = $"/order/review/{OrderId}" });
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, Guid? designId, string? error)
    {
        OrderId = orderId;
        if (designId is Guid d) DesignId = d;
        Error = error;
        if (ReauthGuard() is { } r0) return r0;

        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        if (order is not JsonElement o || o.ValueKind != JsonValueKind.Object) return NotFound();
        Found = true;
        OrderNumber = o.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        OrderStatus = o.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "";
        Version = o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
        if (o.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
        {
            var active = items.EnumerateArray().Where(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False).ToList();
            ItemsCount = active.Count;
            ItemsQty = active.Sum(x => x.TryGetProperty("quantity", out JsonElement q2) && q2.ValueKind == JsonValueKind.Number ? q2.GetDecimal() : 0);
        }
        JsonElement? profile = await core.GetAsync($"api/v1/client/orders/{orderId}/business-profile");
        if (profile is JsonElement p && p.ValueKind == JsonValueKind.Object)
        {
            ShippingMethod = p.TryGetProperty("shippingMethodCode", out JsonElement sm) ? sm.GetString() : null;
            PackagingType = p.TryGetProperty("packagingTypeCode", out JsonElement pt) ? pt.GetString() : null;
        }

        Quote = (await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/pricing/preview", new { version = Version })).Value;
        ParseQuote(Quote);

        JsonElement? validation = await core.GetAsync($"api/v1/client/orders/{orderId}/validate");
        if (validation is JsonElement va && va.ValueKind == JsonValueKind.Array)
        {
            Issues = va.EnumerateArray().Select(x => (
                Code: x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "",
                Field: x.TryGetProperty("field", out JsonElement fl) ? fl.GetString() : null)).ToList();
        }
        ViewData["Title"] = "قیمت و تأیید · پیرنگ";
        return Page();
    }

    private void ParseQuote(JsonElement? quote)
    {
        if (quote is not JsonElement q || q.ValueKind != JsonValueKind.Object) return;
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        foreach (var prop in q.EnumerateObject())
        {
            string text = prop.Value.ValueKind switch
            {
                JsonValueKind.Number => prop.Value.GetRawText(),
                JsonValueKind.String => prop.Value.GetString() ?? "",
                _ => ""
            };
            if (text.Length == 0) continue;
            if (prop.Name is "version" or "taxRatePercent") continue;
            string Label(string fa, string en, string ar) => culture switch { "en" => en, "ar" => ar, _ => fa };
            var label = prop.Name switch
            {
                "rawProductTotal" => Label("محصول خام", "Raw product", "المنتج الخام"),
                "printTotal" => Label("چاپ", "Printing", "الطباعة"),
                "packagingTotal" => Label("بسته‌بندی", "Packaging", "التغليف"),
                "shippingTotal" => Label("ارسال", "Shipping", "الشحن"),
                "additionalServiceTotal" => Label("خدمات افزوده", "Additional services", "خدمات إضافية"),
                "subtotal" => Label("جمع کل", "Subtotal", "المجموع"),
                "discountTotal" => Label("تخفیف", "Discount", "الخصم"),
                "taxableTotal" => Label("مشمول مالیات", "Taxable", "خاضع للضريبة"),
                "taxTotal" => Label("مالیات", "Tax", "الضريبة"),
                "finalTotal" => Label("مبلغ نهایی", "Final total", "الإجمالي النهائي"),
                "taxRatePercent" => Label("نرخ مالیات (٪)", "Tax rate (%)", "نسبة الضريبة"),
                _ => prop.Name
            };
            if (prop.Name is "total" or "totalAmount" or "grandTotal" or "finalTotal")
            {
                TotalRow = (label, text);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Number)
            {
                QuoteRows.Add((label, text));
            }
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid orderId, Guid designId)
    {
        long version = await CurrentVersionAsync(orderId);
        string idemKey = $"submit-{orderId:N}";
        (bool ok, JsonElement? _, string? problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/submit", new { version }, idemKey);
        if (!ok)
        {
            return RedirectToPage(new { orderId, designId, error = problem ?? "submit-failed" });
        }
        return RedirectToPage("/Account/Order", new { id = orderId });
    }

    private async Task<long> CurrentVersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    private string T(string fa) => fa;
}
