using BARANG.Core.Application.BusinessAnalysis;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Api.Errors;

namespace BARANG.Core.Api.BusinessAnalysis;

public static class BusinessProfileEndpoints
{
    public static IEndpointRouteBuilder MapBusinessProfileEndpoints(this IEndpointRouteBuilder e)
    {
        e.MapGet("/api/v1/customers/{id:guid}/business-profile",async(Guid id,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.GetCustomerAsync(id,ct))).RequireAuthorization(SecurityPermissions.CustomerRead);
        e.MapPut("/api/v1/customers/{id:guid}/business-profile",async(Guid id,CustomerBusinessProfileCommand c,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.SetCustomerAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.CustomerManage);
        e.MapGet("/api/v1/products/{id:guid}/technical-profile",async(Guid id,Guid?variantId,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.GetProductAsync(id,variantId,ct))).RequireAuthorization(SecurityPermissions.ProductRead);
        e.MapPut("/api/v1/products/{id:guid}/technical-profile",async(Guid id,ProductTechnicalProfileCommand c,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.SetProductAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.ProductManage);
        e.MapGet("/api/v1/orders/{id:guid}/business-profile",async(Guid id,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.GetOrderAsync(id,ct))).RequireAuthorization(SecurityPermissions.OrderRead);
        e.MapPut("/api/v1/orders/{id:guid}/business-profile",async(Guid id,OrderBusinessProfileCommand c,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.SetOrderAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.OrderManage);
        e.MapGet("/api/v1/order-print-operations/{id:guid}/technical-profile",async(Guid id,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.GetPrintAsync(id,ct))).RequireAuthorization(SecurityPermissions.OrderRead);
        e.MapPut("/api/v1/order-print-operations/{id:guid}/technical-profile",async(Guid id,PrintTechnicalProfileCommand c,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.SetPrintAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.OrderManage);
        return e;
    }
    private static IResult Map<T>(Result<T> result)=>result.IsSuccess?Results.Ok(result.Value):Results.Problem(ApiProblemDetailsMapper.Create(result.Error,"business-profile",null));
}
