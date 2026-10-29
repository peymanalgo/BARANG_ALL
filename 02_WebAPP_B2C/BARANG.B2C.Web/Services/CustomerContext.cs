using System.Security.Claims;
using System.Text.Json;

namespace BARANG.B2C.Web.Services;

// Resolves the signed-in user to their B2C customer profile. The convention
// (until a dedicated user↔customer link ships) is: customer code == username,
// resolved through /client/customers/by-code/{code}. Profiles are created
// on demand from the account hub via the register endpoint.
public sealed class CustomerContext(CoreApiClient core, IHttpContextAccessor accessor)
{
    public string? Username => accessor.HttpContext?.User.FindFirstValue("barang:username");

    public async Task<JsonElement?> CurrentAsync()
    {
        string? username = Username;
        if (string.IsNullOrWhiteSpace(username)) return null;
        return await core.GetAsync($"api/v1/client/customers/by-code/{Uri.EscapeDataString(username)}");
    }

    public async Task<JsonElement?> RegisterSelfAsync(string firstName, string lastName, string mobile)
    {
        string? username = Username;
        if (string.IsNullOrWhiteSpace(username)) return null;
        string first = firstName.Trim();
        string last = lastName.Trim();
        return await core.PostAsync("api/v1/client/customers/register", new
        {
            customerCode = username,
            customerType = "Individual",
            firstName = first,
            lastName = last,
            displayName = $"{first} {last}".Trim(),
            primaryMobile = mobile.Trim()
        });
    }
}
