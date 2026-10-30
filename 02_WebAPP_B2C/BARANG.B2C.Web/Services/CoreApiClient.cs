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

    // The API session token (inside the auth cookie) expires after 30 minutes;
    // when a call comes back 401 the caller should sign the user out and send
    // them to login again.
    public bool SawUnauthorized => accessor.HttpContext?.Items.ContainsKey("core:401") == true;
    private void MarkStatus(int statusCode)
    {
        if (statusCode == 401) accessor.HttpContext?.Items["core:401"] = true;
    }

    public async Task<JsonElement?> GetAsync(string path)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
        if (!response.IsSuccessStatusCode && (int)response.StatusCode == 401)
        {
            // Expired token: retry anonymously so public endpoints still work.
            using HttpRequestMessage anonymous = new(HttpMethod.Get, path);
            using HttpResponseMessage retry = await Create().SendAsync(anonymous);
            if (retry.IsSuccessStatusCode) return await ReadJsonAsync(retry);
            return null;
        }
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<JsonElement?> PostAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<JsonElement?> PutAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
        return response.IsSuccessStatusCode ? await ReadJsonAsync(response) : null;
    }

    public async Task<(bool Ok, string? ProblemCode)> PutRawAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
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

    public async Task<(bool Ok, JsonElement? Value, string? ProblemCode)> PostRawWithBodyAsync(string path, object? body, string? idempotencyKey = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = Body(body) };
        AttachToken(request);
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) request.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
        if (response.IsSuccessStatusCode) return (true, await ReadJsonAsync(response), null);
        try
        {
            JsonElement? problem = await ReadJsonAsync(response);
            return (false, null, problem is JsonElement p && p.TryGetProperty("code", out JsonElement code) ? code.GetString() : null);
        }
        catch (JsonException)
        {
            return (false, null, null);
        }
    }

    public async Task<(bool Ok, string? ProblemCode)> PostRawAsync(string path, object? body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = Body(body) };
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
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

    public async Task<(bool Ok, JsonElement? Value, string? ProblemCode)> PostMultipartAsync(string path, string fileName, string contentType, Stream content, long length, string? idempotencyKey = null, IDictionary<string, string>? fields = null)
    {
        using MultipartFormDataContent form = new();
        StreamContent file = new(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        file.Headers.ContentLength = length;
        form.Add(file, "file", fileName);
        if (fields is not null)
        {
            foreach ((string name, string value) in fields)
            {
                StringContent field = new(value);
                form.Add(field, name);
            }
        }
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = form };
        AttachToken(request);
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) request.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
        if (response.IsSuccessStatusCode) return (true, await ReadJsonAsync(response), null);
        try
        {
            JsonElement? problem = await ReadJsonAsync(response);
            return (false, null, problem is JsonElement p && p.TryGetProperty("code", out JsonElement code) ? code.GetString() : null);
        }
        catch (JsonException)
        {
            return (false, null, null);
        }
    }

    public async Task<(bool Ok, string? ProblemCode)> DeleteAsync(string path)
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, path);
        AttachToken(request);
        using HttpResponseMessage response = await Create().SendAsync(request);
        MarkStatus((int)response.StatusCode);
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
        return await SignInFromLoginAsync(login, username);
    }

    // OTP (mocked delivery): request has no meaningful response beyond "sent";
    // register/login return the same LoginResult shape as password login.
    public async Task<LoginOutcome> OtpRequestAsync(string target)
    {
        JsonElement? result = await PostAsync("api/v1/auth/otp/request", new { target });
        return new(result is not null, result is null ? "otp_request_failed" : null);
    }

    public async Task<LoginOutcome> OtpRegisterAsync(string target, string code, string firstName, string lastName)
    {
        (bool ok, JsonElement? value, string? problem) = await PostRawWithBodyAsync("api/v1/auth/otp/register", new { target, code, firstName, lastName });
        if (!ok) return new(false, problem ?? "otp_invalid");
        return await SignInFromLoginAsync(value, target);
    }

    public async Task<LoginOutcome> OtpLoginAsync(string target, string code)
    {
        (bool ok, JsonElement? value, string? problem) = await PostRawWithBodyAsync("api/v1/auth/otp/login", new { target, code });
        if (!ok) return new(false, problem ?? "otp_invalid");
        return await SignInFromLoginAsync(value, target);
    }

    private async Task<LoginOutcome> SignInFromLoginAsync(JsonElement? payload, string fallbackUsername)
    {
        if (payload is not JsonElement login) return new(false, "invalid_credentials");
        string token = login.TryGetProperty("accessToken", out JsonElement at) ? at.GetString() ?? ""
            : login.TryGetProperty("token", out JsonElement tk) ? tk.GetString() ?? "" : "";
        if (token.Length == 0) return new(false, "invalid_credentials");
        JsonElement user = login.TryGetProperty("user", out JsonElement u) ? u : default;
        string userId = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "";
        string displayName = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("displayName", out JsonElement dn) ? dn.GetString() ?? fallbackUsername : fallbackUsername;
        string usernameValue = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("username", out JsonElement un) ? un.GetString() ?? fallbackUsername : fallbackUsername;

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
        MarkStatus((int)response.StatusCode);
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
