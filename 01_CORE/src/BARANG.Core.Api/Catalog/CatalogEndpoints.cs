using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapProducts(endpoints); MapSuppliers(endpoints); MapEquipment(endpoints); return endpoints;
    }
    private static void MapProducts(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root = endpoints.MapGroup("/api/v1/products"); RouteGroupBuilder read = root.MapGroup("").RequireAuthorization(SecurityPermissions.ProductRead);
        read.MapGet("/", async ([AsParameters] CatalogSearchRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SearchProductsAsync(r.Query(), ct)));
        read.MapGet("/options", async (string? search, int? limit, ICatalogAdministration s, CancellationToken ct) => Map(await s.ProductOptionsAsync(search, limit ?? 20, ct)));
        read.MapGet("/{id:guid}", async (Guid id, ICatalogAdministration s, CancellationToken ct) => Map(await s.GetProductAsync(id, ct)));
        read.MapGet("/{id:guid}/variants", async (Guid id, bool? activeOnly, ICatalogAdministration s, CancellationToken ct) => Map(await s.ListVariantsAsync(id, activeOnly ?? false, ct)));
        read.MapGet("/{id:guid}/variants/options", async (Guid id, string? search, int? limit, ICatalogAdministration s, CancellationToken ct) => Map(await s.VariantOptionsAsync(id, search, limit ?? 20, ct)));
        RouteGroupBuilder manage = root.MapGroup("").RequireAuthorization(SecurityPermissions.ProductManage);
        manage.MapPost("/", async (ProductRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.CreateProductAsync(r.Command(), ct), true));
        manage.MapPut("/{id:guid}", async (Guid id, ProductRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.UpdateProductAsync(id, r.Command(), ct)));
        manage.MapPost("/{id:guid}/activation", async (Guid id, ActivationRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SetProductActiveAsync(id, r.IsActive, r.Version, ct)));
        manage.MapPost("/{id:guid}/variants", async (Guid id, VariantRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.CreateVariantAsync(id, r.Command(), ct), true));
        RouteGroupBuilder variants = endpoints.MapGroup("/api/v1/product-variants").RequireAuthorization(SecurityPermissions.ProductManage);
        variants.MapPut("/{id:guid}", async (Guid id, VariantRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.UpdateVariantAsync(id, r.Command(), ct)));
        variants.MapPost("/{id:guid}/activation", async (Guid id, ActivationRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SetVariantActiveAsync(id, r.IsActive, r.Version, ct)));
    }
    private static void MapSuppliers(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root = endpoints.MapGroup("/api/v1/suppliers"); RouteGroupBuilder read = root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierRead);
        read.MapGet("/", async ([AsParameters] CatalogSearchRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SearchSuppliersAsync(r.Query(), ct))); read.MapGet("/options", async (string? search, int? limit, ICatalogAdministration s, CancellationToken ct) => Map(await s.SupplierOptionsAsync(search, limit ?? 20, ct))); read.MapGet("/{id:guid}", async (Guid id, ICatalogAdministration s, CancellationToken ct) => Map(await s.GetSupplierAsync(id, ct))); read.MapGet("/{id:guid}/products", async (Guid id, bool? activeOnly, ICatalogAdministration s, CancellationToken ct) => Map(await s.ListSupplierProductsAsync(id, activeOnly ?? false, ct)));
        RouteGroupBuilder manage = root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierManage);
        manage.MapPost("/", async (SupplierRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.CreateSupplierAsync(r.Command(), ct), true)); manage.MapPut("/{id:guid}", async (Guid id, SupplierRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.UpdateSupplierAsync(id, r.Command(), ct))); manage.MapPost("/{id:guid}/activation", async (Guid id, ActivationRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SetSupplierActiveAsync(id, r.IsActive, r.Version, ct))); manage.MapPost("/{id:guid}/products", async (Guid id, SupplierProductRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.CreateSupplierProductAsync(id, r.Command(), ct), true)); manage.MapPut("/{id:guid}/products/{linkId:guid}", async (Guid id, Guid linkId, SupplierProductRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.UpdateSupplierProductAsync(id, linkId, r.Command(), ct))); manage.MapPost("/{id:guid}/products/{linkId:guid}/activation", async (Guid id, Guid linkId, ActivationRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SetSupplierProductActiveAsync(id, linkId, r.IsActive, r.Version, ct)));
    }
    private static void MapEquipment(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root = endpoints.MapGroup("/api/v1/equipment"); RouteGroupBuilder read = root.MapGroup("").RequireAuthorization(SecurityPermissions.EquipmentRead);
        read.MapGet("/", async ([AsParameters] CatalogSearchRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SearchEquipmentAsync(r.Query(), ct))); read.MapGet("/options", async (string? search, string? typeCode, string? statusCode, Guid? branchId, int? limit, ICatalogAdministration s, CancellationToken ct) => Map(await s.EquipmentOptionsAsync(search, typeCode, statusCode, branchId, limit ?? 20, ct))); read.MapGet("/{id:guid}", async (Guid id, ICatalogAdministration s, CancellationToken ct) => Map(await s.GetEquipmentAsync(id, ct)));
        RouteGroupBuilder manage = root.MapGroup("").RequireAuthorization(SecurityPermissions.EquipmentManage);
        manage.MapPost("/", async (EquipmentRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.CreateEquipmentAsync(r.Command(), ct), true)); manage.MapPut("/{id:guid}", async (Guid id, EquipmentRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.UpdateEquipmentAsync(id, r.Command(), ct))); manage.MapPost("/{id:guid}/activation", async (Guid id, ActivationRequest r, ICatalogAdministration s, CancellationToken ct) => Map(await s.SetEquipmentActiveAsync(id, r.IsActive, r.Version, ct)));
    }
    private static CatalogResult<T> Map<T>(Result<T> result, bool created = false) => new(result, created);
    private sealed class CatalogResult<T>(Result<T> result, bool created) : IResult { public Task ExecuteAsync(HttpContext c) { if (result.IsSuccess) { c.Response.StatusCode = created ? 201 : 200; return c.Response.WriteAsJsonAsync(result.Value, c.RequestAborted); } ICorrelationContext correlation = c.RequestServices.GetRequiredService<ICorrelationContext>(); ProblemDetails p = ApiProblemDetailsMapper.Create(result.Error, correlation.CorrelationId ?? c.TraceIdentifier, c.Request.Path); c.Response.StatusCode = p.Status!.Value; return c.Response.WriteAsJsonAsync(p, c.RequestAborted); } }
}
public sealed record CatalogSearchRequest(string? Search = null, bool ActiveOnly = false, int PageNumber = 1, int PageSize = 20, string? TypeCode = null, string? StatusCode = null, Guid? BranchId = null) { public CatalogQuery Query() => new(Search, ActiveOnly, PageNumber, PageSize, TypeCode, StatusCode, BranchId); }
public sealed record ProductRequest(string ProductCode, string Name, string? Description, string ProductTypeCode, string UnitOfMeasureCode, long Version = 0) { public ProductCommand Command() => new(ProductCode, Name, Description, ProductTypeCode, UnitOfMeasureCode, Version); }
public sealed record VariantRequest(string Sku, string? DisplayName, string? Barcode, string? SizeCode, string? ColorCode, long Version = 0) { public VariantCommand Command() => new(Sku, DisplayName, Barcode, SizeCode, ColorCode, Version); }
public sealed record SupplierRequest(string SupplierCode, string DisplayName, string? LegalName, string? RegistrationNo, string? TaxId, string? PrimaryEmail, string? PrimaryPhone, string? PrimaryMobile, string? Website, string? Notes, long Version = 0) { public SupplierCommand Command() => new(SupplierCode, DisplayName, LegalName, RegistrationNo, TaxId, PrimaryEmail, PrimaryPhone, PrimaryMobile, Website, Notes, Version); }
public sealed record SupplierProductRequest(Guid ProductId, Guid? ProductVariantId, string? SupplierItemCode, bool IsPreferred, decimal? MinimumOrderQuantity, int? LeadTimeDays, long Version = 0) { public SupplierProductCommand Command() => new(ProductId, ProductVariantId, SupplierItemCode, IsPreferred, MinimumOrderQuantity, LeadTimeDays, Version); }
public sealed record EquipmentRequest(string EquipmentCode, string Name, string EquipmentTypeCode, string OperationalStatusCode, Guid? BranchId, Guid? OrganizationalUnitId, string? Description, string? Manufacturer, string? Model, string? SerialNumber, long Version = 0) { public EquipmentCommand Command() => new(EquipmentCode, Name, EquipmentTypeCode, OperationalStatusCode, BranchId, OrganizationalUnitId, Description, Manufacturer, Model, SerialNumber, Version); }
public sealed record ActivationRequest(bool IsActive, long Version);
