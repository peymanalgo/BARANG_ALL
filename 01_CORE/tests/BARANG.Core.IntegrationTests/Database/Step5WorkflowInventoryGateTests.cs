using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class Step5WorkflowInventoryGateTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task RuntimeInventoryStagesRequireCompleteEvidenceAndAreRetrySafe()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        Guid company = Guid.NewGuid(), branch = Guid.NewGuid(), customer = Guid.NewGuid(), warehouse = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Product product;
        ProductVariant variant;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext())
        {
            db.Companies.Add(Company.Create(company, "STEP5_GATE", "STEP5 gate", now).Value);
            db.Branches.Add(Branch.Create(branch, company, "MAIN", "Main", now).Value);
            db.Customers.Add(Customer.Create(customer, company, "C-5", CustomerType.Organization, null, null, null, "STEP5 customer", null, null, null, null, null, null, null, now).Value);
            product = Product.Create(Guid.NewGuid(), company, "P-5", "STEP5 product", null, "RAW", "PCS", now).Value;
            variant = ProductVariant.Create(Guid.NewGuid(), product, "SKU-5", "STEP5", null, null, null, now).Value;
            db.Products.Add(product);
            db.ProductVariants.Add(variant);
            db.Warehouses.Add(Warehouse.Create(warehouse, company, branch, "W5", "STEP5 warehouse", WarehouseType.RawMaterials, now).Value);
            StockBalance balance = StockBalance.Create(company, warehouse, variant.Id, now);
            Assert.True(balance.ApplyOnHand(10, false, now).IsSuccess);
            db.StockBalances.Add(balance);
            await db.SaveChangesAsync(ct);
        }

        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId = company, username = "step5-gate-admin", displayName = "STEP5 Gate Administrator", password = "StrongStep5Password123" }) };
        bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789");
        (await client.SendAsync(bootstrap, ct)).EnsureSuccessStatusCode();
        await SeedInventoryWorkflowAsync(database.ConnectionString, company, "step5-gate-admin", ct);
        Login login = (await (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "step5-gate-admin", password = "StrongStep5Password123" }, ct)).Content.ReadFromJsonAsync<Login>(ct))!;
        await Send<object>(client, Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, null, new { companyId = company }));
        CurrentUserView me = await Send<CurrentUserView>(client, Request(HttpMethod.Get, "/api/v1/auth/me", login.AccessToken, company));
        await Send<object>(client, Request(HttpMethod.Post, $"/api/v1/inventory/warehouses/{warehouse}/responsibilities", login.AccessToken, company, new { applicationUserId = me.UserId }));

        OrderView draft = await Send<OrderView>(client, Request(HttpMethod.Post, "/api/v1/orders", login.AccessToken, company, new { branchId = branch, customerId = customer, source = "Internal", orderDate = "2026-10-15" }));
        OrderItemView first = await Send<OrderItemView>(client, Request(HttpMethod.Post, $"/api/v1/orders/{draft.Id}/items", login.AccessToken, company, new { productId = product.Id, productVariantId = variant.Id, quantity = 2 }));
        OrderItemView second = await Send<OrderItemView>(client, Request(HttpMethod.Post, $"/api/v1/orders/{draft.Id}/items", login.AccessToken, company, new { productId = product.Id, productVariantId = variant.Id, quantity = 3 }));
        draft = await Send<OrderView>(client, Request(HttpMethod.Get, $"/api/v1/orders/{draft.Id}", login.AccessToken, company));
        OrderView submitted = await Send<OrderView>(client, Request(HttpMethod.Post, $"/api/v1/orders/{draft.Id}/submit", login.AccessToken, company, new { version = draft.Version }));

        TaskView reservationTask = await TaskFor(client, login.AccessToken, company, "InventoryReservation", ct);
        reservationTask = await Send<TaskView>(client, Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{reservationTask.Id}/claim", login.AccessToken, company, new { version = reservationTask.Version }));
        await Send<object>(client, Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{reservationTask.Id}/complete", login.AccessToken, company, new { version = reservationTask.Version, resultJson = JsonSerializer.Serialize(new { outcome = "Approve", warehouseId = warehouse, idempotencyKey = $"workflow-task:{reservationTask.Id:N}" }) }));

        TaskView issueTask = await TaskFor(client, login.AccessToken, company, "InventoryIssue", ct);
        issueTask = await Send<TaskView>(client, Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{issueTask.Id}/claim", login.AccessToken, company, new { version = issueTask.Version }));
        using (HttpResponseMessage incomplete = await client.SendAsync(Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{issueTask.Id}/complete", login.AccessToken, company, new { version = issueTask.Version, resultJson = JsonSerializer.Serialize(new { outcome = "Approve", warehouseId = warehouse, reference = $"step5-issue:{issueTask.Id:N}", items = new[] { new { orderItemId = first.Id, quantity = 2, uomCode = "PCS" } } }) }), ct))
            Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);

        Balance[] before = await Send<Balance[]>(client, Request(HttpMethod.Get, $"/api/v1/inventory/balances?warehouseId={warehouse}&variantId={variant.Id}", login.AccessToken, company));
        Assert.Equal(10, before.Single().OnHand);
        Assert.Equal(5, before.Single().Reserved);
        TaskView stillOpen = await TaskFor(client, login.AccessToken, company, "InventoryIssue", ct);
        Assert.Equal(issueTask.Id, stillOpen.Id);

        await Send<object>(client, Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{issueTask.Id}/complete", login.AccessToken, company, new { version = issueTask.Version, resultJson = JsonSerializer.Serialize(new { outcome = "Approve", warehouseId = warehouse, reference = $"step5-issue:{issueTask.Id:N}", items = new[] { new { orderItemId = first.Id, quantity = 2, uomCode = "PCS" }, new { orderItemId = second.Id, quantity = 3, uomCode = "PCS" } } }) }));
        await Send<object>(client, Request(HttpMethod.Post, $"/api/v1/workflows/definition/runtime/tasks/{issueTask.Id}/complete", login.AccessToken, company, new { version = issueTask.Version, resultJson = "{}" }));
        Balance[] after = await Send<Balance[]>(client, Request(HttpMethod.Get, $"/api/v1/inventory/balances?warehouseId={warehouse}&variantId={variant.Id}", login.AccessToken, company));
        Assert.Equal(5, after.Single().OnHand);
        Assert.Equal(0, after.Single().Reserved);
        Transaction[] movements = await Send<Transaction[]>(client, Request(HttpMethod.Get, $"/api/v1/inventory/transactions?warehouseId={warehouse}&variantId={variant.Id}", login.AccessToken, company));
        Assert.Equal(2, movements.Count(x => x.SourceType == "WAREHOUSE_ISSUE"));
        JsonElement order360 = await Send<JsonElement>(client, Request(HttpMethod.Get, $"/api/v1/orders/{submitted.Id}/360", login.AccessToken, company));
        JsonElement[] order360Items = order360.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, order360Items.Length);
        Assert.Equal(first.Id, order360Items[0].GetProperty("id").GetGuid());
        Assert.Equal(second.Id, order360Items[1].GetProperty("id").GetGuid());
        Assert.Equal(2m, order360Items[0].GetProperty("quantity").GetDecimal());
        Assert.Equal(3m, order360Items[1].GetProperty("quantity").GetDecimal());
        _ = submitted;
    }

    private static async Task<TaskView> TaskFor(HttpClient client, string token, Guid company, string stage, CancellationToken ct)
    {
        using HttpResponseMessage response = await client.SendAsync(Request(HttpMethod.Get, $"/api/v1/workflows/definition/runtime/tasks/eligible?stageKey={stage}&page=1&pageSize=10", token, company), ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, body);
        using JsonDocument json = JsonDocument.Parse(body);
        JsonElement item = json.RootElement.GetProperty("items").EnumerateArray().Single();
        return new(item.GetProperty("id").GetGuid(), item.GetProperty("version").GetInt64());
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, string token, Guid? company, object? body = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (company is not null) request.Headers.Add("X-Company-ID", company.Value.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<T> Send<T>(HttpClient client, HttpRequestMessage request)
    {
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return typeof(T) == typeof(object) ? (T)(object)new() : (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
    }

    private static async Task SeedInventoryWorkflowAsync(string connectionString, Guid company, string username, CancellationToken ct)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await using (NpgsqlCommand context = new("select set_config('barang.company_id', @company, false)", connection, transaction))
        {
            context.Parameters.AddWithValue("company", company.ToString());
            await context.ExecuteNonQueryAsync(ct);
        }
        Guid actor;
        await using (NpgsqlCommand actorCommand = new("select id from barang.application_users where upper(username)=upper(@username)", connection, transaction))
        {
            actorCommand.Parameters.AddWithValue("username", username);
            actor = (Guid)(await actorCommand.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("STEP5 actor missing."));
        }
        Guid definition = Guid.NewGuid(), version = Guid.NewGuid(), start = Guid.NewGuid(), reservation = Guid.NewGuid(), issue = Guid.NewGuid(), end = Guid.NewGuid();
        Guid[] transitions = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        string sql = """
            insert into barang.workflow_definitions(id,company_id,code,name_json,created_at_utc,updated_at_utc,version) values(@d,@c,'BARANG.ORDER','{"fa":"سفارش","en":"Order","ar":"طلب"}'::jsonb,@n,@n,1);
            insert into barang.workflow_definition_versions(id,company_id,definition_id,version_number,status,change_note_json,created_at_utc,published_at_utc,created_by_user_id,published_by_user_id,version) values(@v,@c,@d,1,'Active','{}'::jsonb,@n,@n,@a,@a,1);
            update barang.workflow_definitions set active_version_id=@v where id=@d and company_id=@c;
            insert into barang.workflow_definition_nodes(id,company_id,definition_version_id,node_key,type,title_json,description_json,x,y,version) values
              (@s,@c,@v,'start','Start','{"fa":"شروع","en":"Start","ar":"بدء"}'::jsonb,'{}'::jsonb,0,0,1),
              (@r,@c,@v,'InventoryReservation','UserTask','{"fa":"رزرو","en":"Inventory reservation","ar":"حجز"}'::jsonb,'{}'::jsonb,100,0,1),
              (@i,@c,@v,'InventoryIssue','UserTask','{"fa":"صدور","en":"Inventory issue","ar":"صرف"}'::jsonb,'{}'::jsonb,200,0,1),
              (@e,@c,@v,'end','End','{"fa":"پایان","en":"End","ar":"نهاية"}'::jsonb,'{}'::jsonb,300,0,1);
            insert into barang.workflow_definition_transitions(id,company_id,definition_version_id,from_node_id,to_node_id,condition_json,is_default,sort_order,version) values
              (@t1,@c,@v,@s,@r,'{}'::jsonb,true,0,1),(@t2,@c,@v,@r,@i,'{}'::jsonb,true,1,1),(@t3,@c,@v,@i,@e,'{}'::jsonb,true,2,1);
            """;
        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("d", definition); command.Parameters.AddWithValue("v", version); command.Parameters.AddWithValue("s", start); command.Parameters.AddWithValue("r", reservation); command.Parameters.AddWithValue("i", issue); command.Parameters.AddWithValue("e", end); command.Parameters.AddWithValue("t1", transitions[0]); command.Parameters.AddWithValue("t2", transitions[1]); command.Parameters.AddWithValue("t3", transitions[2]); command.Parameters.AddWithValue("c", company); command.Parameters.AddWithValue("a", actor); command.Parameters.AddWithValue("n", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private sealed record Login(string AccessToken);
    private sealed record CurrentUserView(Guid UserId);
    private sealed record OrderView(Guid Id, long Version);
    private sealed record OrderItemView(Guid Id);
    private sealed record TaskView(Guid Id, long Version);
    private sealed record Balance(Guid WarehouseId, Guid ProductVariantId, decimal OnHand, decimal Reserved, decimal Available, long Version);
    private sealed record Transaction(Guid Id, Guid? SourceId, string SourceType);
}
