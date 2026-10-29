using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class NotificationsModel(CoreApiClient core) : PageModel
{
    public IReadOnlyList<NotifRow> Notifications { get; private set; } = [];
    public long Total { get; private set; }
    public long UnreadCount { get; private set; }
    public int Page { get; private set; } = 1;
    private int PageSize { get; } = 12;
    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;
    [BindProperty(SupportsGet = true)] public string? Unread { get; set; }

    public async Task OnGet([FromQuery] int? page)
    {
        Page = Math.Max(1, page ?? 1);
        bool unreadOnly = Unread == "1";
        string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        string path = $"api/v1/notifications/mine?page={Page}&pageSize={PageSize}" + (unreadOnly ? "&unread=true" : "");
        JsonElement? result = await core.GetAsync(path);
        if (result is JsonElement el && el.ValueKind == JsonValueKind.Object)
        {
            Total = el.TryGetProperty("total", out JsonElement t) && t.TryGetInt64(out long n) ? n : 0;
            if (el.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
            {
                Notifications = items.EnumerateArray().Select(x => new NotifRow(
                    x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
                    NotificationText.Localize(culture,
                        x.TryGetProperty("templateCode", out JsonElement tc) ? tc.GetString() : null,
                        x.TryGetProperty("payload", out JsonElement pl) ? pl.GetString() : null),
                    x.TryGetProperty("category", out JsonElement c) ? c.GetString() ?? "" : "",
                    x.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "" : "",
                    x.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long ver) ? ver : 0,
                    x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset d) ? d : default)).ToList();
                UnreadCount = Notifications.Count(x => x.Unread);
            }
        }
        if (Page == 1 && !unreadOnly)
        {
            JsonElement? all = await core.GetAsync("api/v1/notifications/mine?unread=true&page=1&pageSize=1");
            UnreadCount = all is JsonElement u && u.TryGetProperty("total", out JsonElement ut) && ut.TryGetInt64(out long un) ? un : 0;
        }
    }

    public async Task<IActionResult> OnPostMarkReadAsync(Guid id, long version)
    {
        await core.PostRawAsync($"api/v1/notifications/{id}/read", new { version });
        return RedirectToPage(new { unread = Unread });
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        await core.PostRawAsync("api/v1/notifications/read-all", null);
        return RedirectToPage();
    }

    public sealed record NotifRow(Guid Id, string Summary, string Category, string Status, long Version, DateTimeOffset CreatedAtUtc)
    {
        public bool Unread => !string.Equals(Status, "Read", StringComparison.OrdinalIgnoreCase);
    }
}
