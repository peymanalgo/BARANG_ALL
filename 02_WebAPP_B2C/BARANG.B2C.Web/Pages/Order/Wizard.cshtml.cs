using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Antiforgery;

namespace BARANG.B2C.Web.Pages.Order;

// ویزارد جدید سفارش (۴ فاز) — رابط کاملاً سمت کلاینت است و این PageModel
// فقط نقش BFF دارد: داده و قیمت و سفارش همه از Core می‌آیند، هیچ چیز اینجا
// هاردکد نیست. شروع طراحی فقط برای مشتری واردشده ممکن است؛ در غیر این صورت
// همان اول به ورود/ثبت‌نام هدایت می‌شود (و بعد از ورود به همان ویزارد برمی‌گردد).
public sealed class WizardModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public Guid? EntryDesignId { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public IActionResult OnGet(Guid? design)
    {
        EntryDesignId = design;
        IsAuthenticated = User.Identity?.IsAuthenticated == true;
        if (!IsAuthenticated)
        {
            // گیت ورود در شروع طراحی: طرح ورودی را هم در returnUrl نگه می‌داریم
            string url = "/order/wizard" + (design is Guid id ? "?design=" + id : "");
            return Redirect("/account/login?returnUrl=" + Uri.EscapeDataString(url));
        }
        return Page();
    }

    // ── پیکربندی ویزارد: محصولات/قیمت/اندازه چاپ/بسته‌بندی از Core ──
    public async Task<IActionResult> OnGetConfigAsync()
    {
        JsonElement? config = await core.GetAsync($"api/v1/public/wizard/config?culture={Culture()}");
        return new JsonResult(config);
    }

    // ── جستجوی مفهومی طرح‌ها (بدون عبارت: جدیدترین‌های کاتالوگ عمومی) ──
    public async Task<IActionResult> OnGetConceptAsync(string q, int limit = 24)
    {
        int size = Math.Clamp(limit, 1, 60);
        string term = (q ?? "").Trim();
        if (term.Length == 0)
        {
            JsonElement? page = await core.GetAsync($"api/v1/public/catalog?page=1&pageSize={size}&culture={Culture()}");
            if (page is JsonElement p && p.ValueKind == JsonValueKind.Object && p.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
                return new JsonResult(items);
            return new JsonResult(Array.Empty<object>());
        }
        JsonElement? result = await core.GetAsync($"api/v1/public/catalog/concept-search?q={Uri.EscapeDataString(term)}&limit={size}&culture={Culture()}");
        return new JsonResult(result);
    }

    // ── جزئیات یک طرح (ورود از صفحه نمایش طرح) ──
    public async Task<IActionResult> OnGetDesignAsync(Guid id)
    {
        JsonElement? result = await core.GetAsync($"api/v1/public/catalog/{id}?culture={Culture()}");
        return new JsonResult(result);
    }

    // ── پیش‌فاکتور لحظه‌ای (سمت سرور محاسبه می‌شود) ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostQuoteAsync([FromBody] JsonElement body)
    {
        (bool ok, JsonElement? value, string? problem) = await core.PostRawWithBodyAsync("api/v1/public/wizard/quote", body);
        return new JsonResult(new { ok, quote = value, problem });
    }

    // ── آدرس‌های مشتری برای فاز ۲ ──
    public async Task<IActionResult> OnGetAddressesAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid))
            return new JsonResult(new { ok = false, problem = "auth" });
        JsonElement? addresses = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/addresses");
        if (addresses is not JsonElement list || list.ValueKind != JsonValueKind.Array)
            return new JsonResult(new { ok = false, problem = "unavailable" });
        var rows = list.EnumerateArray().Where(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False).Select(x => new
        {
            id = Text(x, "id"),
            title = Text(x, "title"),
            line = string.Join("، ", new[] { Text(x, "province"), Text(x, "city"), Text(x, "district"), Text(x, "addressLine1"), Text(x, "addressLine2"), Text(x, "postalCode") }.Where(s => s.Length > 0)),
            isDefault = x.TryGetProperty("isDefault", out JsonElement d) && d.ValueKind == JsonValueKind.True,
        }).ToList();
        return new JsonResult(new { ok = true, addresses = rows });
    }

    // ── موجودی کیف پول برای فاز ۴ ──
    public async Task<IActionResult> OnGetWalletAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid))
            return new JsonResult(new { ok = false, problem = "auth" });
        JsonElement? wallet = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/wallet");
        return new JsonResult(new
        {
            ok = wallet is JsonElement w && w.ValueKind == JsonValueKind.Object,
            balance = wallet is JsonElement wb && wb.TryGetProperty("balance", out JsonElement b) && b.TryGetDecimal(out decimal amount) ? amount : 0m,
        });
    }

    // ── ساخت سفارش (فاز ۳): پیش‌نمایش قیمت قطعی سمت سرور ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid))
            return new JsonResult(new { ok = false, problem = "auth" });
        string customerId = cid.GetString() ?? "";

        JsonElement state;
        try
        {
            if (Request.HasFormContentType)
            {
                // حالت multipart: فایل طرح شخصی + state به‌صورت فیلد متنی
                state = JsonDocument.Parse(Request.Form["state"].ToString()).RootElement;
            }
            else
            {
                using StreamReader reader = new(Request.Body);
                state = JsonDocument.Parse(await reader.ReadToEndAsync()).RootElement;
            }
        }
        catch (JsonException) { return new JsonResult(new { ok = false, problem = "invalid" }); }

        if (core.SawUnauthorized) return new JsonResult(new { ok = false, problem = "auth" });

        // سفارش قبلی دیگر کنسل نمی‌شود: هر checkout یک قلم سبد خرید (Draft) است
        // و کاربر می‌تواند چند پیش‌نویس جمع کند و در /cart یکجا پرداخت کند.

        string mode = Text(state, "mode");
        Guid variantId = Guid.TryParse(Text(state, "productVariantId"), out Guid v) ? v : Guid.Empty;
        string printSize = Text(state, "printSizeCode");
        string position = Text(state, "printPosition");
        int quantity = state.TryGetProperty("quantity", out JsonElement q) && q.TryGetInt32(out int n) && n > 0 ? Math.Min(n, 50) : 1;
        if (variantId == Guid.Empty || printSize.Length == 0) return new JsonResult(new { ok = false, problem = "invalid" });

        // ۱) فایل طرح شخصی (اگر مسیر آپلود است) — پیش از هر چیز آپلود می‌شود
        string? fileVersionId = null;
        if (mode == "upload" && Request.HasFormContentType)
        {
            IFormFile? file = Request.Form.Files.GetFiles("file").FirstOrDefault();
            if (file is { Length: > 0 })
            {
                (bool upOk, JsonElement? up, string? upProblem) = await core.PostMultipartAsync(
                    "api/v1/client/files/upload", file.FileName, file.ContentType, file.OpenReadStream(), file.Length, $"wizard-{Guid.NewGuid():N}");
                if (upOk && up is JsonElement uv && uv.TryGetProperty("fileVersionId", out JsonElement fvid)) fileVersionId = fvid.GetString();
                else return new JsonResult(new { ok = false, problem = upProblem ?? "upload-failed" });
            }
            else return new JsonResult(new { ok = false, problem = "file-required" });
        }

        // ۲) طرح بانکی → نسخه فعلی طرح برای عملیات چاپ
        Guid? artworkVersionId = null;
        string designCode = Text(state, "designCode");
        if (mode == "bank" && Guid.TryParse(Text(state, "designId"), out Guid designId))
        {
            JsonElement? design = await core.GetAsync($"api/v1/public/catalog/{designId}");
            if (design is JsonElement d && d.TryGetProperty("artworkVersionId", out JsonElement av) && Guid.TryParse(av.GetString(), out Guid a)) artworkVersionId = a;
        }

        // ۳) آدرس ارسال (انتخابی یا جدید) + مشخصات گیرنده هدیه
        JsonElement? addresses = await core.GetAsync($"api/v1/client/customers/{customerId}/addresses");
        JsonElement? selected = null;
        bool isNewAddress = state.TryGetProperty("newAddress", out JsonElement na) && na.ValueKind == JsonValueKind.Object;
        if (isNewAddress)
        {
            string line = Text(na, "addressLine1");
            string city = Text(na, "city");
            string fullLine = city.Length > 0 ? city + "، " + line : line;
            (bool created, JsonElement? made, string? createProblem) = await core.PostRawWithBodyAsync($"api/v1/client/customers/{customerId}/addresses", new
            {
                addressTypeCode = "shipping",
                title = Text(na, "title") is { Length: > 0 } t ? t : (city.Length > 0 ? city : "نشانی ویزارد"),
                countryCode = "IR",
                cityCode = (string?)null,
                addressLine1 = fullLine,
                postalCode = Text(na, "postalCode"),
                isDefault = false,
                displayOrder = 1,
                version = 0,
            });
            if (created) addresses = await core.GetAsync($"api/v1/client/customers/{customerId}/addresses");
            else return new JsonResult(new { ok = false, problem = createProblem ?? "address-failed" });
        }
        if (addresses is JsonElement list2 && list2.ValueKind == JsonValueKind.Array)
        {
            string? wantedId = Text(state, "addressId");
            List<JsonElement> active = list2.EnumerateArray()
                .Where(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False)
                .ToList();
            JsonElement match = isNewAddress
                ? active.FirstOrDefault(x => x.TryGetProperty("addressLine1", out JsonElement l1) && Text(na, "addressLine1").Length > 0 && (l1.GetString() ?? "").Contains(Text(na, "addressLine1")))
                : active.FirstOrDefault(x => x.TryGetProperty("id", out JsonElement xid) && xid.GetString() == wantedId);
            if (match.ValueKind == JsonValueKind.Object) selected = match;
            if (selected is null)
            {
                JsonElement fallback = active.FirstOrDefault(x => x.TryGetProperty("isDefault", out JsonElement df) && df.ValueKind == JsonValueKind.True);
                if (fallback.ValueKind == JsonValueKind.Object) selected = fallback;
            }
        }
        if (selected is null && !isNewAddress)
            return new JsonResult(new { ok = false, problem = "address-required" });
        // موتور سفارش «نام گیرنده» را اجباری می‌کند؛ از مشخصات مشتری تکمیل می‌شود
        string customerName = string.Join(" ", new[] { Text(c, "firstName"), Text(c, "lastName") }.Where(x => x.Length > 0));
        if (customerName.Length == 0) customerName = User.Identity?.Name ?? "مشتری";
        string RecipientOf(JsonElement a) => Text(a, "recipientName") is { Length: > 0 } rn ? rn : customerName;
        string baseAddressLine = selected is JsonElement s ? Text(s, "addressLine1") : Text(na, "addressLine1");
        string? baseCity = selected is JsonElement s2 ? NullIfEmpty(Text(s2, "cityCode")) : NullIfEmpty(Text(na, "city"));
        string? basePostal = selected is JsonElement s3 ? NullIfEmpty(Text(s3, "postalCode")) : NullIfEmpty(Text(na, "postalCode"));
        string baseRecipient = selected is JsonElement s4 ? RecipientOf(s4) : customerName;
        string basePhone = selected is JsonElement s5 ? Text(s5, "recipientMobile") : "";
        object? shippingAddress = selected is JsonElement sv ? new
        {
            addressTypeCode = "shipping",
            title = Text(sv, "title"),
            countryCode = Text(sv, "countryCode") is { Length: > 0 } cc ? cc : "IR",
            provinceCode = Text(sv, "provinceCode"),
            cityCode = Text(sv, "cityCode"),
            district = Text(sv, "district"),
            addressLine1 = Text(sv, "addressLine1"),
            addressLine2 = Text(sv, "addressLine2"),
            postalCode = Text(sv, "postalCode"),
            recipientName = RecipientOf(sv),
            recipientMobile = Text(sv, "recipientMobile"),
            isDefault = false,
            displayOrder = 1,
            version = 0,
        } : new
        {
            addressTypeCode = "shipping",
            title = "نشانی ویزارد",
            countryCode = "IR",
            provinceCode = (string?)null,
            cityCode = baseCity,
            district = (string?)null,
            addressLine1 = Text(na, "addressLine1"),
            addressLine2 = (string?)null,
            postalCode = basePostal,
            recipientName = customerName,
            recipientMobile = basePhone,
            isDefault = false,
            displayOrder = 1,
            version = 0,
        };

        // گیرنده هدیه: نام/شماره/نشانی جایگزین ارسال و پیام روی کارت هدیه → یادداشت سفارش
        List<string> noteParts = [];
        if (state.TryGetProperty("gift", out JsonElement gift) && gift.ValueKind == JsonValueKind.Object && Text(gift, "name").Length > 0)
        {
            string giftAddress = Text(gift, "address");
            shippingAddress = new
            {
                addressTypeCode = "shipping",
                title = "هدیه",
                countryCode = "IR",
                provinceCode = (string?)null,
                cityCode = baseCity,
                district = (string?)null,
                addressLine1 = giftAddress.Length > 0 ? giftAddress : baseAddressLine,
                addressLine2 = (string?)null,
                postalCode = basePostal,
                recipientName = Text(gift, "name"),
                recipientMobile = Text(gift, "phone"),
                isDefault = false,
                displayOrder = 1,
                version = 0,
            };
            noteParts.Add($"ارسال برای دیگران: {Text(gift, "name")}");
            if (Text(gift, "message").Length > 0) noteParts.Add($"پیام کارت هدیه: {Text(gift, "message")}");
        }
        if (designCode.Length > 0) noteParts.Add($"طرح: {designCode}");
        if (Text(state, "shirtColorName").Length > 0) noteParts.Add($"رنگ تیشرت: {Text(state, "shirtColorName")}");
        if (mode == "text")
        {
            noteParts.Add($"متن طرح: {Text(state, "text")}");
            noteParts.Add($"فونت {Text(state, "font")} · اندازه {Text(state, "textSize")}% · رنگ {Text(state, "textColor")}");
        }

        // ۴) شعبه اجرا (اولین شعبه فعال)
        JsonElement? branches = await core.GetAsync("api/v1/client/branches");
        Guid branchId = Guid.Empty;
        if (branches is JsonElement br && br.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement b in br.EnumerateArray())
            {
                if (b.TryGetProperty("id", out JsonElement bid) && Guid.TryParse(bid.GetString(), out Guid bg)) { branchId = bg; break; }
            }
        }

        // ۵) سفارش پیش‌نویس + قلم + عملیات چاپ
        string idem = Guid.NewGuid().ToString("N");
        (bool draftOk, JsonElement? draft, string? draftProblem) = await core.PostRawWithBodyAsync("api/v1/client/orders/drafts", new
        {
            branchId,
            customerId,
            source = "Web",
            orderDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            shippingAddress,
        }, idem);
        if (!draftOk || draft is not JsonElement d2 || !d2.TryGetProperty("id", out JsonElement oid))
            return new JsonResult(new { ok = false, problem = draftProblem ?? "draft-failed" });
        Guid orderId = oid.GetGuid();
        long version = await VersionAsync(orderId);

        (bool itemOk, JsonElement? item, string? itemProblem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/items", new
        {
            productId = Text(state, "productId") is { Length: > 0 } pid && Guid.TryParse(pid, out Guid p) ? (Guid?)p : null,
            productVariantId = variantId,
            quantity,
            notes = string.Join(" | ", noteParts),
            version,
        }, idem);
        if (!itemOk) { await CancelAsync(orderId); return new JsonResult(new { ok = false, problem = itemProblem ?? "item-failed" }); }
        Guid itemId = item is JsonElement it && it.TryGetProperty("id", out JsonElement iid) ? iid.GetGuid() : Guid.Empty;
        version = await VersionAsync(orderId);

        decimal width = PrintSizeCm(printSize, out decimal height);
        (bool printOk, JsonElement? _, string? printProblem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/items/{itemId}/print-operations", new
        {
            sequence = 1,
            printPositionCode = position,
            printMethodCode = printSize,
            width,
            height,
            artworkVersionId,
            fileVersionId,
            notes = $"اندازه چاپ {printSize} · {Text(state, "printSizeName")}",
            version,
        });
        if (!printOk) { await CancelAsync(orderId); return new JsonResult(new { ok = false, problem = printProblem ?? "print-failed" }); }
        version = await VersionAsync(orderId);

        // ۶) پروفایل کسب‌وکاری سفارش: نوع بسته‌بندی + یادداشت‌ها
        await core.PutRawAsync($"api/v1/client/orders/{orderId}/business-profile", new
        {
            shippingMethodCode = (string?)null,
            packagingTypeCode = Text(state, "packagingTypeCode") is { Length: > 0 } pk ? pk : null,
            includeInvoiceInPackage = false,
            orderTypeCode = (string?)null,
            notes = string.Join(" | ", noteParts),
            version,
        });

        // ۷) قیمت‌گذاری سرور (همان عددی که در پرداخت قطعی می‌شود)
        version = await VersionAsync(orderId);
        (bool priceOk, JsonElement? pricing, string? priceProblem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/pricing/preview", new { version });
        if (!priceOk) { await CancelAsync(orderId); return new JsonResult(new { ok = false, problem = priceProblem ?? "pricing-failed" }); }

        return new JsonResult(new { ok = true, orderId, pricing, designCode });
    }

    // ── پرداخت (فاز ۴): کیف پول واقعی؛ درگاه فعلاً ماکِ همیشه‌موفق ──
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostPayAsync([FromBody] JsonElement body)
    {
        if (!Guid.TryParse(Text(body, "orderId"), out Guid orderId) || orderId == Guid.Empty)
            return new JsonResult(new { ok = false, problem = "invalid" });
        string method = Text(body, "method") == "wallet" ? "wallet" : "gateway";
        long version = await VersionAsync(orderId);

        if (method == "wallet")
        {
            (bool ok, JsonElement? value, string? problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-wallet", new { version }, $"wallet-{orderId:N}");
            if (ok) return new JsonResult(new { ok = true, paidWith = "wallet", order = value });
            if (problem is not null && problem.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
            {
                // موجودی کافی نیست → همان مسیر درگاه ماک ادامه می‌دهد (در این نسخه همیشه موفق)
                (bool mockOk, JsonElement? mockValue, string? _) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-gateway-mock", new { version = await VersionAsync(orderId) }, $"gw-{orderId:N}");
                if (mockOk) return new JsonResult(new { ok = true, paidWith = "gateway", order = mockValue });
            }
            return new JsonResult(new { ok = false, problem = problem ?? "payment-failed" });
        }

        (bool gwOk, JsonElement? gwValue, string? gwProblem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/checkout-gateway-mock", new { version }, $"gw-{orderId:N}");
        return gwOk
            ? new JsonResult(new { ok = true, paidWith = "gateway", order = gwValue })
            : new JsonResult(new { ok = false, problem = gwProblem ?? "payment-failed" });
    }

    private async Task CancelAsync(Guid orderId)
    {
        long v = await VersionAsync(orderId);
        await core.PostRawAsync($"api/v1/client/orders/{orderId}/cancel", new { reasonCode = "customer-change", reasonText = "ویزارد ناتمام", version = v });
    }

    private async Task<long> VersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    // ابعاد سانتی‌متری استاندارد کاغذ A-series برای عملیات چاپ (قیمت از تعرفه سرور می‌آید، این فقط ابعاد فیزیکی است)
    private static decimal PrintSizeCm(string code, out decimal height) => code.ToUpperInvariant() switch
    {
        "A3" => Parse("30", out height, "42"),
        "A4" => Parse("21", out height, "29.7"),
        "A5" => Parse("14", out height, "21"),
        _ => Parse("21", out height, "29.7"),
    };
    private static decimal Parse(string w, out decimal height, string h)
    {
        height = decimal.Parse(h, System.Globalization.CultureInfo.InvariantCulture);
        return decimal.Parse(w, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Text(JsonElement x, string name) => x.ValueKind == JsonValueKind.Object && x.TryGetProperty(name, out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
    private string Culture() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}
