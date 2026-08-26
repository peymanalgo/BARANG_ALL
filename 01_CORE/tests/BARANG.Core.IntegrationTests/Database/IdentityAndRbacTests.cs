using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class IdentityAndRbacTests(PostgreSqlDatabaseFixture database)
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task AuthenticatedSelfServiceContextEnforcesCompanyIsolationAndLifecycle()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyA = await AddCompanyAsync("SELF_A");
        Guid companyB = await AddCompanyAsync("SELF_B");
        Guid companyC = await AddCompanyAsync("SELF_C");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken)).StatusCode);
        await BootstrapAsync(client, companyA);
        LoginResult admin = await LoginAsync(client, "admin", "StrongAdminPassword123");
        UserView single = await CreateUserAsync(client, admin, companyA, "single");
        UserView multiple = await CreateUserAsync(client, admin, companyA, "multiple");
        UserView companyBOnly = await CreateUserAsync(client, admin, companyA, "bonly");
        UserView zero = await CreateUserAsync(client, admin, companyA, "zero");
        UserView expired = await CreateUserAsync(client, admin, companyA, "expired");
        await AddCompanyAccessAsync(single.Id, companyA);
        await AddCompanyAccessAsync(multiple.Id, companyA);
        await AddCompanyAccessAsync(multiple.Id, companyB);
        await AddCompanyAccessAsync(companyBOnly.Id, companyB);
        await AddCompanyAccessAsync(expired.Id, companyC, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1));
        await AssertSelfReadContextDoesNotLeakAsync(single.Id);

        LoginResult singleLogin = await LoginAsync(client, "single", "StrongUserPassword123");
        using HttpResponseMessage meResponse = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/auth/me", singleLogin.AccessToken, null), TestContext.Current.CancellationToken);
        meResponse.EnsureSuccessStatusCode();
        string meJson = await meResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("password", meJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", meJson, StringComparison.OrdinalIgnoreCase);
        CurrentUserView me = JsonSerializer.Deserialize<CurrentUserView>(meJson, WebJson)!;
        Assert.Equal(single.Id, me.UserId);
        Assert.Null(me.CurrentCompanyId);
        Assert.Empty(me.EffectiveRoleCodes);

        MyCompanyView[] singleCompanies = await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", singleLogin.AccessToken, null));
        Assert.Equal(companyA, Assert.Single(singleCompanies).CompanyId);
        CompanySelectionView selected = await SendAsync<CompanySelectionView>(client, Request(HttpMethod.Post, "/api/v1/auth/me/company", singleLogin.AccessToken, null, new { companyId = companyA }));
        Assert.Equal(companyA, selected.CompanyId);
        CurrentUserView selectedMe = await SendAsync<CurrentUserView>(client, Request(HttpMethod.Get, "/api/v1/auth/me", singleLogin.AccessToken, companyA));
        Assert.Equal(companyA, selectedMe.CurrentCompanyId);

        LoginResult multipleLogin = await LoginAsync(client, "multiple", "StrongUserPassword123");
        MyCompanyView[] multipleCompanies = await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", multipleLogin.AccessToken, null));
        Assert.Equal(new[] { companyA, companyB }.Order().ToArray(), multipleCompanies.Select(x => x.CompanyId).Order().ToArray());

        LoginResult bOnlyLogin = await LoginAsync(client, "bonly", "StrongUserPassword123");
        MyCompanyView[] bOnlyCompanies = await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", bOnlyLogin.AccessToken, null));
        Assert.Equal(companyB, Assert.Single(bOnlyCompanies).CompanyId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/me/company", bOnlyLogin.AccessToken, null, new { companyId = companyA }), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/auth/me", bOnlyLogin.AccessToken, companyA), TestContext.Current.CancellationToken)).StatusCode);

        LoginResult zeroLogin = await LoginAsync(client, "zero", "StrongUserPassword123");
        Assert.Empty(await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", zeroLogin.AccessToken, null)));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/me/company", zeroLogin.AccessToken, null, new { companyId = companyA }), TestContext.Current.CancellationToken)).StatusCode);

        LoginResult expiredLogin = await LoginAsync(client, "expired", "StrongUserPassword123");
        Assert.Empty(await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", expiredLogin.AccessToken, null)));

        using HttpRequestMessage deactivate = Request(HttpMethod.Put, $"/api/v1/organization/companies/{companyB}/activation", admin.AccessToken, companyA, new { isActive = false, version = 1 });
        (await client.SendAsync(deactivate, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/me/company", multipleLogin.AccessToken, null, new { companyId = companyB }), TestContext.Current.CancellationToken)).StatusCode);
        Assert.DoesNotContain((await SendAsync<MyCompanyView[]>(client, Request(HttpMethod.Get, "/api/v1/auth/me/companies", multipleLogin.AccessToken, null))), x => x.CompanyId == companyB);

        using HttpRequestMessage logout = Request(HttpMethod.Post, "/api/v1/auth/logout", singleLogin.AccessToken, null);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/auth/me", singleLogin.AccessToken, null), TestContext.Current.CancellationToken)).StatusCode);
    }

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
    private static async Task BootstrapAsync(HttpClient client, Guid companyId) { using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId, username = "admin", displayName = "Administrator", password = "StrongAdminPassword123" }) }; request.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789"); (await client.SendAsync(request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode(); }
    private static Task<UserView> CreateUserAsync(HttpClient client, LoginResult admin, Guid companyId, string username) => SendAsync<UserView>(client, Request(HttpMethod.Post, "/api/v1/security/users", admin.AccessToken, companyId, new { username, displayName = username, password = "StrongUserPassword123" }));
    private async Task AddCompanyAccessAsync(Guid userId, Guid companyId, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        await using NpgsqlConnection connection = new(database.ConnectionString); await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using (NpgsqlCommand context = new("SELECT set_config('barang.company_id', @company, true);", connection, transaction)) { context.Parameters.AddWithValue("company", companyId.ToString()); await context.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); }
        await using NpgsqlCommand insert = new("INSERT INTO barang.user_companies (id, user_id, company_id, valid_from_utc, valid_to_utc, granted_at_utc, granted_by_user_id) VALUES (@id, @user, @company, @from, @to, now(), NULL);", connection, transaction);
        insert.Parameters.AddWithValue("id", Guid.NewGuid()); insert.Parameters.AddWithValue("user", userId); insert.Parameters.AddWithValue("company", companyId); insert.Parameters.AddWithValue("from", from ?? DateTimeOffset.UtcNow.AddMinutes(-1)); insert.Parameters.AddWithValue("to", (object?)to ?? DBNull.Value); await insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); await transaction.CommitAsync(TestContext.Current.CancellationToken);
    }
    private async Task AssertSelfReadContextDoesNotLeakAsync(Guid userId)
    {
        await using NpgsqlConnection connection = new(database.ConnectionString); await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (NpgsqlTransaction first = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await using NpgsqlCommand setUser = new("SELECT set_config('barang.user_id', @user, true);", connection, first); setUser.Parameters.AddWithValue("user", userId.ToString()); await setUser.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand visible = new("SELECT count(*) FROM barang.user_companies;", connection, first); Assert.Equal(1, Convert.ToInt32(await visible.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture)); await first.CommitAsync(TestContext.Current.CancellationToken);
        }
        await using NpgsqlTransaction second = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand hidden = new("SELECT count(*) FROM barang.user_companies;", connection, second); Assert.Equal(0, Convert.ToInt32(await hidden.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture));
    }
    private static HttpRequestMessage Request(HttpMethod method, string path, string? token, Guid? company, object? body = null) { HttpRequestMessage request = new(method, path); if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); if (company is not null) request.Headers.Add("X-Company-ID", company.ToString()); if (body is not null) request.Content = JsonContent.Create(body); return request; }
    private static async Task<LoginResult> LoginAsync(HttpClient client, string username, string password) { using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<LoginResult>(TestContext.Current.CancellationToken))!; }
    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request) { using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!; }
}
