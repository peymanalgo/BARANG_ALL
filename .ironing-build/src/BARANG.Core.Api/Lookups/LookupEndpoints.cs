using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Lookups;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Lookups;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Lookups;

public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root=endpoints.MapGroup("/api/v1/lookups");
        RouteGroupBuilder read=root.MapGroup("").RequireAuthorization(SecurityPermissions.LookupRead);
        read.MapGet("/domains",async(string? culture,ILookupAdministration s,CancellationToken ct)=>Map(await s.GetDomainsAsync(culture??"fa",ct)));
        read.MapGet("/types",async(bool? activeOnly,ILookupAdministration s,CancellationToken ct)=>Map(await s.ListTypesAsync(activeOnly??false,ct)));
        read.MapGet("/types/{id:guid}",async(Guid id,ILookupAdministration s,CancellationToken ct)=>Map(await s.GetTypeAsync(id,ct)));
        read.MapGet("/types/by-code/{code}",async(string code,ILookupAdministration s,CancellationToken ct)=>Map(await s.GetTypeByCodeAsync(code,ct)));
        read.MapGet("/types/{typeCode}/values",async(string typeCode,bool? activeOnly,ILookupAdministration s,CancellationToken ct)=>Map(await s.ListValuesAsync(typeCode,activeOnly??false,ct)));
        read.MapGet("/values/{id:guid}",async(Guid id,ILookupAdministration s,CancellationToken ct)=>Map(await s.GetValueAsync(id,ct)));
        read.MapGet("/types/{typeCode}/values/by-code/{valueCode}",async(string typeCode,string valueCode,ILookupAdministration s,CancellationToken ct)=>Map(await s.GetValueByCodeAsync(typeCode,valueCode,ct)));
        read.MapGet("/{typeCode}/options",async(string typeCode,string? culture,bool? activeOnly,ILookupOptionReader s,CancellationToken ct)=>Map(await s.GetOptionsAsync(typeCode,culture??"fa",activeOnly??true,ct)));
        RouteGroupBuilder manage=root.MapGroup("").RequireAuthorization(SecurityPermissions.LookupManage);
        manage.MapPost("/types",async(CreateLookupTypeRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.CreateTypeAsync(new(r.Code,r.DisplayName,r.AllowCustomValues,r.Scope),ct),true));
        manage.MapPut("/types/{id:guid}",async(Guid id,UpdateLookupTypeRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.UpdateTypeAsync(id,r.DisplayName,r.AllowCustomValues,r.Version,ct)));
        manage.MapPost("/types/{id:guid}/activation",async(Guid id,ActivationRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.SetTypeActiveAsync(id,r.IsActive,r.Version,ct)));
        manage.MapPost("/types/{typeCode}/values",async(string typeCode,CreateLookupValueRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.CreateValueAsync(typeCode,new(r.Code,r.DisplayOrder,r.Translations,r.Descriptions,r.ParentId),ct),true));
        manage.MapPut("/values/{id:guid}",async(Guid id,UpdateLookupValueRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.UpdateValueAsync(id,r.DisplayOrder,r.Version,ct)));
        manage.MapPost("/values/{id:guid}/activation",async(Guid id,ActivationRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.SetValueActiveAsync(id,r.IsActive,r.Version,ct)));
        manage.MapPut("/values/{id:guid}/translations/{culture}",async(Guid id,string culture,UpdateTranslationRequest r,ILookupAdministration s,CancellationToken ct)=>Map(await s.SetTranslationAsync(id,culture,r.DisplayName,r.Description,r.Version,ct)));
        manage.MapPut("/values/{id:guid}/editor",async(Guid id,EditLookupValueCommand r,ILookupAdministration s,CancellationToken ct)=>Map(await s.EditValueAsync(id,r,ct)));
        manage.MapDelete("/values/{id:guid}",async(Guid id,long version,ILookupAdministration s,CancellationToken ct)=>Map(await s.DeleteValueAsync(id,version,ct)));
        foreach (var adapter in new[] { (Code: "artwork.tag", Read: SecurityPermissions.ArtworkRead, Manage: SecurityPermissions.ArtworkManage), (Code: "defect.type", Read: SecurityPermissions.QCRead, Manage: SecurityPermissions.QCManage) })
        {
            string type = adapter.Code;
            RouteGroupBuilder entity = endpoints.MapGroup("/api/v1/reference-entities/" + type + "/values");
            entity.MapGet("/", async (IReferenceEntityAdministration service, CancellationToken ct) => Map(await service.ListAsync(type, ct))).RequireAuthorization(SecurityPermissions.LookupRead, adapter.Read);
            RouteGroupBuilder write = entity.MapGroup("").RequireAuthorization(SecurityPermissions.LookupManage, adapter.Manage);
            write.MapPost("/", async (ReferenceEntityCommand command, IReferenceEntityAdministration service, CancellationToken ct) => Map(await service.SaveAsync(type, null, command, ct), true));
            write.MapPut("/{id:guid}/editor", async (Guid id, ReferenceEntityCommand command, IReferenceEntityAdministration service, CancellationToken ct) => Map(await service.SaveAsync(type, id, command, ct)));
            write.MapPost("/{id:guid}/activation", async (Guid id, ActivationRequest command, IReferenceEntityAdministration service, CancellationToken ct) => Map(await service.ActivateAsync(type, id, command.IsActive, command.Version, ct)));
            write.MapDelete("/{id:guid}", async (Guid id, long version, IReferenceEntityAdministration service, CancellationToken ct) => Map(await service.DeleteAsync(type, id, version, ct)));
        }
        return endpoints;
    }
    private static LookupResult<T> Map<T>(Result<T> result,bool created=false)=>new(result,created);
    private sealed class LookupResult<T>(Result<T> result,bool created):IResult { public Task ExecuteAsync(HttpContext c){if(result.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(result.Value,c.RequestAborted);} ICorrelationContext correlation=c.RequestServices.GetRequiredService<ICorrelationContext>();ProblemDetails p=ApiProblemDetailsMapper.Create(result.Error,correlation.CorrelationId??c.TraceIdentifier,c.Request.Path);c.Response.StatusCode=p.Status!.Value;return c.Response.WriteAsJsonAsync(p,c.RequestAborted);} }
}
public sealed record CreateLookupTypeRequest(string Code,string DisplayName,bool AllowCustomValues=false,LookupScope Scope=LookupScope.Company);
public sealed record UpdateLookupTypeRequest(string DisplayName,bool AllowCustomValues,long Version);
public sealed record CreateLookupValueRequest(string Code,int DisplayOrder,IReadOnlyDictionary<string,string> Translations,IReadOnlyDictionary<string,string>? Descriptions=null,Guid? ParentId=null);
public sealed record UpdateLookupValueRequest(int DisplayOrder,long Version);
public sealed record UpdateTranslationRequest(string DisplayName,string? Description,long Version);
public sealed record ActivationRequest(bool IsActive,long Version);
