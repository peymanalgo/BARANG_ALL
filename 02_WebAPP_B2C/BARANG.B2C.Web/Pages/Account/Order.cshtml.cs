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
    public IReadOnlyList<ShipmentRow> Shipments { get; private set; } = [];
    public IReadOnlyList<TimelineRow> Timeline { get; private set; } = [];
    public bool Found { get; private set; }
    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = "/account" });
    }


    public async Task<IActionResult> OnGet(Guid id)
    {
        Id = id;
        JsonElement? result = await core.GetAsync($"api/v1/client/orders/{id}/tracking");
        if (result is not JsonElement x || x.ValueKind != JsonValueKind.Object) return NotFound();
        Found = true;
        OrderNumber = x.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        OrderStatus = x.TryGetProperty("orderStatus", out JsonElement st) ? st.GetString() ?? "" : "";
        if (x.TryGetProperty("shipments", out JsonElement sh) && sh.ValueKind == JsonValueKind.Array)
        {
            Shipments = sh.EnumerateArray().Select(s => new ShipmentRow(
                s.TryGetProperty("shipmentNumber", out JsonElement sn) ? sn.GetString() ?? "" : "",
                s.TryGetProperty("carrierCode", out JsonElement cc) ? cc.GetString() ?? "" : "",
                s.TryGetProperty("shippingMethodCode", out JsonElement mc) ? mc.GetString() ?? "" : "",
                s.TryGetProperty("trackingCode", out JsonElement tc) ? tc.GetString() : null,
                s.TryGetProperty("status", out JsonElement ss) ? ss.GetString() ?? "" : "",
                ParseDate(s.TryGetProperty("shippedAtUtc", out JsonElement sd) ? sd.GetString() : null),
                ParseDate(s.TryGetProperty("deliveredAtUtc", out JsonElement dd) ? dd.GetString() : null))).ToList();
        }
        if (x.TryGetProperty("events", out JsonElement ev) && ev.ValueKind == JsonValueKind.Array)
        {
            Timeline = ev.EnumerateArray().Select(e => new TimelineRow(
                e.TryGetProperty("type", out JsonElement ty) ? ty.GetString() ?? "" : "",
                ParseDate(e.TryGetProperty("occurredAtUtc", out JsonElement oa) ? oa.GetString() : null)))
                .OrderByDescending(e => e.At).ToList();
        }
        ViewData["Title"] = $"{TOrderNumber()} · برنگ";
        return Page();
    }

    private string? TOrderNumber() => OrderNumber;

    private static DateTimeOffset? ParseDate(string? iso) => DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : null;

    public sealed record ShipmentRow(string Number, string Carrier, string Method, string? TrackingCode, string Status, DateTimeOffset? ShippedAt, DateTimeOffset? DeliveredAt);
    public sealed record TimelineRow(string Type, DateTimeOffset? At);

    public string DateText(DateTimeOffset? at) => at is DateTimeOffset d ? d.ToLocalTime().ToString("yyyy/MM/dd HH:mm", CultureInfo.GetCultureInfo(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "fa")) : "—";
}
