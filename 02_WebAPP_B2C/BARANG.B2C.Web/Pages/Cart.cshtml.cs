using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages;

// سبد خرید = سفارش‌های «پیش‌نویس» (Draft) مشتری. هر تایید در ویزارد یک قلم
// سبد می‌سازد؛ این صفحه اقلام را فهرست می‌کند، حذف قلم (کنسل پیش‌نویس) و
// پرداخت یکجای همه اقلام را از طریق Core انجام می‌دهد (الگوی BFF ویزارد).
public sealed class CartModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public bool HasProfile { get; private set; }
    public IReadOnlyList<CartRow> Items { get; private set; } = [];
    public decimal GrandTotal { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login?returnUrl=%2Fcart");
        List<JsonElement> drafts = await DraftsAsync();
        if (core.SawUnauthorized) return Redirect("/account/login?returnUrl=%2Fcart");
        HasProfile = drafts.Count > 0 || await HasCustomerProfileAsync();
        // اسنپ‌شات محصول فقط هنگام ثبت نهایی پر می‌شود؛ برای پیش‌نویس‌ها نام‌ها از کاتالوگ ویزارد می‌آید
        (Dictionary<Guid, (string Product, string Variant)> variants, Dictionary<string, string> packagingNames) = await WizardCatalogAsync();
        List<CartRow> rows = [];
        foreach (JsonElement d in drafts)
        {
            if (!d.TryGetProperty("id", out JsonElement id) || !Guid.TryParse(id.GetString(), out Guid orderId)) continue;
            JsonElement? detail = await core.GetAsync($"api/v1/client/orders/{orderId}");
            (string title, string product, string variant, int quantity, string printSize, Guid? variantId) = DescribeItem(detail);
            if (variantId is Guid vid && variants.TryGetValue(vid, out (string Product, string Variant) cat))
            {
                if (product.Length == 0) product = cat.Product;
                if (variant.Length == 0) variant = cat.Variant;
            }
            if (variant.StartsWith(product, StringComparison.Ordinal) && product.Length > 0) product = ""; // نام واریانت خودش نام محصول را دارد
            string packagingCode = await PackagingAsync(orderId);
            string packaging = packagingNames.TryGetValue(packagingCode, out string pname) ? pname : packagingCode;
            decimal amount = await FinalTotalAsync(orderId);
            rows.Add(new CartRow(orderId, title, product, variant, quantity, printSize, packaging, amount));
        }
        Items = rows;
        GrandTotal = rows.Sum(x => x.Amount);
        return Page();
    }

    // ── خلاصه برای بج/دراپ‌داون هدر: سبک نگه داشته می‌شود (حداکثر ۴ قلم جزئیات) ──
    public async Task<IActionResult> OnGetSummaryAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return new JsonResult(new { ok = false, problem = "auth" });
        List<JsonElement> drafts = await DraftsAsync();
        if (core.SawUnauthorized) return new JsonResult(new { ok = false, problem = "auth" });
        List<object> preview = [];
        foreach (JsonElement d in drafts.Take(4))
        {
            if (!d.TryGetProperty("id", out JsonElement id) || !Guid.TryParse(id.GetString(), out Guid orderId)) continue;
            JsonElement? detail = await core.GetAsync($"api/v1/client/orders/{orderId}");
            (string title, _, _, _, _, _) = DescribeItem(detail);
            preview.Add(new { title, amount = await FinalTotalAsync(orderId), thumbnailUrl = (string?)null });
        }
        return new JsonResult(new { ok = true, count = drafts.Count, items = preview });
    }

    // ── حذف یک قلم سبد = کنسل سفارش پیش‌نویس ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostRemoveItemAsync([FromBody] JsonElement body)
    {
        if (User.Identity?.IsAuthenticated != true) return new JsonResult(new { ok = false, problem = "auth" });
        if (!Guid.TryParse(Text(body, "orderId"), out Guid orderId) || orderId == Guid.Empty)
            return new JsonResult(new { ok = false, problem = "invalid" });
        long version = await VersionAsync(orderId);
        (bool ok, _, string? problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/cancel",
            new { reasonCode = "customer-change", reasonText = "حذف از سبد خرید", version });
        return new JsonResult(new { ok, problem });
    }

    // ── پرداخت همه: پشت‌سرهم برای هر پیش‌نویس؛ کیف پول با بازگشت به درگاه ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostPayAllAsync([FromBody] JsonElement body)
    {
        if (User.Identity?.IsAuthenticated != true) return new JsonResult(new { ok = false, problem = "auth" });
        string method = Text(body, "method") == "wallet" ? "wallet" : "gateway";
        List<JsonElement> drafts = await DraftsAsync();
        if (core.SawUnauthorized) return new JsonResult(new { ok = false, problem = "auth" });
        List<object> paid = [], failed = [], cleaned = [];
        foreach (JsonElement d in Enumerable.Reverse(drafts)) // قدیمی‌ها اول پرداخت می‌شوند
        {
            if (!d.TryGetProperty("id", out JsonElement id) || !Guid.TryParse(id.GetString(), out Guid orderId)) continue;
            // پیش‌نویس بدون قلم قابل پرداخت نیست (باقی‌مانده ویزارد ناتمام) → از سبد پاک می‌شود
            JsonElement? detail = await core.GetAsync($"api/v1/client/orders/{orderId}");
            if (!HasActiveItem(detail))
            {
                long cv = detail is JsonElement dv && dv.TryGetProperty("version", out JsonElement vvv) && vvv.TryGetInt64(out long vn) ? vn : 0;
                (bool cok, _, _) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/cancel",
                    new { reasonCode = "customer-change", reasonText = "پاک‌سازی پیش‌نویس خالی", version = cv });
                if (cok) cleaned.Add(new { orderId });
                else failed.Add(new { orderId, problem = "empty-draft" });
                continue;
            }
            long version = await VersionAsync(orderId);
            string paidWith = method;
            (bool ok, JsonElement? value, string? problem) = method == "wallet"
                ? await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-wallet", new { version }, $"wallet-{orderId:N}")
                : await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-gateway-mock", new { version }, $"gw-{orderId:N}");
            if (!ok && method == "wallet" && problem is not null && problem.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
            {
                // موجودی کیف پول کافی نیست → همان قلم با درگاه ماک ادامه می‌دهد
                paidWith = "gateway";
                version = await VersionAsync(orderId);
                (ok, value, problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-gateway-mock", new { version }, $"gw-{orderId:N}");
            }
            string? number = value is JsonElement v && v.TryGetProperty("order", out JsonElement inner)
                && inner.ValueKind == JsonValueKind.Object && inner.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
            if (ok) paid.Add(new { orderId, orderNumber = number, paidWith });
            else failed.Add(new { orderId, problem = problem ?? "payment-failed" });
        }
        return new JsonResult(new { ok = failed.Count == 0, paid, cleaned, failed });
    }

    // ── سازوکارها ──
    private static bool HasActiveItem(JsonElement? detail) => detail is JsonElement o
        && o.ValueKind == JsonValueKind.Object
        && o.TryGetProperty("items", out JsonElement items)
        && items.ValueKind == JsonValueKind.Array
        && items.EnumerateArray().Any(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False);

    private async Task<bool> HasCustomerProfileAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        return customer is JsonElement c && c.ValueKind == JsonValueKind.Object && c.TryGetProperty("id", out _);
    }

    private async Task<List<JsonElement>> DraftsAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return [];
        JsonElement? result = await core.GetAsync($"api/v1/client/orders?customerId={cid.GetString()}&status=Draft&page=1&pageSize=100");
        if (result is not JsonElement el || el.ValueKind != JsonValueKind.Object) return [];
        if (!el.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array) return [];
        return items.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("status", out JsonElement st) && st.GetString() == "Draft")
            .ToList();
    }

    private static (string Title, string Product, string Variant, int Quantity, string PrintSize, Guid? VariantId) DescribeItem(JsonElement? detail)
    {
        if (detail is not JsonElement o || o.ValueKind != JsonValueKind.Object) return ("—", "", "", 1, "", null);
        JsonElement first = o.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array
            ? items.EnumerateArray().FirstOrDefault(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False)
            : default;
        JsonElement snap = first.ValueKind == JsonValueKind.Object && first.TryGetProperty("snapshot", out JsonElement sn) && sn.ValueKind == JsonValueKind.Object ? sn : default;
        string product = Text(snap, "productName").Length > 0 ? Text(snap, "productName") : Text(snap, "productCode");
        string variantDisplay = Text(snap, "variantDisplayName").Length > 0 ? Text(snap, "variantDisplayName") : Text(snap, "variantSku");
        string variant = string.Join(" · ", new[] { Text(snap, "sizeCode"), Text(snap, "colorCode") }.Where(s => s.Length > 0));
        if (variant.Length == 0) variant = variantDisplay;
        Guid? variantId = first.ValueKind == JsonValueKind.Object && first.TryGetProperty("productVariantId", out JsonElement pv) && Guid.TryParse(pv.GetString(), out Guid pvid) ? pvid : null;
        int quantity = first.ValueKind == JsonValueKind.Object && first.TryGetProperty("quantity", out JsonElement q) && q.TryGetInt32(out int n) && n > 0 ? n : 1;
        string printSize = "";
        if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("printOperations", out JsonElement ops) && ops.ValueKind == JsonValueKind.Array)
        {
            JsonElement op = ops.EnumerateArray().FirstOrDefault(p => !p.TryGetProperty("isActive", out JsonElement pact) || pact.ValueKind != JsonValueKind.False);
            if (op.ValueKind == JsonValueKind.Object) printSize = Text(op, "printMethodCode");
        }
        // عنوان قلم: کد طرح یا متن طرح از یادداشت‌های قلم (همان ساختار ویزارد)
        string title = "";
        string notes = first.ValueKind == JsonValueKind.Object ? Text(first, "notes") : "";
        foreach (string part in notes.Split(" | ", StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("طرح: ", StringComparison.Ordinal)) title = part["طرح: ".Length..];
            else if (part.StartsWith("متن طرح: ", StringComparison.Ordinal)) title = "«" + part["متن طرح: ".Length..].Trim() + "»";
        }
        if (string.IsNullOrWhiteSpace(title)) title = product.Length > 0 ? product : "—";
        return (title, product, variant, quantity, printSize, variantId);
    }

    // کاتالوگ عمومی ویزارد: نام محصول/واریانت و بسته‌بندی برای زبان فعلی
    private async Task<(Dictionary<Guid, (string Product, string Variant)>, Dictionary<string, string>)> WizardCatalogAsync()
    {
        Dictionary<Guid, (string, string)> variants = [];
        Dictionary<string, string> packaging = [];
        try
        {
            JsonElement? cfg = await core.GetAsync($"api/v1/public/wizard/config?culture={Culture()}");
            if (cfg is JsonElement c && c.ValueKind == JsonValueKind.Object)
            {
                if (c.TryGetProperty("products", out JsonElement products) && products.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement p in products.EnumerateArray())
                    {
                        string pname = Text(p, "name").Length > 0 ? Text(p, "name") : Text(p, "code");
                        if (!p.TryGetProperty("variants", out JsonElement vs) || vs.ValueKind != JsonValueKind.Array) continue;
                        foreach (JsonElement v in vs.EnumerateArray())
                        {
                            if (!v.TryGetProperty("id", out JsonElement vid) || !Guid.TryParse(vid.GetString(), out Guid vguid)) continue;
                            string desc = Text(v, "displayName").Length > 0 ? Text(v, "displayName")
                                : string.Join(" · ", new[] { Text(v, "sizeCode"), Text(v, "colorCode") }.Where(s => s.Length > 0));
                            variants[vguid] = (pname, desc);
                        }
                    }
                }
                if (c.TryGetProperty("packaging", out JsonElement packs) && packs.ValueKind == JsonValueKind.Array)
                    foreach (JsonElement pk in packs.EnumerateArray())
                        if (Text(pk, "code").Length > 0) packaging[Text(pk, "code")] = Text(pk, "name");
            }
        }
        catch { /* کاتالوگ در دسترس نیست — متا سبد ساده می‌ماند */ }
        return (variants, packaging);
    }

    private string Culture() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private async Task<string> PackagingAsync(Guid orderId)
    {
        JsonElement? profile = await core.GetAsync($"api/v1/client/orders/{orderId}/business-profile");
        return profile is JsonElement p && p.ValueKind == JsonValueKind.Object ? Text(p, "packagingTypeCode") : "";
    }

    private async Task<decimal> FinalTotalAsync(Guid orderId)
    {
        long version = await VersionAsync(orderId);
        (bool ok, JsonElement? value, _) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/pricing/preview", new { version });
        return ok && value is JsonElement v && v.TryGetProperty("finalTotal", out JsonElement f) && f.TryGetDecimal(out decimal d) ? d : 0m;
    }

    private async Task<long> VersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    private static string Text(JsonElement x, string name) => x.ValueKind == JsonValueKind.Object && x.TryGetProperty(name, out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

    public sealed record CartRow(Guid OrderId, string Title, string Product, string Variant, int Quantity, string PrintSize, string Packaging, decimal Amount);
}
