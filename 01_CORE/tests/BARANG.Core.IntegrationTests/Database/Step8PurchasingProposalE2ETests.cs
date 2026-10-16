using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class Step8PurchasingProposalE2ETests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task ShortageConversionIsPersistedAndRetryIsIdempotentWithoutPo()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid company = Guid.NewGuid(), branch = Guid.NewGuid(), warehouseId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ProductVariant variant;
        Guid supplierId = Guid.NewGuid();

        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "STEP8_PR", "STEP8 purchasing", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            db.Warehouses.Add(Warehouse.Create(warehouseId, company, branch, "W8", "STEP8 Warehouse", WarehouseType.RawMaterials, now).Value);
            Product product = Product.Create(Guid.NewGuid(), company, "P8", "STEP8 Product", null, "RAW", "PCS", now).Value;
            variant = ProductVariant.Create(Guid.NewGuid(), product, "SKU8", "STEP8 Variant", null, null, null, now).Value;
            db.Suppliers.Add(Supplier.Create(supplierId, company, "SUP8", "STEP8 Supplier", null, null, null, null, null, null, null, null, now).Value);
            db.Products.Add(product);
            db.ProductVariants.Add(variant);
            db.StockBalances.Add(StockBalance.Create(company, warehouseId, variant.Id, now));
            db.StockShortages.Add(StockShortage.Create(company, Guid.NewGuid(), warehouseId, variant.Id, 8m, 0m, now));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap")
        {
            Content = JsonContent.Create(new { companyId = company, username = "step8-admin", displayName = "STEP8 Admin", password = "StrongStep8Password123" })
        };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(bootstrap, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        Login login = (await (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "step8-admin", password = "StrongStep8Password123" }, TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<Login>(TestContext.Current.CancellationToken))!;
        (await client.SendAsync(Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, null, new { companyId = company }), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        ShortageView[] shortages = await Read<ShortageView[]>(client, Request(HttpMethod.Get, "/api/v1/inventory/shortages?includeClosed=false&limit=10", login.AccessToken, company));
        ShortageView shortage = Assert.Single(shortages);
        Assert.Equal(ShortageProposalStatus.Open, shortage.ProposalStatus);

        RequestView first = await Read<RequestView>(client, Request(HttpMethod.Post, $"/api/v1/purchasing/shortages/{shortage.Id}/convert", login.AccessToken, company));
        RequestView retry = await Read<RequestView>(client, Request(HttpMethod.Post, $"/api/v1/purchasing/shortages/{shortage.Id}/convert", login.AccessToken, company));
        Assert.Equal(first.Id, retry.Id);
        RequestDetailView detail = await Read<RequestDetailView>(client, Request(HttpMethod.Get, $"/api/v1/purchasing/requests/{first.Id}", login.AccessToken, company));
        Assert.Equal(shortage.Id, detail.SourceShortageId);

        PurchaseOrderView[] orders = await Read<PurchaseOrderView[]>(client, Request(HttpMethod.Get, "/api/v1/purchasing/orders", login.AccessToken, company));
        Assert.Empty(orders);

        ShortageView[] after = await Read<ShortageView[]>(client, Request(HttpMethod.Get, "/api/v1/inventory/shortages?includeClosed=true&limit=10", login.AccessToken, company));
        ShortageView persisted = Assert.Single(after);
        Assert.Equal(ShortageProposalStatus.Converted, persisted.ProposalStatus);
        Assert.Equal(first.Id, persisted.PurchaseRequestId);

        Guid purchaseOrderId, purchaseOrderItemId;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            PurchaseOrder order = PurchaseOrder.Create(company, supplierId, warehouseId, "PO-STEP8-0001", "USD", null, now);
            purchaseOrderId = order.Id;
            Assert.True(order.Submit(1, now).IsSuccess);
            PurchaseOrderItem item = PurchaseOrderItem.Create(company, order.Id, variant.Id, "PCS", 5m, 10m);
            purchaseOrderItemId = item.Id;
            db.PurchaseOrders.Add(order);
            db.PurchaseOrderItems.Add(item);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await Read<PurchaseOrderView>(client, Request(HttpMethod.Post, $"/api/v1/purchasing/orders/{purchaseOrderId}/receipts", login.AccessToken, company, new { purchaseOrderItemId, quantity = 5m, reference = "STEP8-RECEIPT-1" }));
        ShortageView[] afterReceipt = await Read<ShortageView[]>(client, Request(HttpMethod.Get, "/api/v1/inventory/shortages?includeClosed=true&limit=10", login.AccessToken, company));
        ShortageView rechecked = Assert.Single(afterReceipt);
        Assert.Equal(5m, rechecked.AvailableQuantity);
        Assert.Equal(3m, rechecked.ShortageQuantity);
        Assert.NotNull(rechecked.LastRecheckedAtUtc);
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, string token, Guid? company, object? body = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (company is Guid id) request.Headers.Add("X-Company-ID", id.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<T> Read<T>(HttpClient client, HttpRequestMessage request)
    {
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {payload}");
        return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
    }

    private sealed record Login(string AccessToken);
    private sealed record ShortageView(Guid Id, Guid OrderItemId, Guid WarehouseId, Guid ProductVariantId, decimal RequiredQuantity, decimal AvailableQuantity, decimal ShortageQuantity, bool IsActive, DateTimeOffset CreatedAtUtc, long Version, ShortageProposalStatus ProposalStatus, Guid? PurchaseRequestId, DateTimeOffset? LastRecheckedAtUtc);
    private sealed record RequestView(Guid Id, Guid WarehouseId, string Reason, PurchaseRequestStatus Status, long Version);
    private sealed record RequestDetailView(Guid Id, string RequestNumber, Guid WarehouseId, string Reason, Guid? SourceShortageId, PurchaseRequestStatus Status, long Version, object[] Items);
    private sealed record PurchaseOrderView(Guid Id, string PurchaseOrderNumber, Guid SupplierId, Guid WarehouseId, string CurrencyCode, PurchaseOrderStatus Status, long Version);
}
