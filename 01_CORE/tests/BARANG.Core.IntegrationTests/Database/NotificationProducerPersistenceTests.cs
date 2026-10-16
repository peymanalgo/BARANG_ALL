using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Background;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Logistics;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Production;
using BARANG.Core.Domain.Quality;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Workflow;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class NotificationProducerPersistenceTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task QualityLogisticsAndSlaTransitionsPersistExactNotificationEvents()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = Guid.NewGuid(), branch = Guid.NewGuid(), customer = Guid.NewGuid(), productId = Guid.NewGuid(), variantId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "NOTIF_PRODUCERS", "Notification producer evidence", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            db.Customers.Add(Customer.Create(customer, company, "N-001", CustomerType.Organization, null, null, null, "Notification buyer", null, null, null, null, null, null, null, now).Value);
            Product product = Product.Create(productId, company, "N-PROD", "Notification product", null, "APPAREL", "PCS", now).Value;
            ProductVariant variant = ProductVariant.Create(variantId, product, "N-SKU", "Notification SKU", null, null, null, now).Value;
            db.Products.Add(product); db.ProductVariants.Add(variant);
            await db.SaveChangesAsync(ct);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString, "Development");
        using HttpClient client = factory.CreateClient();
        const string username = "notification-producer-admin";
        const string password = "StrongNotificationPassword123";
        using (HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = company, username, displayName = "Notification producer admin", password }) })
        {
            bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
            (await client.SendAsync(bootstrap, ct)).EnsureSuccessStatusCode();
        }
        Login login = (await (await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, ct)).Content.ReadFromJsonAsync<Login>(ct))!;
        using (HttpRequestMessage select = Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, company, new { companyId = company }))
            (await client.SendAsync(select, ct)).EnsureSuccessStatusCode();
        Guid actor = await UserId(username, ct);

        Guid orderId = Guid.NewGuid(), itemId = Guid.NewGuid();
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            Order order = Order.CreateDraft(orderId, company, branch, customer, OrderSource.Internal, null, DateOnly.FromDateTime(now.UtcDateTime), null, actor, now).Value;
            Assert.True(order.Submit("ORD-NOTIF-1", now).IsSuccess);
            db.Orders.Add(order);
            db.OrderItems.Add(OrderItem.Create(itemId, company, orderId, 1, productId, variantId, 1m, null, now).Value);
            ProductionJob job = ProductionJob.Create(company, orderId, itemId, null, ProductionJobType.DesignPreparation, null, null, now).Value;
            Assert.True(job.Plan(ProductionPriority.Normal, now.AddMinutes(-10), now.AddMinutes(-5), 1, job.Version, now).IsSuccess);
            Assert.True(job.AssignOperator(actor, job.Version, now).IsSuccess);
            Assert.True(job.Start(job.Version, now).IsSuccess);
            Assert.True(job.Complete(1m, "completed output", job.Version, now).IsSuccess);
            db.ProductionJobs.Add(job);
            QCChecklistTemplate template = QCChecklistTemplate.Create(company, "N-QC", "Notification QC", ProductionJobType.DesignPreparation, null, now).Value;
            db.QCTemplates.Add(template);
            db.QCTemplateCriteria.Add(QCChecklistCriterion.Create(company, template.Id, "PASS", "Pass", QCCriterionType.PassFail, false, 1, null, null, null).Value);
            WorkflowInstance instance = WorkflowInstance.Create(company, orderId, "notification-instance", now);
            HumanTask task = HumanTask.Create(company, instance.Id, orderId, WorkflowStage.QualityControl, "QUALITY", SecurityPermissions.QCReview, 1, now.AddMinutes(-10), now.AddMinutes(-1));
            db.WorkflowInstances.Add(instance); db.HumanTasks.Add(task);
            db.WorkflowSlaPolicies.Add(WorkflowSlaPolicy.Create(company, WorkflowStage.QualityControl, 1, SecurityPermissions.QCReview, now).Value);
            Package package = Package.Create(company, orderId, "PKG-NOTIF", "BOX", 1, 10, 10, 10, now).Value;
            Assert.True(package.Complete(actor, package.Version, now).IsSuccess);
            db.Packages.Add(package);
            db.PackageItems.Add(PackageItem.Create(company, package.Id, orderId, itemId, 1m, now).Value);
            Shipment shipment = Shipment.Create(company, orderId, "SHP-NOTIF", "LOCAL", "STANDARD", null, now).Value;
            db.Shipments.Add(shipment); db.ShipmentPackages.Add(ShipmentPackage.Create(company, shipment.Id, package.Id, now));
            await db.SaveChangesAsync(ct);
            await QualityAndLogistics(client, login.AccessToken, company, actor, job.Id, template.Id, shipment.Id, task.Id, orderId, itemId, ct);
        }
    }

    private async Task QualityAndLogistics(HttpClient client, string token, Guid company, Guid actor, Guid jobId, Guid templateId, Guid shipmentId, Guid taskId, Guid orderId, Guid itemId, CancellationToken ct)
    {
        JsonElement inspection = await Read(client, Request(HttpMethod.Post, "/api/v1/quality/inspections", token, company, new { productionJobId = jobId, templateId }), ct);
        Guid inspectionId = inspection.GetProperty("id").GetGuid();
        long inspectionVersion = inspection.GetProperty("version").GetInt64();
        await Read(client, Request(HttpMethod.Put, $"/api/v1/quality/inspections/{inspectionId}/disposition", token, company, new { acceptedQuantity = 0m, rejectedQuantity = 0m, reproductionRequiredQuantity = 1m, acceptedReason = "not accepted", rejectedReason = (string?)null, reproductionReason = "rework required", version = inspectionVersion }), ct);
        JsonElement rejected = await Read(client, Request(HttpMethod.Post, $"/api/v1/quality/inspections/{inspectionId}/reject", token, company, new { reason = "rework required", version = inspectionVersion }), ct);
        Assert.Equal((int)QCInspectionStatus.Rejected, rejected.GetProperty("status").GetInt32());
        JsonElement reproduction = await Read(client, Request(HttpMethod.Post, $"/api/v1/quality/inspections/{inspectionId}/reproduction", token, company, new { requestedStage = 0, quantity = 1m, reason = "rework required" }), ct);
        Guid reproductionId = reproduction.GetProperty("id").GetGuid();
        await AssertEvent("quality.rejected", inspectionId, "quality.rejected", actor, new Dictionary<string, Guid> { ["inspectionId"] = inspectionId, ["orderItemId"] = itemId }, ct);
        await AssertEvent("quality.reproduction.requested", reproductionId, "quality.reproduction.requested", actor, new Dictionary<string, Guid> { ["reproductionRequestId"] = reproductionId, ["inspectionId"] = inspectionId }, ct);

        JsonElement tracked = await Read(client, Request(HttpMethod.Put, $"/api/v1/logistics/shipments/{shipmentId}/tracking", token, company, new { trackingCode = "N-TRACK", version = 1L }), ct);
        JsonElement shipped = await Read(client, Request(HttpMethod.Post, $"/api/v1/logistics/shipments/{shipmentId}/ship", token, company, new { version = tracked.GetProperty("version").GetInt64() }), ct);
        await AssertEvent("shipment.dispatched", shipmentId, "shipment.dispatched", actor, new Dictionary<string, Guid> { ["shipmentId"] = shipmentId, ["orderId"] = orderId }, ct);
        JsonElement inTransit = await Read(client, Request(HttpMethod.Post, $"/api/v1/logistics/shipments/{shipmentId}/in-transit", token, company, new { version = shipped.GetProperty("version").GetInt64() }), ct);
        JsonElement delivered = await Read(client, Request(HttpMethod.Post, $"/api/v1/logistics/shipments/{shipmentId}/deliver", token, company, new { version = inTransit.GetProperty("version").GetInt64() }), ct);
        Assert.Equal((int)ShipmentStatus.Delivered, delivered.GetProperty("status").GetInt32());
        await AssertEvent("shipment.delivered", shipmentId, "shipment.delivered", actor, new Dictionary<string, Guid> { ["shipmentId"] = shipmentId, ["orderId"] = orderId }, ct);

        await Read(client, Request(HttpMethod.Post, "/api/v1/workflows/escalations/process", token, company), ct);
        await AssertEvent("workflow.sla.escalated", taskId, "workflow.sla.escalated", actor, new Dictionary<string, Guid> { ["taskId"] = taskId, ["orderId"] = orderId }, ct);
        JsonElement second = await Read(client, Request(HttpMethod.Post, "/api/v1/workflows/escalations/process", token, company), ct);
        Assert.Equal(0, second.GetInt32());
    }

    private async Task AssertEvent(string type, Guid stableKey, string template, Guid recipient, IReadOnlyDictionary<string, Guid> source, CancellationToken ct)
    {
        await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
        using IDisposable scope = database.CompanyContext.BeginScope(await CompanyId(ct));
        List<OutboxMessage> rows = await db.OutboxMessages.IgnoreQueryFilters().Where(x => x.EventType == "notification.requested" && x.CorrelationId!.StartsWith(type + ":" + stableKey.ToString("N"))).ToListAsync(ct);
        Assert.Single(rows);
        using JsonDocument outer = JsonDocument.Parse(rows[0].Payload);
        Assert.Equal(recipient, outer.RootElement.GetProperty("recipientUserId").GetGuid());
        Assert.Equal(template, outer.RootElement.GetProperty("templateCode").GetString());
        using JsonDocument dispatch = JsonDocument.Parse(outer.RootElement.GetProperty("payload").GetString()!);
        JsonElement inner = dispatch.RootElement.GetProperty("payload");
        foreach ((string key, Guid value) in source) Assert.Equal(value, inner.GetProperty(key).GetGuid());
        Assert.Equal(await CompanyId(ct), rows[0].CompanyId);
    }

    private async Task<Guid> CompanyId(CancellationToken ct) { await using CoreDbContext db = database.CreateAdministrationCoreDbContext(); return await db.Companies.IgnoreQueryFilters().Where(x => x.Code == "NOTIF_PRODUCERS").Select(x => x.Id).SingleAsync(ct); }
    private async Task<Guid> UserId(string username, CancellationToken ct) { await using CoreDbContext db = database.CreateAdministrationCoreDbContext(); string normalized = username.Trim().ToUpperInvariant(); return await db.ApplicationUsers.IgnoreQueryFilters().Where(x => x.Username == normalized).Select(x => x.Id).SingleAsync(ct); }
    private static HttpRequestMessage Request(HttpMethod method, string uri, string token, Guid company, object? body = null) { HttpRequestMessage request = new(method, uri); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); request.Headers.Add("X-Company-ID", company.ToString()); if (body is not null) request.Content = JsonContent.Create(body); return request; }
    private static async Task<JsonElement> Read(HttpClient client, HttpRequestMessage request, CancellationToken ct) { using HttpResponseMessage response = await client.SendAsync(request, ct); string body = await response.Content.ReadAsStringAsync(ct); Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}"); return JsonDocument.Parse(body).RootElement.Clone(); }
    private sealed record Login(string AccessToken);
}
