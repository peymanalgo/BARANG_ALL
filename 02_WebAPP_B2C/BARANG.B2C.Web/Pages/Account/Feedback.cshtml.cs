using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

public sealed record FeedbackItem(Guid Id, string FeedbackTypeCode, string Subject, string Body, string StatusCode, DateTimeOffset CreatedAtUtc);

[Authorize(Policy = "LoggedIn")]
public sealed class FeedbackModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public IReadOnlyList<FeedbackItem> Items { get; private set; } = [];
    public string? Error { get; private set; }
    public bool Sent { get; private set; }

    [BindProperty] public string? FeedbackType { get; set; }
    [BindProperty] public string? Subject { get; set; }
    [BindProperty] public string? Body { get; set; }

    public async Task<IActionResult> OnGet(string? error, bool sent = false)
    {
        Error = error;
        Sent = sent;
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return Page();
        JsonElement? feedbacks = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/feedback");
        if (!core.SawUnauthorized && feedbacks is JsonElement f && f.ValueKind == JsonValueKind.Array)
        {
            Items = f.EnumerateArray().Select(x => new FeedbackItem(
                x.TryGetProperty("id", out JsonElement fid) && Guid.TryParse(fid.GetString(), out Guid fg) ? fg : Guid.Empty,
                x.TryGetProperty("feedbackTypeCode", out JsonElement ty) ? ty.GetString() ?? "" : "",
                x.TryGetProperty("subject", out JsonElement su) ? su.GetString() ?? "" : "",
                x.TryGetProperty("body", out JsonElement bo) ? bo.GetString() ?? "" : "",
                x.TryGetProperty("statusCode", out JsonElement st) ? st.GetString() ?? "" : "",
                x.TryGetProperty("createdAtUtc", out JsonElement ca) && ca.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ca.GetString(), out DateTimeOffset d) ? d : default)).ToList();
        }
        if (core.SawUnauthorized) return RedirectToPage("/Account/Login", new { returnUrl = "/account/feedback" });
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync()
    {
        string type = string.IsNullOrWhiteSpace(FeedbackType) ? "other" : FeedbackType.Trim();
        string subject = (Subject ?? "").Trim();
        string body = (Body ?? "").Trim();
        if (subject.Length is < 3 or > 120 || body.Length is < 10 or > 2000)
            return RedirectToPage(new { error = "validation" });
        if (await customers.CurrentAsync() is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "fail" });
        (bool ok, string? problem) = await core.PostRawAsync($"api/v1/client/customers/{cid.GetString()}/feedback", new { feedbackTypeCode = type, subject, body });
        if (!ok) return RedirectToPage(new { error = string.Equals(problem, "validation.failed", StringComparison.OrdinalIgnoreCase) ? "validation" : "fail" });
        return RedirectToPage(new { sent = true });
    }
}
