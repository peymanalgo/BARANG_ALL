using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class IdentityAndRbacTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task ProductionRejectsAnonymousDevelopmentCompanyHeader()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyId = await AddCompanyAsync("PROD_HEADER");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString, "Production");
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = Request(HttpMethod.Get, "/health/live", null, companyId);

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BootstrapLoginAuthorizationCompanyScopeAndRevocationAreEnforced()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyA = await AddCompanyAsync("SEC_A"); Guid companyB = await AddCompanyAsync("SEC_B");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString); using HttpClient client = factory.CreateClient();
        using HttpRequestMessage anonymous = Request(HttpMethod.Get, "/api/v1/security/users", null, companyA);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(anonymous, TestContext.Current.CancellationToken)).StatusCode);

        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = companyA, username = "admin", displayName = "Administrator", email = "admin@example.test", password = "StrongAdminPassword123" }) };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        using HttpResponseMessage bootstrapped = await client.SendAsync(bootstrap, TestContext.Current.CancellationToken); bootstrapped.EnsureSuccessStatusCode();
        string bootstrapJson = await bootstrapped.Content.ReadAsStringAsync(TestContext.Current.CancellationToken); Assert.DoesNotContain("passwordHash", bootstrapJson, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("StrongAdminPassword123", bootstrapJson, StringComparison.Ordinal);
        using HttpResponseMessage secondBootstrap = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = companyA, username = "other", displayName = "Other", password = "StrongOtherPassword123" }), Headers = { { "X-Bootstrap-Secret", "test-bootstrap-secret-123456789" } } }, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Conflict, secondBootstrap.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "WrongPassword123" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "missing", password = "WrongPassword123" }, TestContext.Current.CancellationToken)).StatusCode);
        LoginResult admin = await LoginAsync(client, "admin", "StrongAdminPassword123");
        using HttpRequestMessage allowed = Request(HttpMethod.Get, "/api/v1/security/users", admin.AccessToken, companyA); using HttpResponseMessage allowedResponse = await client.SendAsync(allowed, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode); Assert.DoesNotContain("passwordHash", await allowedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
        using HttpRequestMessage crossCompany = Request(HttpMethod.Get, "/api/v1/security/users", admin.AccessToken, companyB); Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossCompany, TestContext.Current.CancellationToken)).StatusCode);

        UserView low = await SendAsync<UserView>(client, Request(HttpMethod.Post, "/api/v1/security/users", admin.AccessToken, companyA, new { username = "limited", displayName = "Limited", password = "StrongLimitedPassword123" }));
        await SendAsync<UserCompanyView>(client, Request(HttpMethod.Post, $"/api/v1/security/users/{low.Id}/companies", admin.AccessToken, companyA, new { companyId = companyA }));
        LoginResult limited = await LoginAsync(client, "limited", "StrongLimitedPassword123");
        using HttpRequestMessage forbidden = Request(HttpMethod.Get, "/api/v1/security/users", limited.AccessToken, companyA); Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(forbidden, TestContext.Current.CancellationToken)).StatusCode);
        using HttpRequestMessage cannotGrant = Request(HttpMethod.Post, "/api/v1/security/roles", limited.AccessToken, companyA, new { code = "unsafe.role", displayTitle = "Unsafe" }); Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(cannotGrant, TestContext.Current.CancellationToken)).StatusCode);

        using HttpRequestMessage deactivate = Request(HttpMethod.Put, $"/api/v1/security/users/{low.Id}/activation", admin.AccessToken, companyA, new { isActive = false, version = low.Version }); (await client.SendAsync(deactivate, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "limited", password = "StrongLimitedPassword123" }, TestContext.Current.CancellationToken)).StatusCode);
        using HttpRequestMessage rolesRequest = Request(HttpMethod.Get, "/api/v1/security/roles", admin.AccessToken, companyA); RoleView[] roles = await SendAsync<RoleView[]>(client, rolesRequest); RoleView administratorRole = Assert.Single(roles);
        using HttpRequestMessage deactivateRole = Request(HttpMethod.Put, $"/api/v1/security/roles/{administratorRole.Id}/activation", admin.AccessToken, companyA, new { isActive = false, version = administratorRole.Version }); (await client.SendAsync(deactivateRole, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        using HttpRequestMessage inactiveRole = Request(HttpMethod.Get, "/api/v1/security/users", admin.AccessToken, companyA); Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(inactiveRole, TestContext.Current.CancellationToken)).StatusCode);
        using HttpRequestMessage logout = Request(HttpMethod.Post, "/api/v1/auth/logout", admin.AccessToken, null); Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout, TestContext.Current.CancellationToken)).StatusCode);
        using HttpRequestMessage revoked = Request(HttpMethod.Get, "/api/v1/security/users", admin.AccessToken, companyA); Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(revoked, TestContext.Current.CancellationToken)).StatusCode);
    }

    private async Task<Guid> AddCompanyAsync(string code) { await using var context = database.CreateCoreDbContext(); Company company = Company.Create(Guid.NewGuid(), code, code, DateTimeOffset.UtcNow).Value; context.Companies.Add(company); await context.SaveChangesAsync(TestContext.Current.CancellationToken); return company.Id; }
    private static HttpRequestMessage Request(HttpMethod method, string path, string? token, Guid? company, object? body = null) { HttpRequestMessage request = new(method, path); if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); if (company is not null) request.Headers.Add("X-Company-ID", company.ToString()); if (body is not null) request.Content = JsonContent.Create(body); return request; }
    private static async Task<LoginResult> LoginAsync(HttpClient client, string username, string password) { using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<LoginResult>(TestContext.Current.CancellationToken))!; }
    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request) { using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!; }
}
