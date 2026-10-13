using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Auditing;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class AuditFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task SecurityFlowsPersistSafeCorrelatedCompanyAuditAndQueryFilters()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyA = await AddCompanyAsync("AUDIT_A");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        await BootstrapAsync(client, companyA);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(Request(HttpMethod.Get, "/api/v1/audit/events", null, companyA), TestContext.Current.CancellationToken)).StatusCode);

        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-2);
        LoginResult admin = await LoginAsync(client, "audit.admin", "StrongAuditPassword123");
        Guid auditPersonId = Guid.NewGuid();
        await using (CoreDbContext personContext = database.CreateAdministrationCoreDbContext())
        {
            personContext.Persons.Add(Person.Create(auditPersonId, companyA, "AUDIT-USER", "Audit", "User", null, null, null, null, null, null, DateTimeOffset.UtcNow).Value);
            await personContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        const string correlation = "audit-company-selection-correlation";
        using HttpRequestMessage select = Request(HttpMethod.Post, "/api/v1/auth/me/company", admin.AccessToken, null,
            new { companyId = companyA });
        select.Headers.Add("X-Correlation-ID", correlation);
        (await client.SendAsync(select, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        using HttpRequestMessage createUser = Request(HttpMethod.Post, $"/api/v1/security/users/for-person/{auditPersonId}", admin.AccessToken, companyA,
            new { username = "audit.user", displayName = "Audit User", password = "StrongAuditUserPassword123", enabled = true });
        createUser.Headers.Add("X-Correlation-ID", "audit-user-created-correlation");
        using HttpResponseMessage createdResponse = await client.SendAsync(createUser, TestContext.Current.CancellationToken);
        createdResponse.EnsureSuccessStatusCode();
        using JsonDocument createdJson = JsonDocument.Parse(await createdResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Guid userId = createdJson.RootElement.GetProperty("id").GetGuid();
        long userVersion = createdJson.RootElement.GetProperty("version").GetInt64();

        using HttpResponseMessage updatedResponse = await client.SendAsync(Request(HttpMethod.Put,
            $"/api/v1/security/users/{userId}/account", admin.AccessToken, companyA,
            new { username = "audit.user.updated", version = userVersion }), TestContext.Current.CancellationToken);
        string updatedBody = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(updatedResponse.IsSuccessStatusCode, $"{updatedResponse.StatusCode}: {updatedBody}");

        using HttpResponseMessage rolesResponse = await client.SendAsync(Request(HttpMethod.Get,
            "/api/v1/security/roles/", admin.AccessToken, companyA), TestContext.Current.CancellationToken);
        using JsonDocument rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Guid roleId = rolesJson.RootElement[0].GetProperty("id").GetGuid();
        (await client.SendAsync(Request(HttpMethod.Post, $"/api/v1/security/users/{userId}/roles",
            admin.AccessToken, companyA, new { roleId, companyId = companyA }), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await client.SendAsync(Request(HttpMethod.Delete, $"/api/v1/security/users/{userId}/roles/{roleId}/companies/{companyA}",
            admin.AccessToken, companyA), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        using HttpResponseMessage filtered = await client.SendAsync(Request(HttpMethod.Get,
            $"/api/v1/audit/events?correlationId={correlation}&entityType=company&pageNumber=1&pageSize=10",
            admin.AccessToken, companyA), TestContext.Current.CancellationToken);
        filtered.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement item = Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(AuditActions.CompanySelected, item.GetProperty("action").GetString());
        Assert.Equal(correlation, item.GetProperty("correlationId").GetString());
        Assert.Equal(companyA, item.GetProperty("companyId").GetGuid());
        Assert.True(item.GetProperty("occurredAtUtc").GetDateTimeOffset() >= before);
        Assert.Equal("Success", item.GetProperty("outcome").GetString());

        await using (CoreDbContext auditContext = database.CreateAdministrationCoreDbContext())
        {
            string[] actions = await auditContext.AuditEvents.IgnoreQueryFilters()
                .Where(x => x.CompanyId == companyA).Select(x => x.Action).ToArrayAsync(TestContext.Current.CancellationToken);
            Assert.Contains(AuditActions.UserRoleAssigned, actions);
            Assert.Contains(AuditActions.UserRoleRevoked, actions);
            AuditEvent loginAudit = await auditContext.AuditEvents.IgnoreQueryFilters().SingleAsync(
                x => x.Action == AuditActions.LoginSucceeded && x.ActorUsername == "AUDIT.ADMIN", TestContext.Current.CancellationToken);
            Assert.Equal(admin.User.Id, loginAudit.ActorUserId);
            Assert.Equal("InternalUser", loginAudit.ActorType);
            AuditEvent update = await auditContext.AuditEvents.IgnoreQueryFilters().SingleAsync(
                x => x.Action == AuditActions.UserUpdated && x.EntityId == userId.ToString(), TestContext.Current.CancellationToken);
            Assert.Contains("username", update.ChangesJson, StringComparison.Ordinal);
            Assert.DoesNotContain("password", update.ChangesJson, StringComparison.OrdinalIgnoreCase);
        }

        using HttpResponseMessage limited = await client.SendAsync(Request(HttpMethod.Get,
            "/api/v1/audit/events?pageSize=101", admin.AccessToken, companyA), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, limited.StatusCode);

        (await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/logout", admin.AccessToken, null), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using CoreDbContext logoutContext = database.CreateAdministrationCoreDbContext();
        Assert.True(await logoutContext.AuditEvents.IgnoreQueryFilters().AnyAsync(
            x => x.Action == AuditActions.Logout && x.ActorUserId == admin.User.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedLoginIsAuditedWithoutCredentialMaterial()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid company = await AddCompanyAsync("AUDIT_LOGIN");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        await BootstrapAsync(client, company);
        const string password = "NeverPersistThisPassword123";
        using HttpRequestMessage failed = new(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { username = "missing.audit.user", password })
        };
        failed.Headers.Add("X-Correlation-ID", "failed-login-correlation");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(failed, TestContext.Current.CancellationToken)).StatusCode);
        using HttpRequestMessage longUsername = new(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { username = new string('x', 500), password })
        };
        longUsername.Headers.Add("X-Correlation-ID", "failed-login-long-username");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(longUsername, TestContext.Current.CancellationToken)).StatusCode);

        await using CoreDbContext context = database.CreateAdministrationCoreDbContext();
        AuditEvent audit = context.AuditEvents.IgnoreQueryFilters().Single(x => x.CorrelationId == "failed-login-correlation");
        Assert.Equal(AuditActions.LoginFailed, audit.Action);
        Assert.Equal("Failure", audit.Outcome);
        Assert.Equal("MISSING.AUDIT.USER", audit.ActorUsername);
        Assert.DoesNotContain(password, audit.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("password", audit.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100, context.AuditEvents.IgnoreQueryFilters().Single(
            x => x.CorrelationId == "failed-login-long-username").ActorUsername!.Length);
    }

    [Fact]
    public async Task FailedTransactionLeavesNoSuccessfulAuditEvent()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid company = await AddCompanyAsync("AUDIT_TX");
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using IServiceScope scope = factory.Services.CreateScope();
        CurrentActorContext actors = scope.ServiceProvider.GetRequiredService<CurrentActorContext>();
        CompanyContext companies = scope.ServiceProvider.GetRequiredService<CompanyContext>();
        CorrelationContext correlations = scope.ServiceProvider.GetRequiredService<CorrelationContext>();
        ITransactionExecutor transaction = scope.ServiceProvider.GetRequiredService<ITransactionExecutor>();
        IAuditWriter writer = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        Guid actorId = Guid.NewGuid();
        using IDisposable actorScope = actors.BeginScope(new ActorContext(actorId.ToString(), ActorType.InternalUser, true, SourceChannel.Backoffice, actorId.ToString(), actorUsername: "AUDIT.ACTOR"));
        using IDisposable companyScope = companies.BeginScope(company);
        using IDisposable correlationScope = correlations.BeginScope("rolled-back-audit-correlation");

        Result result = await transaction.ExecuteAsync(_ =>
        {
            writer.Write(new AuditWriteRequest("test.mutation.completed", "probe", "1"));
            return Task.FromResult(Result.Failure(Error.BusinessRule("test.rollback")));
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        await using CoreDbContext verification = database.CreateAdministrationCoreDbContext();
        Assert.False(verification.AuditEvents.IgnoreQueryFilters().Any(x => x.CorrelationId == "rolled-back-audit-correlation"));
    }

    [Fact]
    public async Task RowLevelSecurityIsolatesCompaniesAndPooledContextDoesNotLeak()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyA = await AddCompanyAsync("AUDIT_RLS_A");
        Guid companyB = await AddCompanyAsync("AUDIT_RLS_B");
        await using (CoreDbContext context = database.CreateAdministrationCoreDbContext())
        {
            context.AuditEvents.Add(Event(companyA, "audit-a"));
            context.AuditEvents.Add(Event(companyB, "audit-b"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (NpgsqlTransaction first = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await SetCompanyAsync(connection, first, companyA);
            Assert.Equal(1L, await CountAsync(connection, first));
            await first.CommitAsync(TestContext.Current.CancellationToken);
        }
        await using NpgsqlTransaction second = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0L, await CountAsync(connection, second));
    }

    private async Task<Guid> AddCompanyAsync(string code)
    {
        await using CoreDbContext context = database.CreateCoreDbContext();
        Company company = Company.Create(Guid.NewGuid(), code, code, DateTimeOffset.UtcNow).Value;
        context.Companies.Add(company);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return company.Id;
    }

    private static AuditEvent Event(Guid company, string correlation) => AuditEvent.Create(
        DateTimeOffset.UtcNow, company, null, null, "System", "test.audit.created",
        "probe", Guid.NewGuid().ToString(), correlation, correlation, "System",
        "Success", null, null, null, "{}", "{}");

    private static async Task BootstrapAsync(HttpClient client, Guid companyId)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/security/bootstrap")
        {
            Content = JsonContent.Create(new { companyId, username = "audit.admin", displayName = "Audit Administrator", password = "StrongAuditPassword123" })
        };
        request.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
    }

    private static async Task<LoginResult> LoginAsync(HttpClient client, string username, string password)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResult>(TestContext.Current.CancellationToken))!;
    }

    private static HttpRequestMessage Request(HttpMethod method, string path, string? token, Guid? company, object? body = null)
    {
        HttpRequestMessage request = new(method, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (company is not null) request.Headers.Add("X-Company-ID", company.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task SetCompanyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid company)
    {
        await using NpgsqlCommand command = new("SELECT set_config('barang.company_id', @company, true);", connection, transaction);
        command.Parameters.AddWithValue("company", company.ToString());
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using NpgsqlCommand command = new("SELECT count(*) FROM barang.audit_events;", connection, transaction);
        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }
}

public sealed record LoginResult(string AccessToken, LoginUser User);
public sealed record LoginUser(Guid Id);
