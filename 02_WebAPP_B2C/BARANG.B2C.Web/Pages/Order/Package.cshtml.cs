using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// Order wizard step 4 — packaging + invoice-in-package, stored on the order
// via the business-profile endpoint (ERP step 4 equivalent). Pricing in the
// next step recalculates from these values.
[Authorize(Policy = "LoggedIn")]
public sealed class PackageModel(CoreApiClient core) : PageModel
{
    public Guid OrderId { get; private set; }
    public Guid DesignId { get; set; }
    public string? OrderNumber { get; private set; }
    public long Version { get; private set; }
    public string? PackagingTypeCode { get; private set; }
    public bool IncludeInvoice { get; private set; }
    public IReadOnlyList<(string Code, string Name)> PackagingOptions { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    [BindProperty] public string? PackagingCode { get; set; }
    [BindProperty] public bool InvoiceInPackage { get; set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = $"/order/package/{OrderId}" });
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, Guid? designId, string? error, string? message)
    {
        OrderId = orderId;
        if (designId is Guid d) DesignId = d;
        Error = error;
        Message = message;
        if (ReauthGuard() is { } r0) return r0;

        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        if (order is not JsonElement o || o.ValueKind != JsonValueKind.Object) return NotFound();
        OrderNumber = o.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        Version = o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;

        JsonElement? profile = await core.GetAsync($"api/v1/client/orders/{orderId}/business-profile");
        if (profile is JsonElement p && p.ValueKind == JsonValueKind.Object)
        {
            PackagingTypeCode = p.TryGetProperty("packagingTypeCode", out JsonElement pc) ? pc.GetString() : null;
            IncludeInvoice = p.TryGetProperty("includeInvoiceInPackage", out JsonElement ii) && ii.ValueKind == JsonValueKind.True;
            Version = p.TryGetProperty("version", out JsonElement pv) && pv.TryGetInt64(out long pvv) ? pvv : Version;
        }

        PackagingOptions = await LookupOptionsAsync("packaging.type");
        if (DesignId == Guid.Empty)
        {
            var qs = Request.Query["designId"].ToString();
            if (Guid.TryParse(qs, out Guid dg)) DesignId = dg;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid orderId, Guid designId)
    {
        if (string.IsNullOrWhiteSpace(PackagingCode))
        {
            return RedirectToPage(new { orderId, designId, error = "required" });
        }
        long version = await CurrentBusinessProfileVersionAsync(orderId);
        (bool ok, string? problem) = await core.PutRawAsync($"api/v1/client/orders/{orderId}/business-profile", new
        {
            shippingMethodCode = (string?)null,
            packagingTypeCode = PackagingCode.Trim(),
            includeInvoiceInPackage = InvoiceInPackage,
            orderTypeCode = (string?)null,
            notes = (string?)null,
            version
        });
        if (!ok)
        {
            return RedirectToPage(new { orderId, designId, error = problem ?? "profile-failed" });
        }
        return RedirectToPage("Review", new { orderId, designId });
    }

    private async Task<long> CurrentBusinessProfileVersionAsync(Guid orderId)
    {
        JsonElement? profile = await core.GetAsync($"api/v1/client/orders/{orderId}/business-profile");
        if (profile is JsonElement p && p.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n)) return n;
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement ov) && ov.TryGetInt64(out long on) ? on : 0;
    }

    private async Task<List<(string Code, string Name)>> LookupOptionsAsync(string typeCode)
    {
        JsonElement? result = await core.GetAsync($"api/v1/client/lookups/{typeCode}/options?culture=" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        if (result is not JsonElement arr || arr.ValueKind != JsonValueKind.Array) return [];
        return arr.EnumerateArray().Select(x => (
            Code: x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "",
            Name: x.TryGetProperty("displayName", out JsonElement n) ? (n.GetString() ?? "") : "") ).Where(x => x.Code.Length > 0).ToList();
    }
}
