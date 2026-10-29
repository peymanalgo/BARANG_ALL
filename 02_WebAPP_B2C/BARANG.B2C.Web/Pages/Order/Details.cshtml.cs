using System.Globalization;
using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Order;

// Order wizard step 2 — print operation (placement/size/method + optional
// customer file upload), live pricing and validation, then final submit.
// The chosen public design is linked for real: designId travels in the URL,
// its current artworkVersionId is resolved from the public catalog and sent
// on the print operation.
[Authorize(Policy = "LoggedIn")]
public sealed class DetailsModel(CoreApiClient core) : PageModel
{
    public Guid OrderId { get; private set; }
    public string? OrderNumber { get; private set; }
    public long Version { get; private set; }
    [BindProperty(SupportsGet = true)] public Guid DesignId { get; set; }
    public string? DesignCode { get; private set; }
    public string? DesignPreview { get; private set; }
    public Guid? ArtworkVersionId { get; private set; }
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    public IReadOnlyList<string> PrintPositions { get; private set; } = ["FRONT", "BACK", "SLEEVE", "OTHER"];
    public IReadOnlyList<(string Code, string Name)> PrintMethods { get; private set; } = [];
    public JsonElement? Quote { get; private set; }
    public IReadOnlyList<ValidationIssueRow> Issues { get; private set; } = [];

    [BindProperty] public string PrintPosition { get; set; } = "FRONT";
    [BindProperty] public string? PrintMethod { get; set; }
    [BindProperty] public decimal? Width { get; set; }
    [BindProperty] public decimal? Height { get; set; }
    [BindProperty] public string? PrintNotes { get; set; }
    [BindProperty] public IFormFile? CustomFile { get; set; }

    private IActionResult? ReauthGuard()
    {
        if (!core.SawUnauthorized) return null;
        return RedirectToPage("/Account/Login", new { returnUrl = $"/order/details/{OrderId}" });
    }

    public async Task<IActionResult> OnGetAsync(Guid orderId, Guid? designId, string? error, string? message)
    {
        Error = error;
        Message = message;
        OrderId = orderId;
        if (designId is Guid d)
        {
            DesignId = d;
            (DesignCode, DesignPreview, ArtworkVersionId) = await LoadDesignAsync(d);
        }
        if (ReauthGuard() is { } r0) return r0;
        return await LoadAsync();
    }

    public async Task<IActionResult> OnPostSavePrintAsync(Guid orderId, Guid designId)
    {
        OrderId = orderId;
        if (ReauthGuard() is { } r0) return r0;
        long version = await CurrentVersionAsync(orderId);

        string? fileVersionId = null;
        if (CustomFile is { Length: > 0 })
        {
            string idemKey = $"file-{Guid.NewGuid():N}";
            (bool upOk, JsonElement? up, string? upProblem) = await core.PostMultipartAsync(
                "api/v1/client/files/upload", CustomFile.FileName, CustomFile.ContentType, CustomFile.OpenReadStream(), CustomFile.Length);
            if (!upOk || up is null || !up.Value.TryGetProperty("fileVersionId", out JsonElement fvid))
            {
                return RedirectToPage(new { orderId, designId, error = upProblem ?? "upload-failed" });
            }
            fileVersionId = fvid.GetString();
        }

        (string? code, _, Guid? artworkVersion) = await LoadDesignAsync(designId);
        string designNote = string.IsNullOrWhiteSpace(code) ? "" : $"طرح: {code}";
        string notes = string.Join(" | ", new[] { PrintNotes, designNote }.Where(s => !string.IsNullOrWhiteSpace(s)));

        (bool opOk, string? opProblem) = await AddPrintOperationAsync(orderId, version, artworkVersion, fileVersionId, notes);
        return RedirectToPage(new { orderId, designId, error = opOk ? null : opProblem ?? "print-failed", message = opOk ? "saved" : null });
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid orderId, Guid designId)
    {
        long version = await CurrentVersionAsync(orderId);
        string idemKey = $"submit-{orderId:N}";
        (bool ok, _, string? problem) = await core.PostRawWithBodyAsync($"api/v1/client/orders/{orderId}/submit", new { version }, idemKey);
        if (!ok)
        {
            return RedirectToPage(new { orderId, designId, error = problem ?? "submit-failed" });
        }
        return RedirectToPage("/Account/Order", new { id = orderId });
    }

    private async Task<(string? Code, string? Preview, Guid? ArtworkVersion)> LoadDesignAsync(Guid designId)
    {
        JsonElement? design = await core.GetAsync($"api/v1/public/catalog/{designId}");
        if (design is not JsonElement d || d.ValueKind != JsonValueKind.Object) return (null, null, null);
        string? code = d.TryGetProperty("code", out JsonElement c) ? c.GetString() : null;
        string? preview = d.TryGetProperty("previewUrl", out JsonElement pv) ? pv.GetString() : null;
        Guid? artworkVersion = d.TryGetProperty("artworkVersionId", out JsonElement av) && av.ValueKind == JsonValueKind.String && Guid.TryParse(av.GetString(), out Guid g) ? g : null;
        DesignCode ??= code;
        return (code, preview, artworkVersion);
    }

    private async Task<IActionResult> LoadAsync()
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{OrderId}");
        if (order is not JsonElement o || o.ValueKind != JsonValueKind.Object) return NotFound();
        OrderNumber = o.TryGetProperty("orderNumber", out JsonElement num) ? num.GetString() : null;
        Version = o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;

        JsonElement? form = await core.GetAsync("api/v1/client/orders/form");
        if (form is JsonElement f && f.TryGetProperty("printPositions", out JsonElement pp) && pp.ValueKind == JsonValueKind.Array)
        {
            PrintPositions = pp.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        }

        JsonElement? methods = await core.GetAsync("api/v1/client/lookups/print.method/options?culture=" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        if (methods is JsonElement ml && ml.ValueKind == JsonValueKind.Array)
        {
            PrintMethods = ml.EnumerateArray().Select(x => (
                Code: x.TryGetProperty("code", out JsonElement mc) ? mc.GetString() ?? "" : "",
                Name: x.TryGetProperty("displayName", out JsonElement mn) ? mn.GetString() ?? "" : ""))
                .Where(x => x.Code.Length > 0).ToList();
        }

        Quote = (await core.PostRawWithBodyAsync($"api/v1/client/orders/{OrderId}/pricing/preview", new { version = Version })).Value;

        JsonElement? validation = await core.GetAsync($"api/v1/client/orders/{OrderId}/validate");
        if (validation is JsonElement va && va.ValueKind == JsonValueKind.Array)
        {
            Issues = va.EnumerateArray().Select(x => new ValidationIssueRow(
                x.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "",
                x.TryGetProperty("field", out JsonElement fl) ? fl.GetString() : null)).ToList();
        }
        return Page();
    }

    private async Task<(bool Ok, string? Problem)> AddPrintOperationAsync(Guid orderId, long version, Guid? artworkVersionId, string? fileVersionId, string? notes)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        if (order is not JsonElement o || !o.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array) return (false, "order.not_found");
        JsonElement item = items.EnumerateArray().FirstOrDefault(x => !x.TryGetProperty("isActive", out JsonElement act) || act.ValueKind != JsonValueKind.False);
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("id", out JsonElement iid)) return (false, "order.item_required");
        Guid itemId = iid.GetGuid();

        (bool opOk, JsonElement? _, string? opProblem) = await core.PostRawWithBodyAsync(
            $"api/v1/client/orders/{orderId}/items/{itemId}/print-operations",
            new
            {
                sequence = 1,
                printPositionCode = PrintPosition,
                printMethodCode = string.IsNullOrWhiteSpace(PrintMethod) ? null : PrintMethod,
                width = Width,
                height = Height,
                artworkVersionId,
                fileVersionId,
                notes,
                version
            });
        return (opOk, opProblem);
    }

    private async Task<long> CurrentVersionAsync(Guid orderId)
    {
        JsonElement? order = await core.GetAsync($"api/v1/client/orders/{orderId}");
        return order is JsonElement o && o.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long n) ? n : 0;
    }

    public sealed record ValidationIssueRow(string Code, string? Field);
}
