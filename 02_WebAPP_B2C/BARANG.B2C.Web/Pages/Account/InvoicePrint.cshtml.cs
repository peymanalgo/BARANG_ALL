using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record InvoiceItemRow(string ProductName, string VariantSku, decimal Quantity);
public sealed record InvoicePaymentRow(decimal Amount, string Method, DateTimeOffset PaidAtUtc);

// Print-friendly invoice built from the order + its payments.
[Authorize(Policy = "LoggedIn")]
public sealed class InvoicePrintModel(CoreApiClient core) : PageModel
{
    public Guid Id { get; private set; }
    public string? OrderNumber { get; private set; }
    public string? CustomerName { get; private set; }
    public decimal Total { get; private set; }
    public bool FullyPaid { get; private set; }
    public DateTimeOffset? CreatedAtUtc { get; private set; }
    public IReadOnlyList<InvoiceItemRow> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Id = id;
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{id}");
        if (order is not JsonElement o || o.ValueKind != JsonValueKind.Object) return NotFound();
        OrderNumber = o.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        CreatedAtUtc = o.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : null;
        if (o.TryGetProperty("customer", out JsonElement cust) && cust.ValueKind == JsonValueKind.Object)
            CustomerName = cust.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() : null;
        else
            CustomerName = User.Identity?.Name;
        if (o.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
        {
            Items = items.EnumerateArray()
                .Where(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False)
                .Select(x => new InvoiceItemRow(
                    x.TryGetProperty("productName", out JsonElement pn) ? pn.GetString() ?? "—" : "—",
                    x.TryGetProperty("variantSku", out JsonElement vs) ? vs.GetString() ?? "" : "",
                    x.TryGetProperty("quantity", out JsonElement q) && q.TryGetDecimal(out decimal qd) ? qd : 0)).ToList();
        }

        JsonElement? pay = await core.GetAsync($"api/v1/client/orders/{id}/wallet-payment");
        if (pay is JsonElement w && w.ValueKind == JsonValueKind.Object)
        {
            Total = w.TryGetProperty("total", out JsonElement t) && t.TryGetDecimal(out decimal tt) ? tt : 0;
            FullyPaid = w.TryGetProperty("fullyPaid", out JsonElement fp) && fp.ValueKind == JsonValueKind.True;
        }
        return Page();
    }
}
