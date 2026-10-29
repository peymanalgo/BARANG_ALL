using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class OrdersModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public bool HasProfile { get; private set; }
    public IReadOnlyList<OrderRow> Orders { get; private set; } = [];
    public long Total { get; private set; }
    public int Page { get; private set; } = 1;
    private int PageSize { get; } = 8;
    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid)) return;
        HasProfile = true;
        JsonElement? result = await core.GetAsync($"api/v1/client/orders?customerId={cid.GetString()}&page={Page}&pageSize={PageSize}");
        if (result is JsonElement el && el.ValueKind == JsonValueKind.Object)
        {
            Total = el.TryGetProperty("total", out JsonElement t) && t.TryGetInt64(out long n) ? n : 0;
            if (el.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
            {
                Orders = items.EnumerateArray().Select(x => new OrderRow(
                    x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
                    x.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null,
                    x.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "",
                    x.TryGetProperty("orderDate", out JsonElement od) ? od.GetString() : null,
                    x.TryGetProperty("createdAtUtc", out JsonElement ca) ? ca.GetString() : null)).ToList();
            }
        }
    }

    public sealed record OrderRow(Guid Id, string? OrderNumber, string Status, string? OrderDate, string? CreatedAtUtc)
    {
        public string Number => string.IsNullOrWhiteSpace(OrderNumber) ? "—" : OrderNumber!;
    }
}
