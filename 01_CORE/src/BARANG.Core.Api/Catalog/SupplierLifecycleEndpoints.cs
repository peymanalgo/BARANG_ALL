using BARANG.Core.Application.Catalog;
using BARANG.Core.Api.Errors;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Api.Catalog;

public static class SupplierLifecycleEndpoints
{
    public static IEndpointRouteBuilder MapSupplierLifecycleEndpoints(this IEndpointRouteBuilder e)
    {
        RouteGroupBuilder r = e.MapGroup("/api/v1/suppliers/{supplier:guid}/lifecycle");
        r.MapGet("/profile", async (Guid supplier, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.ProfileAsync(supplier, ct))).RequireAuthorization(SecurityPermissions.SupplierRead);
        r.MapPost("/approval", async (Guid supplier, SupplierApprovalCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SetApprovalAsync(supplier, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/hold", async (Guid supplier, SupplierHoldCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SetHoldAsync(supplier, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapGet("/evaluations", async (Guid supplier, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.EvaluationsAsync(supplier, ct))).RequireAuthorization(SecurityPermissions.SupplierRead);
        r.MapPost("/evaluations", async (Guid supplier, SupplierEvaluationCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.CreateEvaluationAsync(supplier, c, ct), true)).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPut("/evaluations/{id:guid}", async (Guid supplier, Guid id, SupplierEvaluationCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.UpdateEvaluationAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapGet("/quality-incidents", async (Guid supplier, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.IncidentsAsync(supplier, ct))).RequireAuthorization(SecurityPermissions.SupplierRead);
        r.MapPost("/quality-incidents", async (Guid supplier, SupplierQualityIncidentCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.CreateIncidentAsync(supplier, c, ct), true)).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPut("/quality-incidents/{id:guid}", async (Guid supplier, Guid id, SupplierQualityIncidentCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.UpdateIncidentAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/quality-incidents/{id:guid}/activation", async (Guid supplier, Guid id, bool active, long version, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SetIncidentActiveAsync(supplier, id, active, version, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/quality-incidents/{id:guid}/resolve", async (Guid supplier, Guid id, SupplierQualityResolveCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.ResolveIncidentAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapGet("/contracts", async (Guid supplier, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.ContractsAsync(supplier, ct))).RequireAuthorization(SecurityPermissions.SupplierRead);
        r.MapPost("/contracts", async (Guid supplier, SupplierContractCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SaveContractAsync(supplier, null, c, ct), true)).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPut("/contracts/{id:guid}", async (Guid supplier, Guid id, SupplierContractCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SaveContractAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/contracts/{id:guid}/file", async (Guid supplier, Guid id, SupplierContractFileCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.AttachContractFileAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapDelete("/contracts/{id:guid}", async (Guid supplier, Guid id, long version, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.DeleteContractAsync(supplier, id, version, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapGet("/documents", async (Guid supplier, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.DocumentsAsync(supplier, ct))).RequireAuthorization(SecurityPermissions.SupplierRead);
        r.MapPost("/documents", async (Guid supplier, SupplierDocumentCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SaveDocumentAsync(supplier, null, c, ct), true)).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/documents/{id:guid}/file", async (Guid supplier, Guid id, SupplierFileCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.AttachDocumentFileAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPut("/documents/{id:guid}", async (Guid supplier, Guid id, SupplierDocumentCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.UpdateDocumentAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapPost("/documents/{id:guid}/status", async (Guid supplier, Guid id, SupplierDocumentStatusCommand c, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.SetDocumentStatusAsync(supplier, id, c, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        r.MapDelete("/documents/{id:guid}", async (Guid supplier, Guid id, long version, ISupplierLifecycleAdministration s, CancellationToken ct) => Map(await s.DeleteDocumentAsync(supplier, id, version, ct))).RequireAuthorization(SecurityPermissions.SupplierManage);
        return e;
    }
    private static IResult Map(Result r) => r.IsSuccess ? Results.NoContent() : Results.Problem(ApiProblemDetailsMapper.Create(r.Error, "supplier.lifecycle", null));
    private static IResult Map<T>(Result<T> r, bool created = false) => r.IsSuccess ? Results.Json(r.Value, statusCode: created ? 201 : 200) : Results.Problem(ApiProblemDetailsMapper.Create(r.Error, "supplier.lifecycle", null));
}
