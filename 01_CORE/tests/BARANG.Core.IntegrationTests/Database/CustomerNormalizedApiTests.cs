using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class CustomerNormalizedApiTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task IndividualAndLegalCustomersRoundTripThroughNormalizedProfiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = Guid.NewGuid();
        Guid branch = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "NORM_C", "Normalized Customers", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            await db.SaveChangesAsync(ct);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap")
        {
            Content = JsonContent.Create(new { companyId = company, username = "normalized-admin", displayName = "Normalized Admin", password = "StrongNormalizedPassword123" })
        };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(bootstrap, ct)).EnsureSuccessStatusCode();
        JsonDocument login = await Read(await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "normalized-admin", password = "StrongNormalizedPassword123" }, ct), ct);
        string token = login.RootElement.GetProperty("accessToken").GetString()!;
        using HttpResponseMessage companyResponse = await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/me/company", token, company, new { companyId = company }), ct);
        companyResponse.EnsureSuccessStatusCode();

        JsonDocument individual = await Read(await client.SendAsync(Request(HttpMethod.Post, "/api/v1/customers", token, company, new { customerCode = "IND-NORM", customerType = "Individual", displayName = "Alice Smith", firstName = "Alice", lastName = "Smith", primaryMobile = "09120000001" }), ct), ct);
        Guid individualId = individual.RootElement.GetProperty("id").GetGuid();
        JsonDocument individualRead = await Read(await client.SendAsync(Request(HttpMethod.Get, $"/api/v1/customers/{individualId}", token, company), ct), ct);
        Assert.Equal("Alice", individualRead.RootElement.GetProperty("individualProfile").GetProperty("firstName").GetString());

        JsonDocument updated = await Read(await client.SendAsync(Request(HttpMethod.Put, $"/api/v1/customers/{individualId}", token, company, new { displayName = "Alice Updated", firstName = "Alicia", lastName = "Smith", primaryMobile = "09120000002", version = individualRead.RootElement.GetProperty("version").GetInt64(), profileVersion = individualRead.RootElement.GetProperty("individualProfile").GetProperty("version").GetInt64() }), ct), ct);
        Assert.Equal("Alicia", updated.RootElement.GetProperty("individualProfile").GetProperty("firstName").GetString());

        JsonDocument legal = await Read(await client.SendAsync(Request(HttpMethod.Post, "/api/v1/customers", token, company, new { customerCode = "LEG-NORM", customerType = "LegalEntity", displayName = "Beta Ltd", legalName = "Beta Ltd", economicCode = "EC-NORM" }), ct), ct);
        Guid legalId = legal.RootElement.GetProperty("id").GetGuid();
        JsonDocument legalRead = await Read(await client.SendAsync(Request(HttpMethod.Get, $"/api/v1/customers/{legalId}", token, company), ct), ct);
        Assert.Equal("Beta Ltd", legalRead.RootElement.GetProperty("legalProfile").GetProperty("legalName").GetString());

        await using CoreDbContext verify = database.CreateAdministrationCoreDbContext();
        Assert.True(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(verify.IndividualCustomerProfiles.IgnoreQueryFilters(), x => x.CustomerId == individualId, ct));
        Assert.True(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(verify.LegalCustomerProfiles.IgnoreQueryFilters(), x => x.CustomerId == legalId, ct));
        Assert.DoesNotContain(verify.Model.FindEntityType(typeof(BARANG.Core.Domain.Customers.Customer))!.GetProperties(), property => property.GetColumnName() == "first_name");
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, string token, Guid company, object? body = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Company-ID", company.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<JsonDocument> Read(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
            return JsonDocument.Parse(body);
        }
    }
}
