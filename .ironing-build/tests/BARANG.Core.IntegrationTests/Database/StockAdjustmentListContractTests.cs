using System.Net.Http.Headers;
using System.Net.Http.Json;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class StockAdjustmentListContractTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task AdjustmentListUsesCanonicalLedgerWithCompanyFilterSearchAndPaging()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = Guid.NewGuid(), branch = Guid.NewGuid(), customer = Guid.NewGuid(), warehouse = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Product product;
        ProductVariant variant;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "ADJ_LIST", "Adjustment list", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            db.Customers.Add(Customer.Create(customer, company, "ADJ-C", CustomerType.Organization, null, null, null, "Adjustment customer", null, null, null, null, null, null, null, now).Value);
            product = Product.Create(Guid.NewGuid(), company, "ADJ-P", "Adjustment product", null, "RAW", "PCS", now).Value;
            variant = ProductVariant.Create(Guid.NewGuid(), product, "ADJ-SKU", "Adjustment SKU", null, null, null, now).Value;
            db.Products.Add(product);
            db.ProductVariants.Add(variant);
            db.Warehouses.Add(Warehouse.Create(warehouse, company, branch, "ADJ-W", "Adjustment warehouse", WarehouseType.RawMaterials, now).Value);
            await db.SaveChangesAsync(ct);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = company, username = "adjustment-list-admin", displayName = "Adjustment List Admin", password = "StrongAdjustmentPassword123" }) };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(bootstrap, ct)).EnsureSuccessStatusCode();
        Login login = (await (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "adjustment-list-admin", password = "StrongAdjustmentPassword123" }, ct)).Content.ReadFromJsonAsync<Login>(ct))!;
        await Send(client, Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, null, new { companyId = company }), ct);

        string reference = $"ADJ-CONTRACT-{Guid.NewGuid():N}";
        await Send(client, Request(HttpMethod.Post, "/api/v1/inventory/adjustments/true", login.AccessToken, company, new { warehouseId = warehouse, productVariantId = variant.Id, quantity = 3, reference, reasonCode = "COUNT" }), ct);
        AdjustmentPage page = await Send<AdjustmentPage>(client, Request(HttpMethod.Get, $"/api/v1/inventory/adjustments?search={reference}&warehouseId={warehouse}&pageNumber=1&pageSize=1", login.AccessToken, company), ct);

        Assert.Equal(1, page.TotalCount);
        AdjustmentRow row = Assert.Single(page.Items);
        Assert.Equal(reference.ToUpperInvariant(), row.Reference);
        Assert.Equal(warehouse, row.WarehouseId);
        Assert.Equal(variant.Id, row.ProductVariantId);
        Assert.Equal("PCS", row.UomCode);
        Assert.Equal(3m, row.QuantityDelta);
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
        string body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
    }

    private static async Task<T> Send<T>(HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        using HttpResponseMessage response = await client.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private sealed record Login(string AccessToken);
    private sealed record AdjustmentPage(AdjustmentRow[] Items, int TotalCount, int PageNumber, int PageSize, long TotalPages);
    private sealed record AdjustmentRow(Guid Id, Guid WarehouseId, Guid ProductVariantId, string UomCode, decimal QuantityDelta, string Reference);
}
