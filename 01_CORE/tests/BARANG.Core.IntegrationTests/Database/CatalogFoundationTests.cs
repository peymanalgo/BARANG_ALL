using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class CatalogFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact] public async Task MigrationCreatesCatalogTablesPermissionsAndRls()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
        string[] tables = await db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('products','product_variants','suppliers','supplier_products','equipment')").ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(5, tables.Length); Assert.Equal(6, await db.Permissions.CountAsync(x => x.Code == "product.read" || x.Code == "product.manage" || x.Code == "supplier.read" || x.Code == "supplier.manage" || x.Code == "equipment.read" || x.Code == "equipment.manage", TestContext.Current.CancellationToken));
    }
    [Fact] public async Task RlsIsolatesEveryCatalogTableAndPooledContextDoesNotLeak()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); Guid a = Guid.NewGuid(), b = Guid.NewGuid(); DateTimeOffset now = DateTimeOffset.UtcNow;
        await using (CoreDbContext db = database.CreateAdministrationCoreDbContext()) { db.Companies.AddRange(Company.Create(a, "CAT_A", "A", now).Value, Company.Create(b, "CAT_B", "B", now).Value); Product pa = Product.Create(Guid.NewGuid(), a, "P-A", "A", null, "APPAREL", "PCS", now).Value; Product pb = Product.Create(Guid.NewGuid(), b, "P-B", "B", null, "APPAREL", "PCS", now).Value; ProductVariant vb = ProductVariant.Create(Guid.NewGuid(), pb, "SKU-B", null, null, "M", "BLACK", now).Value; Supplier sb = Supplier.Create(Guid.NewGuid(), b, "S-B", "B Supplier", null, null, null, null, null, null, null, null, now).Value; db.Products.AddRange(pa, pb); db.ProductVariants.Add(vb); db.Suppliers.Add(sb); db.SupplierProducts.Add(SupplierProduct.Create(Guid.NewGuid(), b, sb.Id, pb.Id, vb.Id, null, true, 1, 2, now).Value); db.Equipment.Add(Equipment.Create(Guid.NewGuid(), b, "E-B", "Press", "PRESS", "AVAILABLE", null, null, null, null, null, null, now).Value); await db.SaveChangesAsync(TestContext.Current.CancellationToken); }
        await using NpgsqlConnection c = new(database.ConnectionString); await c.OpenAsync(TestContext.Current.CancellationToken); Assert.Equal(new long[] { 1, 0, 0, 0, 0 }, await Counts(c, a)); Assert.Equal(new long[] { 1, 1, 1, 1, 1 }, await Counts(c, b)); Assert.Equal(new long[] { 0, 0, 0, 0, 0 }, await Counts(c, null));
    }
    [Fact] public async Task CatalogEndpointsRejectAnonymousCallers()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString); using HttpClient client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products", TestContext.Current.CancellationToken)).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/suppliers", TestContext.Current.CancellationToken)).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/equipment", TestContext.Current.CancellationToken)).StatusCode);
    }
    [Fact] public async Task AuthenticatedCatalogFlowPersistsRelationshipsAuditAndConcurrency()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); Guid companyId = Guid.NewGuid(); await using (CoreDbContext db = database.CreateAdministrationCoreDbContext()) { db.Companies.Add(Company.Create(companyId, "CAT_E2E", "Catalog E2E", DateTimeOffset.UtcNow).Value); await db.SaveChangesAsync(TestContext.Current.CancellationToken); }
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString); using HttpClient client = factory.CreateClient(); using HttpRequestMessage bootstrap = new(HttpMethod.Post, "/api/v1/security/bootstrap") { Content = JsonContent.Create(new { companyId, username = "catalog-admin", displayName = "Catalog Administrator", password = "StrongCatalogPassword123" }) }; bootstrap.Headers.Add("X-Bootstrap-Secret", "test-bootstrap-secret-123456789"); (await client.SendAsync(bootstrap, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        LoginResult login = (await (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "catalog-admin", password = "StrongCatalogPassword123" }, TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<LoginResult>(TestContext.Current.CancellationToken))!; await Send<object>(client, Request(HttpMethod.Post, "/api/v1/auth/me/company", login.AccessToken, null, new { companyId }));
        ProductView product = await Send<ProductView>(client, Request(HttpMethod.Post, "/api/v1/products", login.AccessToken, companyId, new { productCode = " P-100 ", name = "Shirt", productTypeCode = "APPAREL", unitOfMeasureCode = "PCS" })); VariantView variant = await Send<VariantView>(client, Request(HttpMethod.Post, $"/api/v1/products/{product.Id}/variants", login.AccessToken, companyId, new { sku = "SKU-100-M", sizeCode = "M", colorCode = "BLACK" })); SupplierView supplier = await Send<SupplierView>(client, Request(HttpMethod.Post, "/api/v1/suppliers", login.AccessToken, companyId, new { supplierCode = "SUP-100", displayName = "Supplier" })); SupplierProductView link = await Send<SupplierProductView>(client, Request(HttpMethod.Post, $"/api/v1/suppliers/{supplier.Id}/products", login.AccessToken, companyId, new { productId = product.Id, productVariantId = variant.Id, isPreferred = true, minimumOrderQuantity = 5, leadTimeDays = 2 })); EquipmentView equipment = await Send<EquipmentView>(client, Request(HttpMethod.Post, "/api/v1/equipment", login.AccessToken, companyId, new { equipmentCode = "EQ-100", name = "Press", equipmentTypeCode = "PRESS", operationalStatusCode = "AVAILABLE" }));
        ProductView changedProduct = await Send<ProductView>(client, Request(HttpMethod.Put, $"/api/v1/products/{product.Id}", login.AccessToken, companyId, new { productCode = product.ProductCode, name = "Updated Shirt", productTypeCode = product.ProductTypeCode, unitOfMeasureCode = product.UnitOfMeasureCode, version = product.Version }));
        VariantView changedVariant = await Send<VariantView>(client, Request(HttpMethod.Put, $"/api/v1/product-variants/{variant.Id}", login.AccessToken, companyId, new { sku = variant.Sku, displayName = "Medium Black", barcode = "BAR-100", sizeCode = variant.SizeCode, colorCode = variant.ColorCode, version = variant.Version }));
        SupplierView changedSupplier = await Send<SupplierView>(client, Request(HttpMethod.Put, $"/api/v1/suppliers/{supplier.Id}", login.AccessToken, companyId, new { supplierCode = supplier.SupplierCode, displayName = "Updated Supplier", version = supplier.Version }));
        SupplierProductView changedLink = await Send<SupplierProductView>(client, Request(HttpMethod.Put, $"/api/v1/suppliers/{supplier.Id}/products/{link.Id}", login.AccessToken, companyId, new { productId = product.Id, productVariantId = variant.Id, supplierItemCode = "ITEM-100", isPreferred = true, minimumOrderQuantity = 6, leadTimeDays = 3, version = link.Version }));
        EquipmentView changedEquipment = await Send<EquipmentView>(client, Request(HttpMethod.Put, $"/api/v1/equipment/{equipment.Id}", login.AccessToken, companyId, new { equipmentCode = equipment.EquipmentCode, name = equipment.Name, equipmentTypeCode = equipment.EquipmentTypeCode, operationalStatusCode = "MAINTENANCE", version = equipment.Version }));

        await AssertConflict(client, Request(HttpMethod.Put, $"/api/v1/products/{product.Id}", login.AccessToken, companyId, new { productCode = product.ProductCode, name = "Stale", productTypeCode = product.ProductTypeCode, unitOfMeasureCode = product.UnitOfMeasureCode, version = product.Version }));
        await AssertConflict(client, Request(HttpMethod.Put, $"/api/v1/product-variants/{variant.Id}", login.AccessToken, companyId, new { sku = variant.Sku, displayName = "Stale", sizeCode = variant.SizeCode, colorCode = variant.ColorCode, version = variant.Version }));
        await AssertConflict(client, Request(HttpMethod.Put, $"/api/v1/suppliers/{supplier.Id}", login.AccessToken, companyId, new { supplierCode = supplier.SupplierCode, displayName = "Stale", version = supplier.Version }));
        await AssertConflict(client, Request(HttpMethod.Put, $"/api/v1/suppliers/{supplier.Id}/products/{link.Id}", login.AccessToken, companyId, new { productId = product.Id, productVariantId = variant.Id, isPreferred = false, version = link.Version }));
        await AssertConflict(client, Request(HttpMethod.Put, $"/api/v1/equipment/{equipment.Id}", login.AccessToken, companyId, new { equipmentCode = equipment.EquipmentCode, name = equipment.Name, equipmentTypeCode = equipment.EquipmentTypeCode, operationalStatusCode = "OUT_OF_SERVICE", version = equipment.Version }));

        await Send<ProductView>(client, Request(HttpMethod.Post, $"/api/v1/products/{product.Id}/activation", login.AccessToken, companyId, new { isActive = false, version = changedProduct.Version }));
        ProductOption[] productOptions = await Send<ProductOption[]>(client, Request(HttpMethod.Get, "/api/v1/products/options?limit=100", login.AccessToken, companyId));
        VariantOption[] variantOptions = await Send<VariantOption[]>(client, Request(HttpMethod.Get, $"/api/v1/products/{product.Id}/variants/options?limit=100", login.AccessToken, companyId));
        Assert.DoesNotContain(productOptions, x => x.Id == product.Id); Assert.Empty(variantOptions);
        await Send<SupplierView>(client, Request(HttpMethod.Post, $"/api/v1/suppliers/{supplier.Id}/activation", login.AccessToken, companyId, new { isActive = false, version = changedSupplier.Version }));
        await Send<SupplierProductView>(client, Request(HttpMethod.Post, $"/api/v1/suppliers/{supplier.Id}/products/{link.Id}/activation", login.AccessToken, companyId, new { isActive = false, version = changedLink.Version }));
        await Send<EquipmentView>(client, Request(HttpMethod.Post, $"/api/v1/equipment/{equipment.Id}/activation", login.AccessToken, companyId, new { isActive = false, version = changedEquipment.Version }));
        Assert.DoesNotContain(await Send<SupplierOption[]>(client, Request(HttpMethod.Get, "/api/v1/suppliers/options?limit=100", login.AccessToken, companyId)), x => x.Id == supplier.Id);
        Assert.DoesNotContain(await Send<EquipmentOption[]>(client, Request(HttpMethod.Get, "/api/v1/equipment/options?limit=100", login.AccessToken, companyId)), x => x.Id == equipment.Id);

        Guid otherCompanyId = Guid.NewGuid(); await using (CoreDbContext setup = database.CreateAdministrationCoreDbContext()) { setup.Companies.Add(Company.Create(otherCompanyId, "CAT_OTHER", "Other", DateTimeOffset.UtcNow).Value); await setup.SaveChangesAsync(TestContext.Current.CancellationToken); }
        using (HttpResponseMessage crossCompany = await client.SendAsync(Request(HttpMethod.Get, $"/api/v1/products/{product.Id}", login.AccessToken, otherCompanyId), TestContext.Current.CancellationToken)) { Assert.Equal(HttpStatusCode.NotFound, crossCompany.StatusCode); }

        await using (CoreDbContext verify = database.CreateAdministrationCoreDbContext())
        {
            string[] expectedActions = ["product.created", "product.updated", "product.deactivated", "product.variant.created", "product.variant.updated", "supplier.created", "supplier.updated", "supplier.deactivated", "supplier.product.created", "supplier.product.updated", "supplier.product.deactivated", "equipment.created", "equipment.status.changed", "equipment.deactivated"];
            string[] actual = await verify.AuditEvents.IgnoreQueryFilters().Where(x => x.CompanyId == companyId).Select(x => x.Action).ToArrayAsync(TestContext.Current.CancellationToken);
            Assert.All(expectedActions, action => Assert.Contains(action, actual));
            await verify.Database.ExecuteSqlRawAsync("DELETE FROM barang.role_permissions WHERE permission_id = (SELECT id FROM barang.permissions WHERE code = 'product.read')", TestContext.Current.CancellationToken);
        }
        using (HttpResponseMessage forbidden = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/products", login.AccessToken, companyId), TestContext.Current.CancellationToken)) { Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode); }
        await Send<object>(client, Request(HttpMethod.Post, "/api/v1/auth/logout", login.AccessToken, null));
        using (HttpResponseMessage loggedOut = await client.SendAsync(Request(HttpMethod.Get, "/api/v1/products", login.AccessToken, companyId), TestContext.Current.CancellationToken)) { Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode); }
    }
    private static HttpRequestMessage Request(HttpMethod method, string path, string token, Guid? company, object? body = null) { HttpRequestMessage request = new(method, path); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); if (company is not null) request.Headers.Add("X-Company-ID", company.ToString()); if (body is not null) request.Content = JsonContent.Create(body); return request; }
    private static async Task<T> Send<T>(HttpClient client, HttpRequestMessage request) { using (request) { using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken); response.EnsureSuccessStatusCode(); if (typeof(T) == typeof(object)) return default!; return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!; } }
    private static async Task AssertConflict(HttpClient client, HttpRequestMessage request) { using (request) { using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); } }
    private static async Task<long[]> Counts(NpgsqlConnection c, Guid? company) { await using NpgsqlTransaction tx = await c.BeginTransactionAsync(TestContext.Current.CancellationToken); if (company is not null) { await using NpgsqlCommand set = new("SELECT set_config('barang.company_id',@company,true)", c, tx); set.Parameters.AddWithValue("company", company.Value.ToString()); await set.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); } List<long> values = []; foreach (string table in new[] { "products", "product_variants", "suppliers", "supplier_products", "equipment" }) { await using NpgsqlCommand count = new($"SELECT count(*) FROM barang.{table}", c, tx); values.Add(Convert.ToInt64(await count.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture)); } await tx.RollbackAsync(TestContext.Current.CancellationToken); return [.. values]; }
}
