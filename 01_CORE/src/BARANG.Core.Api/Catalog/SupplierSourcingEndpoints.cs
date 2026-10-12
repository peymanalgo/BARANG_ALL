using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Catalog;

public static class SupplierSourcingEndpoints
{
    public static IEndpointRouteBuilder MapSupplierSourcingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var root=endpoints.MapGroup("/api/v1/suppliers/{supplier:guid}/sourcing");
        var read=root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierRead);
        var write=root.MapGroup("").RequireAuthorization(SecurityPermissions.SupplierManage);
        read.MapGet("/products",async(Guid supplier,[AsParameters]SourcingQuery query,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ProductsAsync(supplier,query,ct)));
        read.MapGet("/products/{id:guid}",async(Guid supplier,Guid id,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ProductAsync(supplier,id,ct)));
        write.MapPost("/products",async(Guid supplier,SourcingCommand<SupplierSourcingData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveProductAsync(supplier,null,command,ct),true));
        write.MapPut("/products/{id:guid}",async(Guid supplier,Guid id,SourcingCommand<SupplierSourcingData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveProductAsync(supplier,id,command,ct)));
        write.MapDelete("/products/{id:guid}",async(Guid supplier,Guid id,long version,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.DeleteProductAsync(supplier,id,version,ct)));
        read.MapGet("/prices",async(Guid supplier,[AsParameters]SourcingQuery query,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.PricesAsync(supplier,query,ct)));
        read.MapGet("/prices/{id:guid}",async(Guid supplier,Guid id,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.PriceAsync(supplier,id,ct)));
        write.MapPost("/prices",async(Guid supplier,SourcingCommand<SupplierPriceData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SavePriceAsync(supplier,null,command,ct),true));
        write.MapPut("/prices/{id:guid}",async(Guid supplier,Guid id,SourcingCommand<SupplierPriceData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SavePriceAsync(supplier,id,command,ct)));
        write.MapDelete("/prices/{id:guid}",async(Guid supplier,Guid id,long version,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.DeletePriceAsync(supplier,id,version,ct)));
        read.MapGet("/price-lists",async(Guid supplier,[AsParameters]SourcingQuery query,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ListsAsync(supplier,query,ct)));
        read.MapGet("/price-lists/{id:guid}",async(Guid supplier,Guid id,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ListAsync(supplier,id,ct)));
        write.MapPost("/price-lists",async(Guid supplier,SourcingCommand<SupplierPriceListData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveListAsync(supplier,null,command,ct),true));
        write.MapPut("/price-lists/{id:guid}",async(Guid supplier,Guid id,SourcingCommand<SupplierPriceListData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveListAsync(supplier,id,command,ct)));
        write.MapDelete("/price-lists/{id:guid}",async(Guid supplier,Guid id,long version,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.DeleteListAsync(supplier,id,version,ct)));
        write.MapPost("/products/{id:guid}/activation",async(Guid supplier,Guid id,ActivationRequest request,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ProductStateAsync(supplier,id,request.Version,request.IsActive,null,ct)));
        write.MapPost("/products/{id:guid}/preferred",async(Guid supplier,Guid id,SourcingPreferenceRequest request,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ProductStateAsync(supplier,id,request.Version,null,request.IsPreferred,ct)));
        write.MapPost("/prices/{id:guid}/activation",async(Guid supplier,Guid id,ActivationRequest request,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.PriceStateAsync(supplier,id,request.Version,request.IsActive,null,ct)));
        write.MapPost("/prices/{id:guid}/end-date",async(Guid supplier,Guid id,SourcingEndDateRequest request,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.PriceStateAsync(supplier,id,request.Version,null,request.EffectiveTo,ct)));
        write.MapPost("/price-lists/{id:guid}/activation",async(Guid supplier,Guid id,ActivationRequest request,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ListStateAsync(supplier,id,request.Version,request.IsActive,ct)));
        read.MapGet("/price-lists/{list:guid}/lines",async(Guid supplier,Guid list,[AsParameters]SourcingQuery query,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.LinesAsync(supplier,list,query,ct)));
        read.MapGet("/price-lists/{list:guid}/lines/{id:guid}",async(Guid supplier,Guid list,Guid id,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.LineAsync(supplier,list,id,ct)));
        write.MapPost("/price-lists/{list:guid}/lines",async(Guid supplier,Guid list,SourcingCommand<SupplierPriceListLineData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveLineAsync(supplier,list,null,command,ct),true));
        write.MapPut("/price-lists/{list:guid}/lines/{id:guid}",async(Guid supplier,Guid list,Guid id,SourcingCommand<SupplierPriceListLineData> command,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SaveLineAsync(supplier,list,id,command,ct)));
        write.MapDelete("/price-lists/{list:guid}/lines/{id:guid}",async(Guid supplier,Guid list,Guid id,long version,long parentVersion,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.DeleteLineAsync(supplier,list,id,version,parentVersion,ct)));
        endpoints.MapGet("/api/v1/purchasing/sourcing-options",async([AsParameters]PurchasingSourcingQuery query,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.SourcesAsync(query,ct))).RequireAuthorization(SecurityPermissions.PurchasingRead);
        endpoints.MapGet("/api/v1/products/{product:guid}/supplier-sourcing",async(Guid product,Guid? variant,ISupplierSourcingAdministration service,CancellationToken ct)=>Map(await service.ProductSourcesAsync(product,variant,ct))).RequireAuthorization(SecurityPermissions.ProductRead);
        return endpoints;
    }
    private static SourcingResult<T> Map<T>(Result<T> result,bool created=false)=>new(result,created);
    private sealed class SourcingResult<T>(Result<T> result,bool created):IResult
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
public sealed record SourcingPreferenceRequest(bool IsPreferred,long Version);
public sealed record SourcingEndDateRequest(DateOnly EffectiveTo,long Version);
