using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Payment;

public sealed record GatewayInfo(string Number, decimal Amount, string CurrencyCode, string Status);

// Sandbox payment gateway page (the mock part of the wallet flow). It reads the
// top-up by token from Core's public endpoint and posts the result back — the
// same round-trip a real bank gateway will perform later.
public sealed class MockModel(CoreApiClient core) : PageModel
{
    public Guid Token { get; private set; }
    public GatewayInfo? Info { get; private set; }
    public bool NotFound { get; private set; }
    public string? Result { get; private set; }
    public string? LoadError { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid token, string? result)
    {
        Token = token;
        Result = result;
        await LoadAsync(token);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid token, string success)
    {
        bool ok = string.Equals(success, "true", StringComparison.OrdinalIgnoreCase);
        string reference = $"MOCK-{DateTime.UtcNow.Ticks}";
        (bool done, string? _) = await core.PostRawAsync($"api/v1/public/wallet/topups/{token}/mock-callback", new { success = ok, gatewayReference = reference });
        if (!done)
        {
            return RedirectToPage(new { token, result = "failed" });
        }
        return RedirectToPage(new { token, result = ok ? "success" : "failed" });
    }

    private async Task LoadAsync(Guid token)
    {
        JsonElement? info = await core.GetAsync($"api/v1/public/wallet/topups/{token}");
        if (info is not JsonElement i || i.ValueKind != JsonValueKind.Object)
        {
            NotFound = true;
            LoadError = null;
            return;
        }
        Info = new GatewayInfo(
            i.TryGetProperty("number", out JsonElement n) ? n.GetString() ?? "" : "",
            i.TryGetProperty("amount", out JsonElement a) && a.TryGetDecimal(out decimal amt) ? amt : 0,
            i.TryGetProperty("currencyCode", out JsonElement c) ? c.GetString() ?? "IRR" : "IRR",
            i.TryGetProperty("status", out JsonElement s) ? s.GetString() ?? "" : "");
    }
}
