using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record WalletEntry(Guid Id, string Direction, decimal Amount, string? ReasonCode, string? ReferenceNote, DateTimeOffset CreatedAtUtc);
public sealed record TopUpRow(Guid Id, string Number, decimal Amount, string Status, DateTimeOffset CreatedAtUtc, long Version);

[Authorize(Policy = "LoggedIn")]
public sealed class WalletModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public decimal Balance { get; private set; }
    public IReadOnlyList<WalletEntry> Items { get; private set; } = [];
    public IReadOnlyList<TopUpRow> TopUps { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Charged { get; private set; }
    public string? Failed { get; private set; }

    public async Task<IActionResult> OnGet(string? error, string? charged, string? failed)
    {
        Error = error;
        Charged = charged;
        Failed = failed;
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return Page();
        string customerId = cid.GetString() ?? "";
        Task<JsonElement?> walletTask = core.GetAsync($"api/v1/client/customers/{customerId}/wallet");
        Task<JsonElement?> topupsTask = core.GetAsync($"api/v1/client/customers/{customerId}/wallet/topups?pageSize=20");
        JsonElement? wallet = await walletTask;
        if (!core.SawUnauthorized && wallet is JsonElement w && w.ValueKind == JsonValueKind.Object)
        {
            Balance = w.TryGetProperty("balance", out JsonElement b) && b.TryGetDecimal(out decimal bal) ? bal : 0;
            if (w.TryGetProperty("transactions", out JsonElement txs) && txs.ValueKind == JsonValueKind.Array)
            {
                Items = txs.EnumerateArray().Select(x => new WalletEntry(
                    x.TryGetProperty("id", out JsonElement wid) && Guid.TryParse(wid.GetString(), out Guid wg) ? wg : Guid.Empty,
                    x.TryGetProperty("direction", out JsonElement dir) ? dir.GetString() ?? "" : "",
                    x.TryGetProperty("amount", out JsonElement am) && am.TryGetDecimal(out decimal amt) ? amt : 0,
                    x.TryGetProperty("reasonCode", out JsonElement rc) ? rc.GetString() : null,
                    x.TryGetProperty("referenceNote", out JsonElement rn) ? rn.GetString() : null,
                    x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), out DateTimeOffset d) ? d : default)).ToList();
            }
        }
        if (await topupsTask is JsonElement tp && tp.ValueKind == JsonValueKind.Array)
        {
            TopUps = tp.EnumerateArray().Select(x => new TopUpRow(
                x.TryGetProperty("id", out JsonElement tid) && Guid.TryParse(tid.GetString(), out Guid tg) ? tg : Guid.Empty,
                x.TryGetProperty("number", out JsonElement tn) ? tn.GetString() ?? "" : "",
                x.TryGetProperty("amount", out JsonElement ta) && ta.TryGetDecimal(out decimal tam) ? tam : 0,
                x.TryGetProperty("status", out JsonElement ts) ? ts.GetString() ?? "" : "",
                x.TryGetProperty("createdAtUtc", out JsonElement tca) && tca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(tca.GetString(), out DateTimeOffset td) ? td : default,
                x.TryGetProperty("version", out JsonElement tv) && tv.TryGetInt64(out long tver) ? tver : 0)).ToList();
        }
        if (core.SawUnauthorized) return RedirectToPage("/Account/Login", new { returnUrl = "/account/wallet" });
        return Page();
    }

    public async Task<IActionResult> OnPostChargeAsync(decimal amount, string? returnToken)
    {
        if (amount <= 0) return RedirectToPage(new { error = "amount" });
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        (bool ok, JsonElement? value, string? problem) = await core.PostRawWithBodyAsync(
            $"api/v1/client/customers/{cid.GetString()}/wallet/topups",
            new { amount, currencyCode = "IRR" },
            Guid.NewGuid().ToString("N"));
        if (!ok)
        {
            string error = problem switch
            {
                "wallet.below_minimum" => "below-minimum",
                "wallet.invalid_amount" => "amount",
                _ => "topup-failed"
            };
            return RedirectToPage(new { error });
        }
        string? token = value is JsonElement v && v.TryGetProperty("paymentToken", out JsonElement pt) ? pt.GetString() : null;
        if (string.IsNullOrWhiteSpace(token)) return RedirectToPage(new { error = "topup-failed" });
        return Redirect($"/payment/mock/{token}");
    }

    public async Task<IActionResult> OnPostCancelTopUpAsync(Guid topUpId, long version)
    {
        if (topUpId == Guid.Empty || await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage();
        await core.PostRawAsync($"api/v1/client/customers/{cid.GetString()}/wallet/topups/{topUpId}/cancel", new { version });
        return RedirectToPage();
    }
}
