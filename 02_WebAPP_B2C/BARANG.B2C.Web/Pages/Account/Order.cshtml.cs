using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class OrderModel(CoreApiClient core) : PageModel
{
    public Guid Id { get; private set; }
    public string? OrderNumber { get; private set; }
    public string OrderStatus { get; private set; } = "";
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateOnly? OrderDate { get; private set; }
    public DateOnly? RequestedDelivery { get; private set; }
    public long Version { get; private set; }
    public OrderAddressRow? ShippingAddress { get; private set; }
    public IReadOnlyList<ItemRow> Items { get; private set; } = [];
    public IReadOnlyList<ShipmentRow> Shipments { get; private set; } = [];
    public IReadOnlyList<TimelineRow> Timeline { get; private set; } = [];
    public JsonElement? Quote { get; private set; }
    public int Stage { get; private set; } = 1; // 1=ثبت, 2=ارسال, 3=تحویل; 0=لغو
    public bool Found { get; private set; }
    public string? Error { get; private set; }
    [BindProperty] public string? CancelText { get; set; }
    [BindProperty] public string? CancelReason { get; set; }
    public IReadOnlyList<(string Code, string Name)> CancelReasons { get; private set; } = [];
    public WalletPaymentInfo? WalletPayment { get; private set; }
    public string? Paid { get; private set; }
    public string? PayError { get; private set; }

    public sealed record WalletPaymentInfo(string? OrderNumber, string OrderStatus, string CurrencyCode, decimal Total, decimal Paid, decimal Remaining, decimal WalletBalance, bool FullyPaid);


    public async Task<IActionResult> OnPostPayWalletAsync(Guid id)
    {
        long version = 0;
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{id}");
        if (order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n)) version = n;
        (bool ok, string? problem) = await core.PostRawAsync($"api/v1/client/orders/{id}/pay-with-wallet", new { version });
        return RedirectToPage(new { id, paid = ok ? "1" : null, payerror = ok ? null : (problem ?? "wallet-failed") });
    }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = $"/account/orders/{Id}" });
    }

    public async Task<IActionResult> OnGetAsync(Guid id, string? error, string? paid, string? payerror)
    {
        Id = id;
        Error = error;
        Paid = paid;
        PayError = payerror;
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{id}");
        if (ReauthGuard() is { } r0) return r0;
        if (order is not JsonElement o || o.ValueKind != JsonValueKind.Object) return NotFound();
        Found = true;
        ParseOrder(o);
        await LoadTrackingAsync();
        JsonElement? wp = await core.GetAsync($"api/v1/client/orders/{id}/wallet-payment");
        if (wp is JsonElement w && w.ValueKind == JsonValueKind.Object)
        {
            WalletPayment = new WalletPaymentInfo(
                w.TryGetProperty("orderNumber", out JsonElement won) ? won.GetString() : null,
                w.TryGetProperty("orderStatus", out JsonElement wst) ? wst.GetString() ?? "" : "",
                w.TryGetProperty("currencyCode", out JsonElement wcu) ? wcu.GetString() ?? "IRR" : "IRR",
                w.TryGetProperty("total", out JsonElement wto) && wto.TryGetDecimal(out decimal wtot) ? wtot : 0,
                w.TryGetProperty("paid", out JsonElement wpa) && wpa.TryGetDecimal(out decimal wpd) ? wpd : 0,
                w.TryGetProperty("remaining", out JsonElement wre) && wre.TryGetDecimal(out decimal wre2) ? wre2 : 0,
                w.TryGetProperty("walletBalance", out JsonElement wba) && wba.TryGetDecimal(out decimal wbal) ? wbal : 0,
                w.TryGetProperty("fullyPaid", out JsonElement wfu) && wfu.ValueKind == JsonValueKind.True);
        }
        ViewData["Title"] = $"{OrderNumber ?? "—"} · پیرنگ";
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        long version = await CurrentVersionAsync(id);
        (bool ok, _, string? problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{id}/cancel",
            new { reasonCode = string.IsNullOrWhiteSpace(CancelReason) ? "customer-change" : CancelReason.Trim(), reasonText = string.IsNullOrWhiteSpace(CancelText) ? null : CancelText.Trim(), version });
        return RedirectToPage(new { error = ok ? null : problem ?? "cancel-failed" });
    }

    private void ParseOrder(JsonElement o)
    {
        OrderNumber = o.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        OrderStatus = o.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "";
        Stage = OrderStatus switch
        {
            "Delivered" => 3,
            "Submitted" => 2,
            "Draft" => 1,
            _ => 1
        };
        Version = o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
        OrderDate = ParseDateOnly(o.TryGetProperty("orderDate", out JsonElement od) ? od.GetString() : null);
        RequestedDelivery = ParseDateOnly(o.TryGetProperty("requestedDeliveryDate", out JsonElement rd) ? rd.GetString() : null);
        SubmittedAt = ParseDateTime(o.TryGetProperty("submittedAtUtc", out JsonElement sa) ? sa.GetString() : null);
        if (o.TryGetProperty("shippingAddress", out JsonElement ad) && ad.ValueKind == JsonValueKind.Object)
        {
            ShippingAddress = new OrderAddressRow(
                Prop(ad, "recipientName"),
                Prop(ad, "recipientMobile").Length > 0 ? Prop(ad, "recipientMobile") : Prop(ad, "recipientPhone"),
                string.Join("، ", new[] { Prop(ad, "province"), Prop(ad, "city"), Prop(ad, "district"), Prop(ad, "addressLine1"), Prop(ad, "addressLine2") }.Where(s => s.Length > 0)),
                Prop(ad, "postalCode"));
        }
        if (o.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
        {
            Items = items.EnumerateArray().Where(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False)
                .Select(x =>
                {
                    JsonElement snap = x.TryGetProperty("snapshot", out JsonElement sn) && sn.ValueKind == JsonValueKind.Object ? sn : default;
                    List<PrintOpRow> ops = [];
                    if (x.TryGetProperty("printOperations", out JsonElement pos) && pos.ValueKind == JsonValueKind.Array)
                    {
                        ops = pos.EnumerateArray()
                            .Where(p => !p.TryGetProperty("isActive", out JsonElement pact) || pact.ValueKind != JsonValueKind.False)
                            .Select(p => new PrintOpRow(
                                p.TryGetProperty("printPositionCode", out JsonElement pc) ? pc.GetString() ?? "" : "",
                                p.TryGetProperty("printMethodCode", out JsonElement pm) ? pm.GetString() : null,
                                p.TryGetProperty("width", out JsonElement w) && w.ValueKind == JsonValueKind.Number ? w.GetDecimal() : null,
                                p.TryGetProperty("height", out JsonElement h) && h.ValueKind == JsonValueKind.Number ? h.GetDecimal() : null,
                                p.TryGetProperty("notes", out JsonElement pn) ? pn.GetString() : null)).ToList();
                    }
                    return new ItemRow(
                        x.TryGetProperty("lineNumber", out JsonElement ln) && ln.TryGetInt32(out int line) ? line : 0,
                        Prop(snap, "productName").Length > 0 ? Prop(snap, "productName") : Prop(snap, "productCode"),
                        Prop(snap, "variantDisplayName").Length > 0 ? Prop(snap, "variantDisplayName") : Prop(snap, "variantSku"),
                        Prop(snap, "sizeCode"), Prop(snap, "colorCode"), Prop(snap, "uomCode"),
                        x.TryGetProperty("quantity", out JsonElement q) && q.ValueKind == JsonValueKind.Number ? q.GetDecimal() : 0,
                        x.TryGetProperty("notes", out JsonElement no) ? no.GetString() : null,
                        ops);
                }).ToList();
        }
    }

    private async Task LoadTrackingAsync()
    {
        JsonElement? result = await core.GetAsync($"api/v1/client/orders/{Id}/tracking");
        if (result is JsonElement x && x.ValueKind == JsonValueKind.Object)
        {
            if (x.TryGetProperty("shipments", out JsonElement sh) && sh.ValueKind == JsonValueKind.Array)
            {
                Shipments = sh.EnumerateArray().Select(s => new ShipmentRow(
                    s.TryGetProperty("shipmentNumber", out JsonElement sn) ? sn.GetString() ?? "" : "",
                    s.TryGetProperty("carrierCode", out JsonElement cc) ? cc.GetString() ?? "" : "",
                    s.TryGetProperty("trackingCode", out JsonElement tc) ? tc.GetString() : null,
                    s.TryGetProperty("status", out JsonElement ss) ? ss.GetString() ?? "" : "",
                    ParseDateTime(s.TryGetProperty("shippedAtUtc", out JsonElement sd) ? sd.GetString() : null),
                    ParseDateTime(s.TryGetProperty("deliveredAtUtc", out JsonElement dd) ? dd.GetString() : null))).ToList();
            }
            if (x.TryGetProperty("events", out JsonElement ev) && ev.ValueKind == JsonValueKind.Array)
            {
                Timeline = ev.EnumerateArray().Select(e => new TimelineRow(
                    e.TryGetProperty("type", out JsonElement ty) ? ty.GetString() ?? "" : "",
                    ParseDateTime(e.TryGetProperty("occurredAtUtc", out JsonElement oa) ? oa.GetString() : null)))
                    .OrderByDescending(e => e.At).ToList();
            }
        }
        JsonElement? reasons = await core.GetAsync("api/v1/client/lookups/order.cancellation.reason/options?culture=" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        if (reasons is JsonElement rr && rr.ValueKind == JsonValueKind.Array)
        {
            CancelReasons = rr.EnumerateArray().Select(x => (
                x.TryGetProperty("code", out JsonElement rc) ? rc.GetString() ?? "" : "",
                x.TryGetProperty("displayName", out JsonElement rn) ? (rn.GetString() ?? "") : "")).Where(x => x.Item1.Length > 0).ToList();
        }

        Quote = (await core.PostRawWithBodyAsync($"api/v1/client/orders/{Id}/pricing/preview", new { version = Version })).Value;
    }

    private async Task<long> CurrentVersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    private static string Prop(JsonElement x, string name) => x.ValueKind == JsonValueKind.Object && x.TryGetProperty(name, out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
    private static DateOnly? ParseDateOnly(string? iso) => DateOnly.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d) ? d : null;
    private static DateTimeOffset? ParseDateTime(string? iso) => DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : null;

    public string DateText(DateTimeOffset? at) => at is DateTimeOffset d ? d.ToLocalTime().ToString("yyyy/MM/dd HH:mm", CultureInfo.GetCultureInfo(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "fa")) : "—";
    public string DateText(DateOnly? d) => d is DateOnly v ? v.ToString("yyyy/MM/dd") : "—";

    public sealed record OrderAddressRow(string RecipientName, string RecipientMobile, string Line, string PostalCode);
    public sealed record PrintOpRow(string Position, string? Method, decimal? Width, decimal? Height, string? Notes);
    public sealed record ItemRow(int Line, string Product, string Variant, string Size, string Color, string Uom, decimal Quantity, string? Notes, IReadOnlyList<PrintOpRow> PrintOperations);
    public sealed record ShipmentRow(string Number, string Carrier, string? TrackingCode, string Status, DateTimeOffset? ShippedAt, DateTimeOffset? DeliveredAt);
    public sealed record TimelineRow(string Type, DateTimeOffset? At);
}
