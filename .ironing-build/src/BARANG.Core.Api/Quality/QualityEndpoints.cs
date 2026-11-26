using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Quality;
using BARANG.Core.Domain.Common;using BARANG.Core.Domain.Quality;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Api.Quality;
public sealed record QualityInspectionListRequest(QCInspectionStatus? Status,Guid? OrderId,Guid? ProductionJobId,Guid? InspectorUserId,int? Page,int? PageSize){public QualityInspectionQuery Query()=>new(Status,OrderId,ProductionJobId,InspectorUserId,Page??1,PageSize??20);}

public static class QualityEndpoints
{
    public static IEndpointRouteBuilder MapQualityEndpoints(this IEndpointRouteBuilder e)
    {
        RouteGroupBuilder r = e.MapGroup("/api/v1/quality");
        r.MapGet("/templates", async (bool? includeInactive, int? limit, IQualityAdministration s, CancellationToken ct) => Map(await s.ListTemplatesAsync(includeInactive ?? false, Math.Clamp(limit ?? 100, 1, 200), ct))).RequireAuthorization(SecurityPermissions.QCRead);
        r.MapGet("/defect-types", async (bool? includeInactive, int? limit, IQualityAdministration s, CancellationToken ct) => Map(await s.ListDefectTypesAsync(includeInactive ?? false, Math.Clamp(limit ?? 100, 1, 200), ct))).RequireAuthorization(SecurityPermissions.QCRead);
        r.MapPost("/templates", async (CreateQCTemplateCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.CreateTemplateAsync(c, ct), true)).RequireAuthorization(SecurityPermissions.QCManage);
        r.MapPut("/templates/{id:guid}/active", async (Guid id, SetQualityActiveCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.SetTemplateActiveAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCManage);
        r.MapPost("/defect-types", async (CreateDefectTypeCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.CreateDefectTypeAsync(c, ct), true)).RequireAuthorization(SecurityPermissions.QCManage);
        r.MapPut("/defect-types/{id:guid}/active", async (Guid id, SetQualityActiveCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.SetDefectTypeActiveAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCManage);
        r.MapPost("/inspections", async (CreateInspectionCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.StartInspectionAsync(c, ct), true)).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapGet("/inspections/{id:guid}", async (Guid id, IQualityAdministration s, CancellationToken ct) => Map(await s.GetInspectionAsync(id, ct))).RequireAuthorization(SecurityPermissions.QCRead);
        r.MapPost("/inspections/{id:guid}/results", async (Guid id, RecordCriteriaResultCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.RecordResultAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapPost("/inspections/{id:guid}/defects", async (Guid id, RecordDefectCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.RecordDefectAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapPut("/inspections/{id:guid}/disposition", async (Guid id, SetInspectionDispositionCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.SetDispositionAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapPost("/inspections/{id:guid}/approve", async (Guid id, QualityVersionCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.ApproveAsync(id, c.Version, ct))).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapPost("/inspections/{id:guid}/suspend", async (Guid id, QualityReasonCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.SuspendAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCInspect);
        r.MapPost("/inspections/{id:guid}/review", async (Guid id, QualityReasonCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.ReviewAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCReview);
        r.MapPost("/inspections/{id:guid}/reject", async (Guid id, QualityReasonCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.RejectAsync(id, c, ct))).RequireAuthorization(SecurityPermissions.QCReview);
        r.MapGet("/inspections",async([AsParameters]QualityInspectionListRequest q,IQualityAdministration s,CancellationToken ct)=>Map(await s.ListInspectionsAsync(q.Query(),ct)));r.MapGet("/inspections/{id:guid}/results",async(Guid id,IQualityAdministration s,CancellationToken ct)=>Map(await s.ResultsAsync(id,ct)));r.MapGet("/inspections/{id:guid}/defects",async(Guid id,IQualityAdministration s,CancellationToken ct)=>Map(await s.DefectsAsync(id,ct)));r.MapPost("/inspections/{id:guid}/reproduction", async (Guid id, CreateReproductionCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.RequestReproductionAsync(id, c, ct), true)).RequireAuthorization(SecurityPermissions.ReproductionManage);
        r.MapPost("/reproductions/{id:guid}/jobs", async (Guid id, QualityVersionCommand c, IQualityAdministration s, CancellationToken ct) => Map(await s.CreateReproductionJobsAsync(id, c.Version, ct), true)).RequireAuthorization(SecurityPermissions.ReproductionManage);
        r.MapGet("/history/{entityId:guid}", async (Guid entityId, IQualityAdministration s, CancellationToken ct) => Map(await s.HistoryAsync(entityId, ct))).RequireAuthorization(SecurityPermissions.QCRead);
        r.MapGet("/analytics", async (DateTimeOffset from, DateTimeOffset to, int? top, IQualityAdministration s, CancellationToken ct) => Map(await s.AnalyticsAsync(from, to, top ?? 10, ct))).RequireAuthorization(SecurityPermissions.QCRead);
        return e;
    }

    private static IResult Map(Result r) => r.IsSuccess ? Results.NoContent() : Results.Problem(ApiProblemDetailsMapper.Create(r.Error, "quality", null));
    private static IResult Map<T>(Result<T> r, bool created = false) => r.IsSuccess ? Results.Json(r.Value, statusCode: created ? 201 : 200) : Results.Problem(ApiProblemDetailsMapper.Create(r.Error, "quality", null));
}
