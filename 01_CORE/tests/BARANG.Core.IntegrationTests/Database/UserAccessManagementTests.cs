using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Auditing;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class UserAccessManagementTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task CompanySelectionRejectsMissingAccessAndInactiveMembershipWithExplicitProblemCodes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await database.ResetAsync(cancellationToken);
        Guid accessible = await AddCompany("SELECT_ALLOWED");
        Guid inaccessible = await AddCompany("SELECT_FORBIDDEN");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        LoginResult admin = await BootstrapAndLogin(client, accessible);

        using HttpResponseMessage forbidden = await client.SendAsync(
            Request(HttpMethod.Post, "/api/v1/auth/me/company", admin.AccessToken, null, new { companyId = inaccessible }),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, forbidden.StatusCode);

        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            Company company = await db.Companies.SingleAsync(x => x.Id == accessible, cancellationToken);
            company.Deactivate(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        using HttpResponseMessage inactive = await client.SendAsync(
            Request(HttpMethod.Post, "/api/v1/auth/me/company", admin.AccessToken, null, new { companyId = accessible }),
            cancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inactive.StatusCode);
        Assert.Contains("security.company.inactive", await inactive.Content.ReadAsStringAsync(cancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrationAddsDirectGrantsGranularCatalogRlsAndUniqueness()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*) FROM information_schema.tables WHERE table_schema='barang' AND table_name='user_permissions'),
              (SELECT count(*) FROM barang.permissions WHERE id BETWEEN '00000000-0000-0000-0000-000000000071'::uuid AND '00000000-0000-0000-0000-000000000080'::uuid),
              (SELECT count(*) FROM pg_policies WHERE schemaname='barang' AND tablename='user_permissions'),
              (SELECT count(*) FROM pg_indexes WHERE schemaname='barang' AND tablename='user_permissions' AND indexname='uq_user_permissions_user_id_permission_id_company_id'),
              has_table_privilege('barang_app','barang.user_permissions','SELECT'),
              has_table_privilege('barang_app','barang.user_permissions','INSERT'),
              has_table_privilege('barang_app','barang.user_permissions','DELETE'),
              has_table_privilege('barang_app','barang.user_permissions','UPDATE');
            """;
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, reader.GetInt64(0));
        Assert.Equal(10, reader.GetInt64(1));
        Assert.Equal(2, reader.GetInt64(2));
        Assert.Equal(1, reader.GetInt64(3));
        Assert.True(reader.GetBoolean(4));
        Assert.True(reader.GetBoolean(5));
        Assert.True(reader.GetBoolean(6));
        Assert.False(reader.GetBoolean(7));
        await reader.DisposeAsync();

        Guid owningCompany = await AddCompany("UAM_RLS_A");
        Guid otherCompany = await AddCompany("UAM_RLS_B");
        Guid person = await AddPerson(owningCompany, "P-RLS", "Rls", "Subject", null);
        Guid userId = Guid.NewGuid();
        await using (CoreDbContext administration = database.CreateAdministrationCoreDbContext())
        {
            ApplicationUser user = ApplicationUser.CreateIdentity(userId, person, "rls.subject", "Rls Subject", null, DateTimeOffset.UtcNow).Value;
            user.SetPasswordHash("not-a-plaintext-password", DateTimeOffset.UtcNow);
            administration.ApplicationUsers.Add(user);
            administration.UserPermissions.Add(UserPermission.Create(userId, Guid.Parse("00000000-0000-0000-0000-000000000071"), owningCompany, null, DateTimeOffset.UtcNow).Value);
            await administration.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        NpgsqlConnectionStringBuilder applicationConnectionString = new(database.ConnectionString) { Username = "barang_app", Password = "barang_app_dev_only" };
        await using NpgsqlConnection applicationConnection = new(applicationConnectionString.ConnectionString);
        await applicationConnection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlTransaction transaction = await applicationConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand applicationCommand = applicationConnection.CreateCommand();
        applicationCommand.Transaction = transaction;
        applicationCommand.CommandText = $"SELECT set_config('barang.user_id', '{userId}', true); SELECT set_config('barang.company_id', '{otherCompany}', true);";
        await applicationCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        applicationCommand.CommandText = "SELECT count(*) FROM barang.user_permissions";
        Assert.Equal(0L, (long)(await applicationCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
        applicationCommand.CommandText = $"SELECT set_config('barang.company_id', '{owningCompany}', true);";
        await applicationCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        applicationCommand.CommandText = "SELECT count(*) FROM barang.user_permissions WHERE user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid";
        Assert.Equal(1L, (long)(await applicationCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
        await transaction.RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExistingPersonCreatesOneIdentityAccountWithHashedPasswordAndSafeApi()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = await AddCompany("UAM_PERSON_A"), other = await AddCompany("UAM_PERSON_B");
        Guid person = await AddPerson(company, "P-100", "Sara", "Operator", "09120000001");
        Guid second = await AddPerson(company, "P-101", "Second", "Operator", null);
        _ = await AddPerson(other, "P-OTHER", "Hidden", "Person", null);
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        LoginResult admin = await BootstrapAndLogin(client, company);

        using HttpResponseMessage emptyState = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/security/users/search?accountState=", admin.AccessToken, company), ct);
        Assert.Equal(HttpStatusCode.OK, emptyState.StatusCode);
        using HttpResponseMessage invalidState = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/security/users/search?accountState=unknown", admin.AccessToken, company), ct);
        Assert.Equal(HttpStatusCode.BadRequest, invalidState.StatusCode);

        JsonElement candidates = await Send(client, Request(HttpMethod.Get, "/api/v1/security/users/person-candidates?search=Sara", admin.AccessToken, company));
        JsonElement candidate = Assert.Single(candidates.EnumerateArray());
        Assert.Equal(person, candidate.GetProperty("person").GetProperty("id").GetGuid());
        Assert.False(candidate.GetProperty("hasUserAccount").GetBoolean());

        using HttpResponseMessage weak = await client.SendAsync(Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = person, username = "sara.operator", password = "weak", enabled = true }), ct);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        const string password = "StrongSaraPassword123";
        JsonElement created = await Send(client, Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = person, username = "sara.operator", password, enabled = true }));
        Guid userId = created.GetProperty("id").GetGuid();
        Assert.Equal(person, created.GetProperty("personId").GetGuid());
        Assert.DoesNotContain("password", created.ToString(), StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage duplicatePerson = await client.SendAsync(Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = person, username = "sara.second", password, enabled = true }), ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicatePerson.StatusCode);
        using HttpResponseMessage duplicateUsername = await client.SendAsync(Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = second, username = "sara.operator", password, enabled = true }), ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicateUsername.StatusCode);

        JsonElement linkedCandidate = await Send(client, Request(HttpMethod.Get, "/api/v1/security/users/person-candidates?search=Sara", admin.AccessToken, company));
        Assert.True(Assert.Single(linkedCandidate.EnumerateArray()).GetProperty("hasUserAccount").GetBoolean());
        JsonElement detail = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{userId}", admin.AccessToken, company));
        Assert.Equal("Sara Operator", detail.GetProperty("person").GetProperty("fullName").GetString());
        Assert.DoesNotContain("passwordHash", detail.ToString(), StringComparison.OrdinalIgnoreCase);

        await using CoreDbContext verification = database.CreateAdministrationCoreDbContext();
        ApplicationUser stored = await verification.ApplicationUsers.SingleAsync(x => x.Id == userId, ct);
        Assert.Equal(person, stored.PersonId);
        Assert.StartsWith("AQAAAA", stored.PasswordHash, StringComparison.Ordinal);
        Assert.DoesNotContain(password, stored.PasswordHash, StringComparison.Ordinal);
        Assert.Single(await verification.UserCompanies.IgnoreQueryFilters().Where(x => x.UserId == userId && x.CompanyId == company).ToListAsync(ct));
        Assert.True(await verification.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "security.user.created" && x.EntityId == userId.ToString(), ct));
    }

    [Fact]
    public async Task IdentityPasswordResetDisableAndLockoutUseSecureAccountOperations()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = await AddCompany("UAM_IDENTITY");
        Guid person = await AddPerson(company, "P-150", "Identity", "User", null);
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        LoginResult admin = await BootstrapAndLogin(client, company);
        const string oldPassword = "StrongIdentityPassword123";
        const string newPassword = "NewStrongIdentityPassword456";
        JsonElement created = await Send(client, Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = person, username = "identity.user", password = oldPassword, enabled = true }));
        Guid userId = created.GetProperty("id").GetGuid();

        _ = await Login(client, "identity.user", oldPassword);
        using HttpResponseMessage weakReset = await client.SendAsync(Request(HttpMethod.Put, $"/api/v1/security/users/{userId}/password", admin.AccessToken, company, new { password = "weak" }), ct);
        Assert.Equal(HttpStatusCode.BadRequest, weakReset.StatusCode);
        using HttpResponseMessage reset = await client.SendAsync(Request(HttpMethod.Put, $"/api/v1/security/users/{userId}/password", admin.AccessToken, company, new { password = newPassword }), ct);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "identity.user", password = oldPassword }, ct)).StatusCode);
        _ = await Login(client, "identity.user", newPassword);

        JsonElement detail = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{userId}", admin.AccessToken, company));
        JsonElement disabled = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/users/{userId}/activation", admin.AccessToken, company, new { isActive = false, version = detail.GetProperty("account").GetProperty("version").GetInt64() }));
        Assert.Equal("Inactive", disabled.GetProperty("accountState").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "identity.user", password = newPassword }, ct)).StatusCode);
        JsonElement enabled = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/users/{userId}/activation", admin.AccessToken, company, new { isActive = true, version = disabled.GetProperty("version").GetInt64() }));
        Assert.Equal("Active", enabled.GetProperty("accountState").GetString());

        for (int attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "identity.user", password = "WrongIdentityPassword999" }, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "identity.user", password = newPassword }, ct)).StatusCode);
        JsonElement lockedDetail = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{userId}", admin.AccessToken, company));
        Assert.Equal("Locked", lockedDetail.GetProperty("account").GetProperty("accountState").GetString());
        JsonElement unlocked = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/users/{userId}/unlock", admin.AccessToken, company, new { version = lockedDetail.GetProperty("account").GetProperty("version").GetInt64() }));
        Assert.Equal("Active", unlocked.GetProperty("accountState").GetString());
        _ = await Login(client, "identity.user", newPassword);

        await using CoreDbContext verification = database.CreateAdministrationCoreDbContext();
        ApplicationUser stored = await verification.ApplicationUsers.SingleAsync(x => x.Id == userId, ct);
        Assert.DoesNotContain(oldPassword, stored.PasswordHash, StringComparison.Ordinal);
        Assert.DoesNotContain(newPassword, stored.PasswordHash, StringComparison.Ordinal);
        Assert.True(await verification.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "security.user.password_reset" && x.EntityId == userId.ToString(), ct));
        Assert.True(await verification.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "security.user.deactivated" && x.EntityId == userId.ToString(), ct));
        Assert.True(await verification.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "security.user.unlocked" && x.EntityId == userId.ToString(), ct));
    }

    [Fact]
    public async Task DirectInheritedAndEffectivePermissionsMatchRuntimeAuthorization()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = await AddCompany("UAM_EFFECTIVE");
        Guid person = await AddPerson(company, "P-200", "Effective", "User", null);
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        LoginResult admin = await BootstrapAndLogin(client, company);
        JsonElement created = await Send(client, Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = person, username = "effective.user", password = "StrongEffectivePassword123", enabled = true }));
        Guid user = created.GetProperty("id").GetGuid();
        JsonElement role = await Send(client, Request(HttpMethod.Post, "/api/v1/security/roles", admin.AccessToken, company, new { code = "uam.operator", displayTitle = "UAM operator", description = "Integration role" }));
        Guid roleId = role.GetProperty("id").GetGuid();
        JsonElement catalog = await Send(client, Request(HttpMethod.Get, "/api/v1/security/permissions", admin.AccessToken, company));
        Guid inheritedPermission = catalog.EnumerateArray().Single(x => x.GetProperty("code").GetString() == SecurityPermissions.SecurityUserRead).GetProperty("id").GetGuid();
        Guid directPermission = catalog.EnumerateArray().Single(x => x.GetProperty("code").GetString() == SecurityPermissions.AuditRead).GetProperty("id").GetGuid();

        JsonElement roleTarget = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/roles/{roleId}/permission-target", admin.AccessToken, company));
        _ = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/roles/{roleId}/permissions", admin.AccessToken, company, new { version = roleTarget.GetProperty("version").GetInt64(), permissionIds = new[] { inheritedPermission } }));
        _ = await Send(client, Request(HttpMethod.Post, $"/api/v1/security/users/{user}/roles", admin.AccessToken, company, new { roleId, companyId = company }));
        using HttpResponseMessage duplicateRole = await client.SendAsync(Request(HttpMethod.Post, $"/api/v1/security/users/{user}/roles", admin.AccessToken, company, new { roleId, companyId = company }), ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicateRole.StatusCode);

        JsonElement userDetail = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{user}", admin.AccessToken, company));
        JsonElement saved = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/users/{user}/permissions", admin.AccessToken, company, new { version = userDetail.GetProperty("account").GetProperty("version").GetInt64(), permissionIds = new[] { directPermission } }));
        PermissionState inherited = State(saved, inheritedPermission); PermissionState direct = State(saved, directPermission);
        Assert.False(inherited.Direct); Assert.True(inherited.Inherited); Assert.True(inherited.Effective);
        Assert.True(direct.Direct); Assert.False(direct.Inherited); Assert.True(direct.Effective);

        LoginResult userLogin = await Login(client, "effective.user", "StrongEffectivePassword123");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/security/users", userLogin.AccessToken, company), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/audit/events?pageSize=10", userLogin.AccessToken, company), ct)).StatusCode);

        // Successful login updates security metadata/version; reload the target before a later admin edit.
        JsonElement currentTarget = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{user}/permission-target", admin.AccessToken, company));
        JsonElement removed = await Send(client, Request(HttpMethod.Put, $"/api/v1/security/users/{user}/permissions", admin.AccessToken, company, new { version = currentTarget.GetProperty("version").GetInt64(), permissionIds = Array.Empty<Guid>() }));
        inherited = State(removed, inheritedPermission); direct = State(removed, directPermission);
        Assert.True(inherited.Inherited); Assert.True(inherited.Effective);
        Assert.False(direct.Direct); Assert.False(direct.Effective);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/security/users", userLogin.AccessToken, company), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/audit/events?pageSize=10", userLogin.AccessToken, company), ct)).StatusCode);

        await using CoreDbContext audit = database.CreateAdministrationCoreDbContext();
        Assert.True(await audit.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "security.role_permissions.changed" && x.EntityId == roleId.ToString(), ct));
        Assert.Equal(2, await audit.AuditEvents.IgnoreQueryFilters().CountAsync(x => x.Action == "security.user_direct_permissions.changed" && x.EntityId == user.ToString(), ct));
    }

    [Fact]
    public async Task CriticalSystemAdministratorAndCrossCompanyTargetsAreProtected()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = await AddCompany("UAM_PROTECT_A"), other = await AddCompany("UAM_PROTECT_B");
        Guid foreignPerson = await AddPerson(other, "P-FOREIGN", "Foreign", "Person", null);
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        LoginResult admin = await BootstrapAndLogin(client, company);
        JsonElement detail = await Send(client, Request(HttpMethod.Get, $"/api/v1/security/users/{admin.User.Id}", admin.AccessToken, company));
        long version = detail.GetProperty("account").GetProperty("version").GetInt64();
        using HttpResponseMessage disable = await client.SendAsync(Request(HttpMethod.Put, $"/api/v1/security/users/{admin.User.Id}/activation", admin.AccessToken, company, new { isActive = false, version }), ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, disable.StatusCode);

        JsonElement roles = await Send(client, Request(HttpMethod.Get, "/api/v1/security/roles", admin.AccessToken, company));
        JsonElement systemRole = Assert.Single(roles.EnumerateArray());
        using HttpResponseMessage mutate = await client.SendAsync(Request(HttpMethod.Put, $"/api/v1/security/roles/{systemRole.GetProperty("id").GetGuid()}/activation", admin.AccessToken, company, new { isActive = false, version = systemRole.GetProperty("version").GetInt64() }), ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mutate.StatusCode);
        using HttpResponseMessage foreignCreate = await client.SendAsync(Request(HttpMethod.Post, "/api/v1/security/users/linked", admin.AccessToken, company, new { personId = foreignPerson, username = "foreign.user", password = "StrongForeignPassword123", enabled = true }), ct);
        Assert.Equal(HttpStatusCode.NotFound, foreignCreate.StatusCode);
        using HttpResponseMessage crossCompany = await client.SendAsync(Request(HttpMethod.Get, $"/api/v1/security/users/{admin.User.Id}/permission-target", admin.AccessToken, other), ct);
        Assert.Equal(HttpStatusCode.NotFound, crossCompany.StatusCode);
    }

    private async Task<Guid> AddCompany(string code)
    {
        await using CoreDbContext context = database.CreateAdministrationCoreDbContext();
        Company company = Company.Create(Guid.NewGuid(), code, code, DateTimeOffset.UtcNow).Value;
        context.Companies.Add(company); await context.SaveChangesAsync(TestContext.Current.CancellationToken); return company.Id;
    }
    private async Task<Guid> AddPerson(Guid company, string code, string first, string last, string? mobile)
    {
        await using CoreDbContext context = database.CreateAdministrationCoreDbContext();
        Person person = Person.Create(Guid.NewGuid(), company, code, first, last, null, null, null, mobile, null, null, DateTimeOffset.UtcNow).Value;
        context.Persons.Add(person); await context.SaveChangesAsync(TestContext.Current.CancellationToken); return person.Id;
    }
    private static async Task<LoginResult> BootstrapAndLogin(HttpClient client, Guid company)
    {
        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = company, username = "uam.admin", displayName = "UAM Administrator", password = "StrongUamAdminPassword123" }) };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(bootstrap, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        return await Login(client, "uam.admin", "StrongUamAdminPassword123");
    }
    private static async Task<LoginResult> Login(HttpClient client, string username, string password)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<LoginResult>(TestContext.Current.CancellationToken))!;
    }
    private static HttpRequestMessage Request(HttpMethod method, string path, string? token, Guid? company, object? body = null)
    {
        HttpRequestMessage request = new(method, path); if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); if (company is not null) request.Headers.Add("X-Company-ID", company.ToString()); if (body is not null) request.Content = JsonContent.Create(body); return request;
    }
    private static async Task<JsonElement> Send(HttpClient client, HttpRequestMessage request)
    {
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken));
    }
    private static PermissionState State(JsonElement target, Guid permission) { JsonElement value = target.GetProperty("permissions").EnumerateArray().Single(x => x.GetProperty("permissionId").GetGuid() == permission); return new(value.GetProperty("direct").GetBoolean(), value.GetProperty("inherited").GetBoolean(), value.GetProperty("effective").GetBoolean()); }
    private sealed record PermissionState(bool Direct, bool Inherited, bool Effective);
}
