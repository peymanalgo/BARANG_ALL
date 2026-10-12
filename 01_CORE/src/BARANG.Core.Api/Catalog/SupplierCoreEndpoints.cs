using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Application.Abstractions.Correlation;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Catalog;

public static class SupplierCoreEndpoints
{
    public static IEndpointRouteBuilder MapSupplierCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var root=endpoints.MapGroup("/api/v1/suppliers");
        var read=root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierRead);
        var write=root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierManage);
        read.MapGet("/core",async([AsParameters]SupplierCoreQuery query,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SearchAsync(query,ct)));
        read.MapGet("/core/{id:guid}",async(Guid id,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.GetAsync(id,ct)));
        write.MapPost("/core",async(SupplierCoreCommand request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveAsync(null,request,ct),true));
        write.MapPut("/core/{id:guid}",async(Guid id,SupplierCoreCommand request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveAsync(id,request,ct)));
        write.MapPost("/{id:guid}/status",async(Guid id,SupplierStatusRequest request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.StatusAsync(id,request.Status,request.HoldReasonId,request.Version,ct)));
        write.MapDelete("/{id:guid}",async(Guid id,long version,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.DeleteAsync(id,version,ct)));
        read.MapGet("/{id:guid}/default-address",async(Guid id,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.ResolveDefaultAddressAsync(id,ct)));
        MapChild<SupplierAddress>(root,"addresses","default");
        MapChild<SupplierContact>(root,"contacts","primary");
        MapChild<SupplierBankAccount>(root,"bank-accounts","default");
        write.MapPost("/{owner:guid}/addresses",async(Guid owner,SupplierChildCommand<SupplierAddressData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveAddressAsync(owner,null,request,ct),true));
        write.MapPut("/{owner:guid}/addresses/{id:guid}",async(Guid owner,Guid id,SupplierChildCommand<SupplierAddressData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveAddressAsync(owner,id,request,ct)));
        write.MapPost("/{owner:guid}/contacts",async(Guid owner,SupplierChildCommand<SupplierContactData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveContactAsync(owner,null,request,ct),true));
        write.MapPut("/{owner:guid}/contacts/{id:guid}",async(Guid owner,Guid id,SupplierChildCommand<SupplierContactData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveContactAsync(owner,id,request,ct)));
        write.MapPost("/{owner:guid}/bank-accounts",async(Guid owner,SupplierChildCommand<SupplierBankData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveBankAsync(owner,null,request,ct),true));
        write.MapPut("/{owner:guid}/bank-accounts/{id:guid}",async(Guid owner,Guid id,SupplierChildCommand<SupplierBankData> request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.SaveBankAsync(owner,id,request,ct)));
        return endpoints;
    }
    private static void MapChild<T>(RouteGroupBuilder root,string kind,string defaultAction) where T:SupplierOwnedEntity
    {
        var read=root.MapGroup("/{owner:guid}/"+kind).RequireAuthorization(SecurityPermissions.SupplierRead);
        var write=root.MapGroup("/{owner:guid}/"+kind).RequireAuthorization(SecurityPermissions.SupplierManage);
        read.MapGet("",async(Guid owner,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.ChildrenAsync<T>(owner,ct)));
        write.MapPost("/{id:guid}/activation",async(Guid owner,Guid id,ActivationRequest request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.ChangeChildAsync<T>(owner,id,request.Version,request.IsActive,ct)));
        write.MapPost("/{id:guid}/"+defaultAction,async(Guid owner,Guid id,SupplierVersionRequest request,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.ChangeChildAsync<T>(owner,id,request.Version,null,ct)));
        write.MapDelete("/{id:guid}",async(Guid owner,Guid id,long version,ISupplierCoreAdministration service,CancellationToken ct)=>Map(await service.DeleteChildAsync<T>(owner,id,version,ct)));
    }
    private static SupplierResult<T> Map<T>(Result<T> result,bool created=false)=>new SupplierResult<T>(result,created);
    private sealed class SupplierResult<T>(Result<T> result,bool created):IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            if(result.IsSuccess){context.Response.StatusCode=created?201:200;return context.Response.WriteAsJsonAsync(result.Value,context.RequestAborted);}
            var correlation=context.RequestServices.GetRequiredService<ICorrelationContext>();
            var problem=ApiProblemDetailsMapper.Create(result.Error,correlation.CorrelationId??context.TraceIdentifier,context.Request.Path);
            context.Response.StatusCode=problem.Status!.Value;return context.Response.WriteAsJsonAsync(problem,context.RequestAborted);
        }
    }
}
public sealed record SupplierStatusRequest(string Status,long Version,Guid? HoldReasonId=null);
public sealed record SupplierVersionRequest(long Version);
