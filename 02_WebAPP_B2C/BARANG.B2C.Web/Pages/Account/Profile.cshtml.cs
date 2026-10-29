using System.Text.Json;
using BARANG.B2C.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BARANG.B2C.Web.Pages.Account;

[Authorize(Policy = "LoggedIn")]
public sealed class ProfileModel(CoreApiClient core, CustomerContext customers) : PageModel
{
    public bool HasProfile { get; private set; }
    public string? CustomerCode { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Error { get; private set; }
    public string? Saved { get; private set; }

    [BindProperty] public string? FirstName { get; set; }
    [BindProperty] public string? LastName { get; set; }
    [BindProperty] public string? DisplayNameInput { get; set; }
    [BindProperty] public string? Mobile { get; set; }
    [BindProperty] public string? PrimaryPhone { get; set; }
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? NationalId { get; set; }
    [BindProperty] public string? FatherName { get; set; }
    [BindProperty] public string? BirthDate { get; set; }
    public long Version { get; private set; }
    public long ProfileVersion { get; private set; }
    public int Completeness { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? error, string? saved)
    {
        Error = error;
        Saved = saved;
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid))
        {
            return RedirectToPage("Index", new { error = "noprofile" });
        }
        HasProfile = true;
        CustomerCode = c.TryGetProperty("customerCode", out JsonElement code) ? code.GetString() : null;
        DisplayName = c.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() : null;
        Version = c.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long cv) ? cv : 0;
        Mobile = c.TryGetProperty("primaryMobile", out JsonElement mo) ? mo.GetString() : null;
        PrimaryPhone = c.TryGetProperty("primaryPhone", out JsonElement ph) ? ph.GetString() : null;
        Email = c.TryGetProperty("primaryEmail", out JsonElement em) ? em.GetString() : null;
        if (c.TryGetProperty("individualProfile", out JsonElement ip) && ip.ValueKind == JsonValueKind.Object)
        {
            FirstName = ip.TryGetProperty("firstName", out JsonElement fn) ? fn.GetString() : null;
            LastName = ip.TryGetProperty("lastName", out JsonElement ln) ? ln.GetString() : null;
            NationalId = ip.TryGetProperty("nationalId", out JsonElement ni) ? ni.GetString() : null;
            FatherName = ip.TryGetProperty("fatherName", out JsonElement fa) ? fa.GetString() : null;
            BirthDate = ip.TryGetProperty("birthDate", out JsonElement bd) && bd.ValueKind == JsonValueKind.String ? bd.GetString() : null;
            ProfileVersion = ip.TryGetProperty("version", out JsonElement pv) && pv.TryGetInt64(out long pvv) ? pvv : 0;
        }
        int filled = 0;
        if (!string.IsNullOrWhiteSpace(FirstName)) filled++;
        if (!string.IsNullOrWhiteSpace(LastName)) filled++;
        if (!string.IsNullOrWhiteSpace(Mobile)) filled++;
        if (!string.IsNullOrWhiteSpace(Email)) filled++;
        if (!string.IsNullOrWhiteSpace(NationalId)) filled++;
        Completeness = filled * 100 / 5;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        JsonElement? customer = await customers.CurrentAsync();
        if (customer is not JsonElement c || c.ValueKind != JsonValueKind.Object || !c.TryGetProperty("id", out JsonElement cid))
        {
            return RedirectToPage(new { error = "noprofile" });
        }
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Mobile))
        {
            return RedirectToPage(new { error = "fields" });
        }
        long version = c.TryGetProperty("version", out JsonElement v) && v.TryGetInt64(out long cv) ? cv : 0;
        long profileVersion = 0;
        if (c.TryGetProperty("individualProfile", out JsonElement ip) && ip.ValueKind == JsonValueKind.Object && ip.TryGetProperty("version", out JsonElement pv) && pv.TryGetInt64(out long pvv)) profileVersion = pvv;
        string display = string.IsNullOrWhiteSpace(DisplayNameInput) ? $"{FirstName.Trim()} {LastName.Trim()}" : DisplayNameInput.Trim();

        (bool ok, string? problem) = await core.PutRawAsync($"api/v1/client/customers/{cid.GetString()}", new
        {
            displayName = display,
            firstName = FirstName.Trim(),
            lastName = LastName.Trim(),
            nationalId = string.IsNullOrWhiteSpace(NationalId) ? null : NationalId.Trim(),
            primaryMobile = Mobile.Trim(),
            primaryPhone = string.IsNullOrWhiteSpace(PrimaryPhone) ? null : PrimaryPhone.Trim(),
            primaryEmail = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            fatherName = string.IsNullOrWhiteSpace(FatherName) ? null : FatherName.Trim(),
            birthDate = string.IsNullOrWhiteSpace(BirthDate) ? null : BirthDate.Trim(),
            version,
            profileVersion
        });
        return RedirectToPage(new { saved = ok ? "1" : null, error = ok ? null : problem ?? "failed" });
    }
}
