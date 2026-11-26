using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Pricing;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Pricing;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Api.Pricing;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder e)
    {
        RouteGroupBuilder tariffs=e.MapGroup("/api/v1/pricing/tariffs");tariffs.MapGet("/",async(TariffType? type,bool? includeInactive,IPricingAdministration s,CancellationToken ct)=>Map(await s.ListTariffsAsync(type,includeInactive??false,ct))).RequireAuthorization(SecurityPermissions.PricingRead);tariffs.MapPost("/",async(TariffCommand c,IPricingAdministration s,CancellationToken ct)=>Map(await s.CreateTariffAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.PricingManage);tariffs.MapPut("/{id:guid}",async(Guid id,TariffUpdateRequest r,IPricingAdministration s,CancellationToken ct)=>Map(await s.UpdateTariffAsync(id,r.Command,r.IsActive,ct))).RequireAuthorization(SecurityPermissions.PricingManage);
        RouteGroupBuilder rules=e.MapGroup("/api/v1/pricing").RequireAuthorization(SecurityPermissions.PricingManage);rules.MapPost("/discount-rules",async(DiscountRuleCommand c,IPricingAdministration s,CancellationToken ct)=>Map(await s.CreateDiscountRuleAsync(c,ct),true));rules.MapPost("/tax-rules",async(TaxRuleCommand c,IPricingAdministration s,CancellationToken ct)=>Map(await s.CreateTaxRuleAsync(c,ct),true));
        RouteGroupBuilder orders=e.MapGroup("/api/v1/orders/{orderId:guid}/pricing");orders.MapGet("/",async(Guid orderId,IPricingAdministration s,CancellationToken ct)=>Map(await s.GetOrderPricingAsync(orderId,ct))).RequireAuthorization(SecurityPermissions.OrderRead,SecurityPermissions.PricingRead);orders.MapPost("/recalculate",async(Guid orderId,PricingVersionRequest r,IPricingAdministration s,CancellationToken ct)=>Map(await s.RecalculateAsync(orderId,r.OrderVersion,null,ct))).RequireAuthorization(SecurityPermissions.OrderManage,SecurityPermissions.PricingRead);orders.MapPost("/recalculate-with-manual-discount",async(Guid orderId,ManualPricingRequest r,IPricingAdministration s,CancellationToken ct)=>Map(await s.RecalculateAsync(orderId,r.OrderVersion,r.Discount,ct))).RequireAuthorization(SecurityPermissions.OrderManage,SecurityPermissions.PricingRead,SecurityPermissions.PricingDiscountApply);return e;
    }
    private static PricingResult<T> Map<T>(Result<T> result,bool created=false)=>new(result,created);private sealed class PricingResult<T>(Result<T> result,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(result.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(result.Value,c.RequestAborted);}return Results.Problem(ApiProblemDetailsMapper.Create(result.Error,c.TraceIdentifier,c.Request.Path)).ExecuteAsync(c);}}
}
public sealed record TariffUpdateRequest(TariffCommand Command,bool IsActive);
public sealed record PricingVersionRequest(long OrderVersion);
public sealed record ManualPricingRequest(long OrderVersion,ManualDiscountCommand Discount);
