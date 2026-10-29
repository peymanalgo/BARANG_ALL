using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record WalletEntry(Guid Id, string Direction, decimal Amount, string? ReasonCode, string? ReferenceNote, DateTimeOffset CreatedAtUtc);

[Authorize(Policy = "LoggedIn")]
public sealed class WalletModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public decimal Balance { get; private set; }
    public IReadOnlyList<WalletEntry> Items { get; private set; } = [];

    public async Task<IActionResult> OnGet()
    {
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return Page();
        JsonElement? wallet = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/wallet");
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
        if (core.SawUnauthorized) return RedirectToPage("/Account/Login", new { returnUrl = "/account/wallet" });
        return Page();
    }
}
