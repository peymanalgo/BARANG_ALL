using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BARANG.B2C.Web.Services;

// Thin backend-for-frontend client. The store never exposes the customer's
// bearer token to the browser: pages call this client server-side, which
// attaches the token stored in the auth cookie's claims. Client API
// endpoints are company-scoped, so the configured store company id is sent
// as X-Company-ID on every call (public catalog resolves its company itself).
public sealed class CoreApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string? CompanyId => configuration["CoreApi:CompanyId"];
    private HttpClient Create() => factory.CreateClient("core");

    public async Task<JsonElement?> GetAsync(string path)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<JsonElement?> PostAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<JsonElement?> PutAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<(bool Ok, string? ProblemCode)> PutRawAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            JsonElement? problem = await ReadJsonAsync(response);
            return (false, problem is JsonElement p && p.TryGetProperty("code", out JsonElement code) ? code.GetString() : null);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }

    public async Task<(bool Ok, string? ProblemCode)> PostRawAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        if (response.IsSuccessStatusCode) return (true, null);
        try
        {
            JsonElement? problem = await ReadJsonAsync(response);
            return (false, problem is JsonElement p && p.TryGetProperty("code", out JsonElement code) ? code.GetString() : null);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }

    public sealed record LoginOutcome(bool Ok, string? ProblemCode);

    public async Task<LoginOutcome> LoginAsync(string username, string password)
    {
        JsonElement? result = await PostAsync("api/v1/auth/login", new { username, password });
        if (result is not JsonElement login) return new(false, "invalid_credentials");
        string token = login.TryGetProperty("accessToken", out JsonElement at) ? at.GetString() ?? ""
            : login.TryGetProperty("token", out JsonElement tk) ? tk.GetString() ?? "" : "";
        if (token.Length == 0) return new(false, "invalid_credentials");
        JsonElement user = login.TryGetProperty("user", out JsonElement u) ? u : default;
        string userId = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "";
        string displayName = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? username : username;
        string usernameValue = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("username", out JsonElement un) ? un.GetString() ?? username : username;

        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("barang:username", usernameValue),
            new Claim("barang:token", token)
        ], CookieAuthenticationDefaults.AuthenticationScheme));
        HttpContext? context = accessor.HttpContext;
        if (context is null) return new(false, "no_context");
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
        return new(true, null);
    }

    public async Task LogoutAsync()
    {
        string? token = accessor.HttpContext?.User.FindFirstValue("barang:token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, "api/v1/auth/logout") { Content = Body(null) };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using HttpResponseMessage response = await Create().SendAsync(request);
            }
            catch (HttpRequestException) { /* best effort - the session expires server-side anyway */ }
        }
        if (accessor.HttpContext is { } context) await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static StringContent Body(object? body) => new(JsonSerializer.Serialize(body ?? new { }, Json), Encoding.UTF8, "application/json");

    private void AttachToken(HttpRequestMessage request)
    {
        string? token = accessor.HttpContext?.User.FindFirstValue("barang:token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(CompanyId)) request.Headers.Add("X-Company-ID", CompanyId);
    }

    private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, Json);
    }
}
