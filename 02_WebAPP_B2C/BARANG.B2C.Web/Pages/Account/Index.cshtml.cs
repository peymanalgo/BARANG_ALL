using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record RecentOrder(Guid Id, string? OrderNumber, string Status);

[Authorize(Policy = "LoggedIn")]
public sealed class IndexModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public string? DisplayName { get; private set; }
    public string? Username { get; private set; }
    public string? Error { get; private set; }
    public bool HasProfile { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerCode { get; private set; }
    public long AddressCount { get; private set; }
    public long OrderCount { get; private set; }
    public IReadOnlyList<NotificationRow> Notifications { get; private set; } = [];
    public IReadOnlyList<RecentOrder> RecentOrders { get; private set; } = [];
    public long UnreadCount { get; private set; }

    [BindProperty] public string? ProfileFirstName { get; set; }
    [BindProperty] public string? ProfileLastName { get; set; }
    [BindProperty] public string? ProfileMobile { get; set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = "/account" });
    }
    public async Task<IActionResult> OnGet(string? error)
    {
        Error = error;
        Username = User.FindFirst("barang:username")?.Value;
        DisplayName = User.Identity?.Name;
        JsonElement? me = await core.GetAsync("api/v1/auth/me");
        if (ReauthGuard() is { } r1) return r1;
        if (me is JsonElement x && x.ValueKind == JsonValueKind.Object)
        {
            DisplayName = x.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? DisplayName : DisplayName;
            Username = x.TryGetProperty("username", out JsonElement un) ? un.GetString() ?? Username : Username;
        }

        JsonElement? customer = await customers.CurrentAsync();
        if (customer is JsonElement c && c.ValueKind == JsonValueKind.Object && c.TryGetProperty("id", out JsonElement cid))
        {
            HasProfile = true;
            CustomerName = c.TryGetProperty("displayName", out JsonElement name) ? name.GetString() : null;
            CustomerCode = c.TryGetProperty("customerCode", out JsonElement code) ? code.GetString() : null;
            string customerId = cid.GetString() ?? "";

            JsonElement? addresses = await core.GetAsync($"api/v1/client/customers/{customerId}/addresses");
            AddressCount = addresses is JsonElement a && a.ValueKind == JsonValueKind.Array ? a.GetArrayLength() : 0;

            JsonElement? orders = await core.GetAsync($"api/v1/client/orders?customerId={customerId}&page=1&pageSize=3");
            OrderCount = orders is JsonElement o && o.TryGetProperty("total", out JsonElement t) && t.TryGetInt64(out long orderTotal) ? orderTotal : 0;
            if (orders is JsonElement oo && oo.TryGetProperty("items", out JsonElement oi) && oi.ValueKind == JsonValueKind.Array)
            {
                RecentOrders = oi.EnumerateArray().Select(x => new RecentOrder(
                    x.TryGetProperty("id", out JsonElement rid) && Guid.TryParse(rid.GetString(), out Guid rg) ? rg : Guid.Empty,
                    x.TryGetProperty("orderNumber", out JsonElement rnum) ? rnum.GetString() : null,
                    x.TryGetProperty("status", out JsonElement rst) ? rst.GetString() ?? "" : "")).ToList();
            }
        }

        JsonElement? notifs = await core.GetAsync("api/v1/notifications/mine?page=1&pageSize=5");
        if (notifs is JsonElement n && n.ValueKind == JsonValueKind.Object)
        {
            Notifications = NotificationRow.FromPage(n);
            UnreadCount = 0;
            foreach (NotificationRow row in Notifications) if (row.Unread) UnreadCount++;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostCreateProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(ProfileFirstName) || string.IsNullOrWhiteSpace(ProfileLastName) || string.IsNullOrWhiteSpace(ProfileMobile))
        {
            return RedirectToPage(new { error = "profile-fields" });
        }
        JsonElement? created = await customers.RegisterSelfAsync(ProfileFirstName, ProfileLastName, ProfileMobile);
        if (created is null) return RedirectToPage(new { error = "register" });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        await core.PostRawAsync("api/v1/notifications/read-all", null);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await core.LogoutAsync();
        return RedirectToPage("/Index");
    }

    public sealed record NotificationRow(Guid Id, string Summary, string Category, string Status, DateTimeOffset CreatedAtUtc)
    {
        public bool Unread => !string.Equals(Status, "Read", StringComparison.OrdinalIgnoreCase) && !string.Equals(Status, "ReadAtUtc", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<NotificationRow> FromPage(JsonElement page)
        {
            if (!page.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array) return [];
            return items.EnumerateArray().Select(x => new NotificationRow(
                x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
                x.TryGetProperty("summary", out JsonElement s) ? s.GetString() ?? "" : "",
                x.TryGetProperty("category", out JsonElement c) ? c.GetString() ?? "" : "",
                x.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "",
                x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : default)).ToList();
        }
    }
}
