using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record RecentOrder(Guid Id, string? OrderNumber, string Status, string? OrderDate, DateTimeOffset CreatedAtUtc);
public sealed record FavoriteDesign(Guid ArtworkId, string Title, string? ThumbnailUrl);
public sealed record OfferDesign(Guid Id, string Title, string? ThumbnailUrl);
public sealed record AddressCard(Guid Id, string? Title, string? TypeCode, string? City, string? AddressLine, bool IsDefault);
public sealed record WalletSummary(decimal Balance, string CurrencyCode);

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
    public long InProgressCount { get; private set; }
    public long DeliveredCount { get; private set; }
    public long FavoriteCount { get; private set; }
    public WalletSummary Wallet { get; private set; } = new(0, "IRR");
    public string? ReferralCode { get; private set; }
    public int InvitedCount { get; private set; }
    public decimal ReferralEarned { get; private set; }
    public IReadOnlyList<NotificationRow> Notifications { get; private set; } = [];
    public IReadOnlyList<RecentOrder> RecentOrders { get; private set; } = [];
    public IReadOnlyList<FavoriteDesign> Favorites { get; private set; } = [];
    public IReadOnlyList<OfferDesign> Offers { get; private set; } = [];
    public IReadOnlyList<AddressCard> Addresses { get; private set; } = [];
    public long UnreadCount { get; private set; }

    [BindProperty] public string? ProfileFirstName { get; set; }
    [BindProperty] public string? ProfileLastName { get; set; }
    [BindProperty] public string? ProfileMobile { get; set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = "/account" });
    }

    public async Task<IActionResult> OnGet(string? error, [FromQuery(Name = "ref")] string? refCode) 
    {
        Error = error;
        if (!string.IsNullOrWhiteSpace(refCode) && refCode.Length is >= 4 and <= 20)
        {
            Response.Cookies.Append("barang.ref", refCode.Trim().ToUpperInvariant(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), IsEssential = true });
        }
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

            Task<JsonElement?> addressesTask = core.GetAsync($"api/v1/client/customers/{customerId}/addresses");
            Task<JsonElement?> ordersTask = core.GetAsync($"api/v1/client/orders?customerId={customerId}&page=1&pageSize=5");
            Task<JsonElement?> favoritesTask = core.GetAsync($"api/v1/client/customers/{customerId}/favorites");
            Task<JsonElement?> walletTask = core.GetAsync($"api/v1/client/customers/{customerId}/wallet");
            Task<JsonElement?> notifsTask = core.GetAsync("api/v1/notifications/mine?page=1&pageSize=5");
            Task<JsonElement?> offersTask = core.GetAsync("api/v1/public/catalog?page=1&pageSize=8");
            Task<JsonElement?> referralTask = core.GetAsync($"api/v1/client/customers/{customerId}/referral");
            await Task.WhenAll(addressesTask, ordersTask, favoritesTask, walletTask, notifsTask, offersTask, referralTask);
            if (ReauthGuard() is { } r2) return r2;

            if (addressesTask.Result is JsonElement a && a.ValueKind == JsonValueKind.Array)
            {
                AddressCount = a.GetArrayLength();
                Addresses = a.EnumerateArray().Take(2).Select(x => new AddressCard(
                    x.TryGetProperty("id", out JsonElement aid) && Guid.TryParse(aid.GetString(), out Guid ag) ? ag : Guid.Empty,
                    x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() : null,
                    x.TryGetProperty("addressTypeCode", out JsonElement ty) ? ty.GetString() : null,
                    x.TryGetProperty("cityCode", out JsonElement ci) ? ci.GetString() : null,
                    x.TryGetProperty("addressLine1", out JsonElement al) ? al.GetString() : null,
                    x.TryGetProperty("isDefault", out JsonElement df) && df.ValueKind == JsonValueKind.True)).ToList();
            }

            if (ordersTask.Result is JsonElement o)
            {
                OrderCount = o.TryGetProperty("totalCount", out JsonElement t) && t.TryGetInt64(out long orderTotal) ? orderTotal : 0;
                if (o.TryGetProperty("items", out JsonElement oi) && oi.ValueKind == JsonValueKind.Array)
                {
                    List<RecentOrder> recent = [];
                    foreach (JsonElement x2 in oi.EnumerateArray())
                    {
                        string status = x2.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "";
                        if (status is "Draft" or "Cancelled") { }
                        else if (status == "Delivered") DeliveredCount++;
                        else InProgressCount++;
                        recent.Add(new RecentOrder(
                            x2.TryGetProperty("id", out JsonElement rid) && Guid.TryParse(rid.GetString(), out Guid rg) ? rg : Guid.Empty,
                            x2.TryGetProperty("orderNumber", out JsonElement rnum) ? rnum.GetString() : null,
                            status,
                            x2.TryGetProperty("orderDate", out JsonElement od) ? od.GetString() : null,
                            x2.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : default));
                    }
                    RecentOrders = recent;
                }
            }

            if (favoritesTask.Result is JsonElement f && f.ValueKind == JsonValueKind.Array)
            {
                FavoriteCount = f.GetArrayLength();
                Favorites = f.EnumerateArray().Take(4).Select(x => new FavoriteDesign(
                    x.TryGetProperty("artworkId", out JsonElement fid) && Guid.TryParse(fid.GetString(), out Guid fg) ? fg : Guid.Empty,
                    x.TryGetProperty("title", out JsonElement ft) ? ft.GetString() ?? "" : "",
                    x.TryGetProperty("thumbnailUrl", out JsonElement th) ? th.GetString() : null)).ToList();
            }

            if (walletTask.Result is JsonElement w && w.ValueKind == JsonValueKind.Object)
            {
                Wallet = new WalletSummary(
                    w.TryGetProperty("balance", out JsonElement b) && b.TryGetDecimal(out decimal bal) ? bal : 0,
                    w.TryGetProperty("currencyCode", out JsonElement cur) ? cur.GetString() ?? "IRR" : "IRR");
            }

            if (notifsTask.Result is JsonElement n && n.ValueKind == JsonValueKind.Object)
            {
                Notifications = NotificationRow.FromPage(n);
                UnreadCount = 0;
                foreach (NotificationRow row in Notifications) if (row.Unread) UnreadCount++;
            }

            if (referralTask.Result is JsonElement rf && rf.ValueKind == JsonValueKind.Object)
            {
                ReferralCode = rf.TryGetProperty("code", out JsonElement rcode) ? rcode.GetString() : null;
                InvitedCount = rf.TryGetProperty("invitedCount", out JsonElement ric) && ric.TryGetInt32(out int invited) ? invited : 0;
                ReferralEarned = rf.TryGetProperty("totalEarned", out JsonElement rte) && rte.TryGetDecimal(out decimal earned) ? earned : 0;
            }

            if (offersTask.Result is JsonElement of && of.TryGetProperty("items", out JsonElement oi2) && oi2.ValueKind == JsonValueKind.Array)
            {
                Random rnd = Random.Shared;
                List<OfferDesign> pool = oi2.EnumerateArray().Select(x => new OfferDesign(
                    x.TryGetProperty("id", out JsonElement oid) && Guid.TryParse(oid.GetString(), out Guid og) ? og : Guid.Empty,
                    x.TryGetProperty("title", out JsonElement ot) ? ot.GetString() ?? "" : "",
                    x.TryGetProperty("thumbnailUrl", out JsonElement oth) ? oth.GetString() : null)).ToList();
                Offers = pool.OrderBy(_ => rnd.Next()).Take(4).ToList();
            }
        }
        else
        {
            await LoadOffersAsync();
        }
        return Page();
    }

    private async Task LoadOffersAsync()
    {
        JsonElement? offers = await core.GetAsync("api/v1/public/catalog?page=1&pageSize=8");
        if (offers is JsonElement of && of.TryGetProperty("items", out JsonElement oi) && oi.ValueKind == JsonValueKind.Array)
        {
            List<OfferDesign> pool = oi.EnumerateArray().Select(x => new OfferDesign(
                x.TryGetProperty("id", out JsonElement oid) && Guid.TryParse(oid.GetString(), out Guid og) ? og : Guid.Empty,
                x.TryGetProperty("title", out JsonElement ot) ? ot.GetString() ?? "" : "",
                x.TryGetProperty("thumbnailUrl", out JsonElement oth) ? oth.GetString() : null)).ToList();
            Offers = pool.OrderBy(_ => Random.Shared.Next()).Take(4).ToList();
        }
    }

    public async Task<IActionResult> OnPostCreateProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(ProfileFirstName) || string.IsNullOrWhiteSpace(ProfileLastName) || string.IsNullOrWhiteSpace(ProfileMobile))
        {
            return RedirectToPage(new { error = "profile-fields" });
        }
        JsonElement? created = await customers.RegisterSelfAsync(ProfileFirstName, ProfileLastName, ProfileMobile);
        if (created is null) return RedirectToPage(new { error = "register" });
        string? refCode = Request.Cookies["barang.ref"];
        if (!string.IsNullOrWhiteSpace(refCode))
        {
            await core.PostRawAsync($"api/v1/client/customers/{created.Value.GetProperty("id").GetString()}/referrals/attach", new { code = refCode.Trim() });
            Response.Cookies.Delete("barang.ref");
        }
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
            string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return items.EnumerateArray().Select(x => new NotificationRow(
                x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
                BARANG.B2C.Web.Services.NotificationText.Localize(culture,
                    x.TryGetProperty("templateCode", out JsonElement tc) ? tc.GetString() : null,
                    x.TryGetProperty("payload", out JsonElement pl) ? pl.GetString() : null),
                x.TryGetProperty("category", out JsonElement c) ? c.GetString() ?? "" : "",
                x.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "",
                x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : default)).ToList();
        }
    }
}
