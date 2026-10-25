using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class Step8PurchasingApprovalE2ETests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task EligibleNonCreatorApprovesRequestAndOrderBeforeSend()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid company = Guid.Parse("88888888-8888-8888-8888-888888888888"), branch = Guid.NewGuid(), warehouse = Guid.NewGuid(), supplier = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ProductVariant variant;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "STEP8_AP", "STEP8 approvals", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            db.Warehouses.Add(Warehouse.Create(warehouse, company, branch, "APW", "Approval warehouse", WarehouseType.RawMaterials, now).Value);
            Product product = Product.Create(Guid.NewGuid(), company, "APP", "Approval product", null, "RAW", "PCS", now).Value;
            variant = ProductVariant.Create(Guid.NewGuid(), product, "AP-SKU", "Approval SKU", null, null, null, now).Value;
            db.Products.Add(product); db.ProductVariants.Add(variant);
            db.Suppliers.Add(Supplier.Create(supplier, company, "APS", "Approval supplier", null, null, null, null, null, null, null, null, now).Value);
            foreach (string code in new[] { SecurityPermissions.PurchasingRequestApprove, SecurityPermissions.PurchasingOrderApprove })
                if (!await db.Permissions.AnyAsync(x => x.Code == code, TestContext.Current.CancellationToken))
                    db.Permissions.Add(Permission.Create(Guid.NewGuid(), code, code, "purchasing"));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString, "Development");
        using HttpClient client = factory.CreateClient();
        await Bootstrap(client, company, "step8-creator", "Step8 Creator", "Step8CreatorPassword123");
        Login creator = await LoginAsync(client, "step8-creator", "Step8CreatorPassword123");
        await SelectCompany(client, creator.AccessToken, company);
        Guid creatorId = await UserId(database, "step8-creator");
        Guid approverId = Guid.NewGuid();
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            ApplicationUser source = await db.ApplicationUsers.SingleAsync(x => x.Id == creatorId, TestContext.Current.CancellationToken);
            Guid approverPersonId = Guid.NewGuid();
            db.Persons.Add(Person.Create(approverPersonId, company, "AP-001", "Step8", "Approver", null, null, null, null, null, null, now).Value);
            ApplicationUser approverUser = ApplicationUser.CreateIdentity(approverId, approverPersonId, "step8-approver", "Step8 Approver", null, now).Value;
            approverUser.SetPasswordHash(source.PasswordHash!, now);
            db.ApplicationUsers.Add(approverUser);
            db.UserCompanies.Add(UserCompany.Create(approverId, company, now.AddMinutes(-1), null, creatorId, now).Value);
            Guid permission = await db.Permissions.Where(x => x.Code == SecurityPermissions.PurchasingRequestApprove).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);
            db.UserPermissions.Add(UserPermission.Create(approverId, permission, company, creatorId, now).Value);
            Guid poPermission = await db.Permissions.Where(x => x.Code == SecurityPermissions.PurchasingOrderApprove).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);
            db.UserPermissions.Add(UserPermission.Create(approverId, poPermission, company, creatorId, now).Value);
            Guid managePermission = await db.Permissions.Where(x => x.Code == SecurityPermissions.PurchasingManage).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);
            db.UserPermissions.Add(UserPermission.Create(approverId, managePermission, company, creatorId, now).Value);
            Guid readPermission = await db.Permissions.Where(x => x.Code == SecurityPermissions.PurchasingRead).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);
            db.UserPermissions.Add(UserPermission.Create(approverId, readPermission, company, creatorId, now).Value);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Login approver = await LoginAsync(client, "step8-approver", "Step8CreatorPassword123");
        await SelectCompany(client, approver.AccessToken, company);
        using HttpRequestMessage create = Request(HttpMethod.Post, "/api/v1/purchasing/requests", creator.AccessToken, company, new { warehouseId = warehouse, reason = "approved replenishment", items = new[] { new { productVariantId = variant.Id, quantity = 2m, uomCode = "PCS" } } });
        JsonElement request = await Read(client, create);
        Guid requestId = request.GetProperty("id").GetGuid(); long requestVersion = request.GetProperty("version").GetInt64();
        JsonElement submitted = await Read(client, Request(HttpMethod.Post, $"/api/v1/purchasing/requests/{requestId}/submit?version={requestVersion}", creator.AccessToken, company));
        long submittedVersion = submitted.GetProperty("version").GetInt64();
        JsonElement approved = await Read(client, Request(HttpMethod.Post, $"/api/v1/purchasing/requests/{requestId}/approve", approver.AccessToken, company, new { version = submittedVersion }));
        Assert.Equal((int)PurchaseRequestStatus.Approved, approved.GetProperty("status").GetInt32());
        using HttpResponseMessage self = await client.SendAsync(Request(HttpMethod.Post, $"/api/v1/purchasing/requests/{requestId}/approve", creator.AccessToken, company, new { version = approved.GetProperty("version").GetInt64() }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);

        PurchaseOrder order = PurchaseOrder.Create(company, supplier, warehouse, creatorId, "PO-AP-1", "USD", null, now);
        Assert.True(order.Submit(order.Version, now).IsSuccess);
        Guid orderId = order.Id;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        { db.PurchaseOrders.Add(order); db.PurchaseOrderItems.Add(PurchaseOrderItem.Create(company, order.Id, variant.Id, "PCS", 2m, 10m)); db.PurchasingHistory.Add(PurchasingHistory.Create(company, "PURCHASE_ORDER", order.Id, "Created", creatorId, now)); await db.SaveChangesAsync(TestContext.Current.CancellationToken); }
        JsonElement poApproved = await Read(client, Request(HttpMethod.Post, $"/api/v1/purchasing/orders/{orderId}/approve", approver.AccessToken, company, new { version = order.Version }));
        Assert.Equal((int)PurchaseOrderStatus.Approved, poApproved.GetProperty("status").GetInt32());
        JsonElement sent = await Read(client, Request(HttpMethod.Post, $"/api/v1/purchasing/orders/{orderId}/send?version={poApproved.GetProperty("version").GetInt64()}", creator.AccessToken, company));
        Assert.Equal((int)PurchaseOrderStatus.Sent, sent.GetProperty("status").GetInt32());

        JsonElement pending = await Read(client, Request(HttpMethod.Post, "/api/v1/purchasing/requests", creator.AccessToken, company, new { warehouseId = warehouse, reason = "browser approval fixture", items = new[] { new { productVariantId = variant.Id, quantity = 1m, uomCode = "PCS" } } }));
        await Read(client, Request(HttpMethod.Post, $"/api/v1/purchasing/requests/{pending.GetProperty("id").GetGuid()}/submit?version={pending.GetProperty("version").GetInt64()}", creator.AccessToken, company));
    }

    private static async Task Bootstrap(HttpClient client, Guid company, string username, string displayName, string password){using HttpRequestMessage r=new(HttpMethod.Post,"/api/v1/security/bootstrap"){Content=JsonContent.Create(new{companyId=company,username,displayName,password})};r.Headers.Add("X-Bootstrap-Secret","test-bootstrap-secret-123456789");using HttpResponseMessage response=await client.SendAsync(r,TestContext.Current.CancellationToken);response.EnsureSuccessStatusCode();}
    private static async Task<Login> LoginAsync(HttpClient client,string username,string password){Login login=(await (await client.PostAsJsonAsync("/api/v1/auth/login",new{username,password},TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<Login>(TestContext.Current.CancellationToken))!;return login;}
    private static async Task SelectCompany(HttpClient client,string token,Guid company){using HttpResponseMessage response=await client.SendAsync(Request(HttpMethod.Post,"/api/v1/auth/me/company",token,company,new{companyId=company}),TestContext.Current.CancellationToken);response.EnsureSuccessStatusCode();}
    private static async Task<Guid> UserId(PostgreSqlDatabaseFixture database,string username){await using CoreDbContext db=database.CreateAdministrationCoreDbContext();string normalized=username.Trim().ToUpperInvariant();return await db.ApplicationUsers.IgnoreQueryFilters().Where(x=>x.Username==normalized).Select(x=>x.Id).SingleAsync(TestContext.Current.CancellationToken);}
    private static HttpRequestMessage Request(HttpMethod method,string uri,string token,Guid company,object? body=null){HttpRequestMessage r=new(method,uri);r.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);r.Headers.Add("X-Company-ID",company.ToString());if(body is not null)r.Content=JsonContent.Create(body);return r;}
    private static async Task<JsonElement> Read(HttpClient client,HttpRequestMessage request){using HttpResponseMessage response=await client.SendAsync(request,TestContext.Current.CancellationToken);string payload=await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);Assert.True(response.IsSuccessStatusCode,$"{response.StatusCode}: {payload}");return JsonDocument.Parse(payload).RootElement.Clone();}
    private sealed record Login(string AccessToken);
}
