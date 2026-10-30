using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// Order wizard step 2 — choose the shirt (product + variant + quantity),
// branch and shipping address, then create the order draft with the item.
public sealed class ShirtModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid AddToOrderId { get; set; }
    public Guid DesignId { get; private set; }
    public string DesignTitle { get; private set; } = "";
    public string DesignCode { get; private set; } = "";
    public string? DesignPreview { get; private set; }
    public bool DesignFound { get; private set; }

    public IReadOnlyList<ProductRow> Products { get; private set; } = [];
    public IReadOnlyList<VariantRow> Variants { get; private set; } = [];
    public IReadOnlyList<AddressRow> Addresses { get; private set; } = [];
    public IReadOnlyList<BranchRow> Branches { get; private set; } = [];
    public bool HasProfile { get; private set; }
    public string? Error { get; private set; }
    // انتخاب‌های پاپ‌آپ «تیشرتت رو ببین» (از کوکی barang.preview)
    public string? PreviewShirtColor { get; private set; }
    public string? PreviewDesignColor { get; private set; }
    public bool HasPreview { get; private set; }
    public string ErrorText => Error switch
    {
        "variant" => "واریانت و تعداد را انتخاب کنید.",
        "branch" => "شعبه در دسترس نیست؛ بعداً تلاش کنید.",
        "customer.not_found" => "پروفایل مشتری یافت نشد.",
        "order.duplicate_external_reference" => "درخواست تکراری؛ چند لحظه بعد دوباره تلاش کنید.",
        _ => "ایجاد سفارش انجام نشد؛ دوباره تلاش کنید."
    };

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ProductId { get; set; }
    [BindProperty] public Guid? VariantId { get; set; }
    [BindProperty] public decimal Quantity { get; set; } = 1;
    [BindProperty] public Guid? BranchId { get; set; }
    [BindProperty] public Guid? AddressId { get; set; }
    [BindProperty] public string? Notes { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid designId, [FromQuery] Guid? product, string? error)
    {
        Error = error;
        DesignId = designId;
        await LoadDesignAsync();
        if (!DesignFound) return NotFound();
        (HasPreview, PreviewShirtColor, PreviewDesignColor) = ReadPreviewCookie(designId);

        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid))
        {
            return RedirectToPage("/Account/Index", new { error = "noprofile" });
        }
        HasProfile = true;
        string customerId = cid.GetString() ?? "";

        if (ProductId is null && Products.Count > 0) ProductId = Products[0].Id;

        JsonElement? products = await core.GetAsync("api/v1/client/catalog/products?page=1&pageSize=50" + (string.IsNullOrWhiteSpace(Search) ? "" : $"&search={Uri.EscapeDataString(Search.Trim())}"));
        if (products is JsonElement p)
        {
            JsonElement items = p.ValueKind == JsonValueKind.Array
                ? p
                : p.TryGetProperty("items", out JsonElement pi) && pi.ValueKind == JsonValueKind.Array ? pi : default;
            if (items.ValueKind == JsonValueKind.Array)
            {
                Products = items.EnumerateArray().Select(x => new ProductRow(
                    x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid pid) ? pid : Guid.Empty,
                    x.TryGetProperty("name", out JsonElement nm) ? nm.GetString() ?? "" : "",
                    x.TryGetProperty("productCode", out JsonElement pc) ? pc.GetString() ?? "" : "")).ToList();
                ProductId ??= Products[0].Id;
            }
        }

        if (ProductId is Guid selected)
        {
            JsonElement? variants = await core.GetAsync($"api/v1/client/catalog/products/{selected}/variants");
            if (variants is JsonElement v && v.ValueKind == JsonValueKind.Array)
            {
                Variants = v.EnumerateArray().Select(x => new VariantRow(
                    x.TryGetProperty("id", out JsonElement vid) && Guid.TryParse(vid.GetString(), out Guid gid) ? gid : Guid.Empty,
                    x.TryGetProperty("sku", out JsonElement sku) ? sku.GetString() ?? "" : "",
                    x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() : null,
                    x.TryGetProperty("sizeCode", out JsonElement sz) ? sz.GetString() : null,
                    x.TryGetProperty("colorCode", out JsonElement cl) ? cl.GetString() : null)).ToList();
                // اگر از پاپ‌آپ پیش‌نمایش رنگی انتخاب شده، هم‌رنگ‌ترین واریانت خودکار انتخاب شود
                if (HasPreview && !string.IsNullOrWhiteSpace(PreviewShirtColor) && VariantId is null && Variants.Count > 0)
                {
                    string color = PreviewShirtColor.Trim();
                    VariantRow? match = Variants.FirstOrDefault(x =>
                            (x.DisplayName ?? "").Contains(color, StringComparison.OrdinalIgnoreCase))
                        ?? Variants.FirstOrDefault(x => (x.ColorCode ?? "").Contains(color, StringComparison.OrdinalIgnoreCase));
                    if (match is not null) VariantId = match.Id;
                }
            }
        }

        JsonElement? addresses = await core.GetAsync($"api/v1/client/customers/{customerId}/addresses");
        if (addresses is JsonElement a && a.ValueKind == JsonValueKind.Array)
        {
            Addresses = a.EnumerateArray()
                .Where(x => x.TryGetProperty("isActive", out JsonElement act) && act.ValueKind == JsonValueKind.True)
                .Select(x => new AddressRow(
                    x.TryGetProperty("id", out JsonElement aid) && Guid.TryParse(aid.GetString(), out Guid g) ? g : Guid.Empty,
                    x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
                    x.TryGetProperty("isDefault", out JsonElement df) && df.ValueKind == JsonValueKind.True,
                    ComposeLine(x))).ToList();
        }

        JsonElement? branches = await core.GetAsync("api/v1/client/branches");
        if (branches is JsonElement b && b.ValueKind == JsonValueKind.Array)
        {
            Branches = b.EnumerateArray().Select(x => new BranchRow(
                x.TryGetProperty("id", out JsonElement bid) && Guid.TryParse(bid.GetString(), out Guid g) ? g : Guid.Empty,
                x.TryGetProperty("displayName", out JsonElement nm) ? nm.GetString() ?? "" : "")).ToList();
            if (Branches.Count == 1) BranchId ??= Branches[0].Id;
        }
        ViewData["Title"] = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() switch { "en" => "Choose a t-shirt · Peyrang Khan", "ar" => "اختر تيشيرت · بيرنگ خان", _ => "انتخاب تیشرت · پیرنگ خان" };
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid designId, Guid addToOrderId)
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid))
        {
            return RedirectToPage("/Account/Index", new { error = "noprofile" });
        }
        if (VariantId is null || Quantity <= 0)
        {
            return RedirectToPage(new { designId, search = Search, productId = ProductId, error = "variant" });
        }
        if (BranchId is null)
        {
            return RedirectToPage(new { designId, search = Search, productId = ProductId, error = "branch" });
        }

        string idempotencyKey = Guid.NewGuid().ToString("N");
        string today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        object? shippingAddress = null;
        if (AddressId is Guid addressId)
        {
            JsonElement? addresses = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/addresses");
            JsonElement? match = addresses is JsonElement arr && arr.ValueKind == JsonValueKind.Array
                ? arr.EnumerateArray().FirstOrDefault(x => x.TryGetProperty("id", out JsonElement xid) && Guid.TryParse(xid.GetString(), out Guid g) && g == addressId)
                : (JsonElement?)null;
            if (match is JsonElement m)
            {
                shippingAddress = new
                {
                    addressTypeCode = "shipping",
                    title = m.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "shipping" : "shipping",
                    countryCode = m.TryGetProperty("countryCode", out JsonElement co) ? co.GetString() ?? "IR" : "IR",
                    provinceCode = m.TryGetProperty("provinceCode", out JsonElement pr) ? pr.GetString() : null,
                    cityCode = m.TryGetProperty("cityCode", out JsonElement ci) ? ci.GetString() : null,
                    district = m.TryGetProperty("district", out JsonElement di) ? di.GetString() : null,
                    addressLine1 = m.TryGetProperty("addressLine1", out JsonElement l1) ? l1.GetString() ?? "" : "",
                    addressLine2 = m.TryGetProperty("addressLine2", out JsonElement l2) ? l2.GetString() : null,
                    postalCode = m.TryGetProperty("postalCode", out JsonElement po) ? po.GetString() : null,
                    recipientName = m.TryGetProperty("recipientName", out JsonElement rn) ? rn.GetString() : null,
                    recipientMobile = m.TryGetProperty("recipientMobile", out JsonElement rm) ? rm.GetString() : null,
                    isDefault = false,
                    displayOrder = 1,
                    version = 0
                };
            }
        }

        string designNote = $"طرح: {DesignCode}";
        object notes = string.IsNullOrWhiteSpace(Notes) ? designNote : $"{Notes} | {designNote}";
        // add to an existing draft order (multi-design orders) or create a new one
        Guid orderId;
        if (AddToOrderId is Guid draftId && draftId != Guid.Empty)
        {
            var existing = await core.GetAsync($"api/v1/client/orders/{draftId}");
            if (existing is JsonElement ex && ex.TryGetProperty("id", out JsonElement exId)
                && ex.TryGetProperty("status", out JsonElement exSt) && string.Equals(exSt.GetString(), "Draft"))
            {
                orderId = exId.GetGuid();
                // refresh shipping address on the draft so the new item ships to the same place
                if (shippingAddress is not null)
                {
                    long exVersion = ex.TryGetProperty("version", out JsonElement ev) && ev.TryGetInt64(out long evn) ? evn : 0;
                    await core.PutRawAsync($"api/v1/client/orders/{draftId}", new
                    {
                        branchId = BranchId,
                        shippingAddress,
                        version = exVersion
                    });
                }
            }
            else
            {
                return RedirectToPage(new { designId, search = Search, productId = ProductId, error = "draft-failed" });
            }
        }
        else
        {
            (bool ok, JsonElement? draft, string? problem) = await core.PostRawWithBodyAsync("api/v1/client/orders/drafts", new
            {
                branchId = BranchId,
                customerId = cid.GetString(),
                source = "Web",
                orderDate = today,
                shippingAddress
            }, idempotencyKey);
            if (!ok || draft is not JsonElement d || !d.TryGetProperty("id", out JsonElement did))
            {
                return RedirectToPage(new { designId, search = Search, productId = ProductId, error = problem ?? "draft-failed" });
            }
            orderId = did.GetGuid();
        }
        long version = await CurrentVersionAsync(orderId);

        (bool itemOk, JsonElement? item, string? itemProblem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/items", new
        {
            productId = ProductId,
            productVariantId = VariantId,
            quantity = Quantity,
            notes,
            version
        }, idempotencyKey);
        if (!itemOk)
        {
            await core.PostRawAsync($"api/v1/client/orders/{orderId}/cancel", new { reasonCode = "customer-change", reasonText = "ایجاد سفارش ناتمام", version = await CurrentVersionAsync(orderId) });
            return RedirectToPage(new { designId, search = Search, productId = ProductId, error = itemProblem ?? "item-failed" });
        }

        return RedirectToPage("Print", new { orderId, designId });
    }

    private async Task<long> CurrentVersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    private async Task LoadDesignAsync()
    {
        JsonElement? result = await core.GetAsync($"api/v1/public/catalog/{DesignId}");
        if (result is JsonElement x && x.ValueKind == JsonValueKind.Object)
        {
            DesignFound = true;
            DesignTitle = x.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : "";
            DesignCode = x.TryGetProperty("code", out JsonElement cd) ? cd.GetString() ?? "" : "";
            DesignPreview = x.TryGetProperty("previewUrl", out JsonElement th) ? th.GetString() : null;
        }
    }

    private static string ComposeLine(JsonElement x)    {
        string[] parts =
        {
            Prop(x, "district"), Prop(x, "addressLine1"), Prop(x, "addressLine2"),
            Prop(x, "recipientName"), Prop(x, "recipientMobile")
        };
        return string.Join("، ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
    private static string Prop(JsonElement x, string name) => x.TryGetProperty(name, out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

    // خواندن کوکی پیش‌نمایش «تیشرتت رو ببین»؛ فقط وقتی با طرح جاری یکسان است معتبر است.
    private (bool Valid, string? ShirtColor, string? DesignColor) ReadPreviewCookie(Guid designId)
    {
        string? raw = Request.Cookies["barang.preview"];
        if (string.IsNullOrWhiteSpace(raw)) return (false, null, null);
        try
        {
            JsonElement x = JsonDocument.Parse(raw).RootElement;
            string? id = x.TryGetProperty("designId", out JsonElement di) ? di.GetString() : null;
            if (!Guid.TryParse(id, out Guid pid) || pid != designId) return (false, null, null);
            string? shirt = x.TryGetProperty("shirtColor", out JsonElement sc) ? sc.GetString() : null;
            string? design = x.TryGetProperty("designColor", out JsonElement dc) ? dc.GetString() : null;
            bool hasDesign = !string.IsNullOrWhiteSpace(design) && design != "رنگ واقعی طرح";
            return (true, shirt, hasDesign ? design : null);
        }
        catch (JsonException) { return (false, null, null); }
    }

    private string T(string fa, string en) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? en : fa;

    public sealed record ProductRow(Guid Id, string Name, string Code);
    public sealed record VariantRow(Guid Id, string Sku, string? DisplayName, string? SizeCode, string? ColorCode)
    {
        public string Label => string.Join(" · ", new[] { Sku, DisplayName, SizeCode, ColorCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
    public sealed record AddressRow(Guid Id, string Title, bool IsDefault, string Line)
    {
        public string Label => $"{Title} — {Line}";
    }
    public sealed record BranchRow(Guid Id, string Name);
}
