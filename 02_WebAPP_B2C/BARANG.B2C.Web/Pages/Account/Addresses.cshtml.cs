using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class AddressesModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public bool HasProfile { get; private set; }
    public IReadOnlyList<AddressRow> Addresses { get; private set; } = [];
    public string? Error { get; private set; }

    [BindProperty] public string AddressTypeCode { get; set; } = "shipping";
    [BindProperty] public string Title { get; set; } = "";
    [BindProperty] public string CountryCode { get; set; } = "IR";
    [BindProperty] public string? District { get; set; }
    [BindProperty] public string AddressLine1 { get; set; } = "";
    [BindProperty] public string? AddressLine2 { get; set; }
    [BindProperty] public string? PostalCode { get; set; }
    [BindProperty] public string? RecipientName { get; set; }
    [BindProperty] public string? RecipientPhone { get; set; }
    [BindProperty] public string? RecipientMobile { get; set; }
    [BindProperty] public bool IsDefault { get; set; }

    public async Task OnGet(string? error)
    {
        Error = error;
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid)) return;
        HasProfile = true;
        JsonElement? list = await core.GetAsync($"api/v1/client/customers/{cid.GetString()}/addresses");
        if (list is JsonElement arr && arr.ValueKind == JsonValueKind.Array)
        {
            Addresses = arr.EnumerateArray()
                .Where(x => x.TryGetProperty("isActive", out JsonElement act) && act.ValueKind == JsonValueKind.True)
                .Select(x => new AddressRow(
                    x.TryGetProperty("id", out JsonElement id) && Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty,
                    x.TryGetProperty("addressTypeCode", out JsonElement tc) ? tc.GetString() ?? "" : "",
                    x.TryGetProperty("title", out JsonElement ti) ? ti.GetString() ?? "" : "",
                    x.TryGetProperty("countryCode", out JsonElement co) ? co.GetString() ?? "" : "",
                    x.TryGetProperty("provinceCode", out JsonElement pr) ? pr.GetString() : null,
                    x.TryGetProperty("cityCode", out JsonElement ci) ? ci.GetString() : null,
                    x.TryGetProperty("district", out JsonElement di) ? di.GetString() : null,
                    x.TryGetProperty("addressLine1", out JsonElement l1) ? l1.GetString() ?? "" : "",
                    x.TryGetProperty("addressLine2", out JsonElement l2) ? l2.GetString() : null,
                    x.TryGetProperty("postalCode", out JsonElement pc) ? pc.GetString() : null,
                    x.TryGetProperty("recipientName", out JsonElement rn) ? rn.GetString() : null,
                    x.TryGetProperty("recipientMobile", out JsonElement rm) ? rm.GetString() : null,
                    x.TryGetProperty("isDefault", out JsonElement df) && df.ValueKind == JsonValueKind.True,
                    x.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long ver) ? ver : 0)).ToList();
        }
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        (bool ok, string? problem) = await core.PostRawAsync($"api/v1/client/customers/{cid.GetString()}/addresses", new
        {
            addressTypeCode = AddressTypeCode,
            title = Title,
            countryCode = CountryCode,
            district = District,
            addressLine1 = AddressLine1,
            addressLine2 = AddressLine2,
            postalCode = PostalCode,
            recipientName = RecipientName,
            recipientPhone = RecipientPhone,
            recipientMobile = RecipientMobile,
            isDefault = IsDefault,
            displayOrder = 1
        });
        return RedirectToPage(new { error = ok ? null : problem ?? "failed" });
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, long version, string title, string addressLine1, string? addressLine2, string? postalCode, string? recipientName, string? recipientMobile, string addressTypeCode)
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        (bool ok, string? problem) = await core.PutRawAsync($"api/v1/client/customers/{cid.GetString()}/addresses/{id}", new
        {
            addressTypeCode,
            title,
            countryCode = "IR",
            addressLine1,
            addressLine2,
            postalCode,
            recipientName,
            recipientMobile,
            isDefault = false,
            displayOrder = 1,
            version
        });
        return RedirectToPage(new { error = ok ? null : problem ?? "failed" });
    }

    public async Task<IActionResult> OnPostSetDefaultAsync(Guid id, long version)
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        (bool ok, string? problem) = await core.PostRawAsync($"api/v1/client/customers/{cid.GetString()}/addresses/{id}/default", new { version });
        return RedirectToPage(new { error = ok ? null : problem ?? "failed" });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid id, long version)
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || !c.TryGetProperty("id", out JsonElement cid)) return RedirectToPage(new { error = "noprofile" });
        (bool ok, string? problem) = await core.PostRawAsync($"api/v1/client/customers/{cid.GetString()}/addresses/{id}/remove", new { version });
        return RedirectToPage(new { error = ok ? null : problem ?? "failed" });
    }

    public sealed record AddressRow(Guid Id, string TypeCode, string Title, string CountryCode, string? ProvinceCode, string? CityCode, string? District, string Line1, string? Line2, string? PostalCode, string? RecipientName, string? RecipientMobile, bool IsDefault, long Version)
    {
        public string Line => string.Join("، ", new[] { ProvinceCode, CityCode, District, Line1, Line2 }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }
}
