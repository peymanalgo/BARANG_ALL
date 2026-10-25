using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Application.Background;
using BARANG.Core.Domain.Background;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

#pragma warning disable CA1707
[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class NotificationOutboxAcceptanceTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task WorkerIsConcurrentRetryableDeadLetteredAndAuthorized()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using (CoreDbContext setup = database.CreateAdministrationCoreDbContext())
        {
            setup.Companies.Add(Company.Create(company, "OUTBOX_WORKER", "Outbox worker", now).Value);
            await setup.SaveChangesAsync(ct);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using WebApplicationFactory<Program> host = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISmsSender, FailingSmsSender>();
            }));
        using HttpClient client = host.CreateClient();
        string username = $"outbox-admin-{Guid.NewGuid():N}";
        string password = "StrongOutboxPassword123";
        await Bootstrap(client, company, username, password, ct);
        Login login = await Json<Login>(await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, ct), ct);
        await Send(client, Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, null, new { companyId = company }), ct);
        Guid actor = await UserId(username, ct);

        FailingSmsSender.Fail = true;
        Guid queued = await Json<Guid>(await client.SendAsync(Request(HttpMethod.Post, "/api/v1/notifications/queue", login.AccessToken, company,
            new { recipientUserId = actor, channel = NotificationChannel.Sms, templateCode = "test.outbox.failure", payload = "{}" }), ct), ct);

        for (int attempt = 0; attempt < BackgroundRetryPolicy.MaximumAttempts; attempt++)
        {
            await Send(client, Request(HttpMethod.Post, "/api/v1/background/outbox/dispatch", login.AccessToken, company), ct);
            await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE barang.outbox_messages SET next_attempt_at_utc = CURRENT_TIMESTAMP WHERE id = {queued}", ct);
        }

        JsonElement deadLetters = await JsonElementResult(client, Request(HttpMethod.Get, "/api/v1/background/dead-letters", login.AccessToken, company), ct);
        JsonElement dead = deadLetters.EnumerateArray().First(x => x.GetProperty("outboxMessageId").GetGuid() == queued);
        Assert.Equal((int)DeadLetterStatus.Open, dead.GetProperty("status").GetInt32());
        long deadVersion = dead.GetProperty("version").GetInt64();

        FailingSmsSender.Fail = false;
        await Send(client, Request(HttpMethod.Post, $"/api/v1/background/dead-letters/{dead.GetProperty("id").GetGuid()}/retry", login.AccessToken, company,
            new { version = deadVersion }), ct);
        using (IServiceScope workerScope = host.Services.CreateScope())
        {
            await workerScope.ServiceProvider.GetRequiredService<IOutboxDispatcher>().DispatchAllAsync(ct);
        }

        await using (CoreDbContext verify = database.CreateAdministrationCoreDbContext())
        {
            using IDisposable scope = database.CompanyContext.BeginScope(company);
            OutboxMessage message = await verify.OutboxMessages.IgnoreQueryFilters().SingleAsync(x => x.Id == queued, ct);
            Assert.Equal(OutboxStatus.Processed, message.Status);
            Assert.Equal(1, await verify.Notifications.IgnoreQueryFilters().CountAsync(x => x.CompanyId == company && x.RecipientUserId == actor && x.TemplateCode == "test.outbox.failure", ct));
        }

        JsonElement filtered = await JsonElementResult(client, Request(HttpMethod.Get, "/api/v1/notifications/mine?search=test.outbox.failure&page=1&pageSize=10", login.AccessToken, company), ct);
        Assert.Equal(1, filtered.GetProperty("total").GetInt64());
        JsonElement filteredRow = filtered.GetProperty("items").EnumerateArray().Single();
        Assert.Equal("system", filteredRow.GetProperty("category").GetString());
        Assert.Equal("urgent", filteredRow.GetProperty("priority").GetString());
        Assert.False(filteredRow.GetProperty("isActionable").GetBoolean());

        Guid concurrent = await Json<Guid>(await client.SendAsync(Request(HttpMethod.Post, "/api/v1/notifications/queue", login.AccessToken, company,
            new { recipientUserId = actor, channel = NotificationChannel.InApp, templateCode = "test.outbox.concurrent", payload = "{}" }), ct), ct);
        using HttpClient second = host.CreateClient();
        HttpResponseMessage[] dispatchResponses = await Task.WhenAll(
            second.SendAsync(Request(HttpMethod.Post, "/api/v1/background/outbox/dispatch", login.AccessToken, company), ct),
            client.SendAsync(Request(HttpMethod.Post, "/api/v1/background/outbox/dispatch", login.AccessToken, company), ct));
        foreach (HttpResponseMessage response in dispatchResponses)
        {
            response.EnsureSuccessStatusCode();
            response.Dispose();
        }
        await using (CoreDbContext verify = database.CreateAdministrationCoreDbContext())
        {
            Assert.Equal(OutboxStatus.Processed, (await verify.OutboxMessages.IgnoreQueryFilters().SingleAsync(x => x.Id == concurrent, ct)).Status);
            Assert.Equal(1, await verify.Notifications.IgnoreQueryFilters().CountAsync(x => x.CompanyId == company && x.TemplateCode == "test.outbox.concurrent", ct));
        }

        using HttpResponseMessage unauthorized = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/background/dead-letters", login.AccessToken, Guid.NewGuid()), ct);
        Assert.True(unauthorized.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    private async Task<Guid> UserId(string username, CancellationToken ct)
    {
        await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
        string normalized = username.ToUpperInvariant();
        return await db.ApplicationUsers.IgnoreQueryFilters().Where(x => x.Username == normalized).Select(x => x.Id).SingleAsync(ct);
    }

    private static async Task Bootstrap(HttpClient client, Guid company, string username, string password, CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = company, username, displayName = "Outbox admin", password }) };
        request.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(request, ct)).EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, string token, Guid? company, object? body = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (company is not null) request.Headers.Add("X-Company-ID", company.Value.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task Send(HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        using HttpResponseMessage response = await client.SendAsync(request, ct);
        string payload = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {payload}");
    }

    private static async Task<T> Json<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            string payload = await response.Content.ReadAsStringAsync(ct);
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {payload}");
            return (await response.Content.ReadFromJsonAsync<T>(ct))!;
        }
    }

    private static async Task<JsonElement> JsonElementResult(HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        using HttpResponseMessage response = await client.SendAsync(request, ct);
        string payload = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {payload}");
        return System.Text.Json.JsonDocument.Parse(payload).RootElement.Clone();
    }

    private sealed record Login(string AccessToken);

    private sealed class FailingSmsSender : ISmsSender
    {
        public static volatile bool Fail;
        public Task<Result> SendAsync(NotificationDelivery delivery, CancellationToken ct) =>
            Task.FromResult(Fail ? Result.Failure(Error.Infrastructure("test.delivery_failed")) : Result.Success());
    }
}
#pragma warning restore CA1707
