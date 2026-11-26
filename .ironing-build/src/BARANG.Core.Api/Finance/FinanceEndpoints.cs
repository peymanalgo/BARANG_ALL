using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Finance;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Api.Finance;
public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder e)
    {
        RouteGroupBuilder root=e.MapGroup("/api/v1/orders/{orderId:guid}/finance");RouteGroupBuilder read=root.MapGroup("").RequireAuthorization(SecurityPermissions.FinanceRead);read.MapGet("/payments",async(Guid orderId,IFinanceAdministration s,CancellationToken ct)=>Map(await s.ListPaymentsAsync(orderId,ct)));read.MapGet("/balance",async(Guid orderId,IFinanceAdministration s,CancellationToken ct)=>Map(await s.GetBalanceAsync(orderId,ct)));read.MapGet("/status",async(Guid orderId,IFinanceAdministration s,CancellationToken ct)=>Map(await s.GetFinancialStateAsync(orderId,ct)));read.MapGet("/invoices",async(Guid orderId,IFinanceAdministration s,CancellationToken ct)=>Map(await s.ListInvoicesAsync(orderId,ct)));read.MapGet("/history",async(Guid orderId,IFinanceAdministration s,CancellationToken ct)=>Map(await s.HistoryAsync(orderId,ct)));
        root.MapPost("/payments",async(Guid orderId,RegisterPaymentCommand c,IFinanceAdministration s,CancellationToken ct)=>Map(await s.RegisterPaymentAsync(orderId,c,ct),true)).RequireAuthorization(SecurityPermissions.PaymentManage);root.MapPost("/payments/{paymentId:guid}/reversals",async(Guid orderId,Guid paymentId,ReversePaymentCommand c,IFinanceAdministration s,CancellationToken ct)=>Map(await s.ReversePaymentAsync(orderId,paymentId,c,ct),true)).RequireAuthorization(SecurityPermissions.PaymentReverse);root.MapPost("/decision",async(Guid orderId,FinanceDecisionCommand c,IFinanceAdministration s,CancellationToken ct)=>Map(await s.DecideAsync(orderId,c,ct))).RequireAuthorization(SecurityPermissions.FinanceApprove);root.MapPost("/invoices/{type}",async(Guid orderId,InvoiceType type,IFinanceAdministration s,CancellationToken ct)=>Map(await s.GenerateInvoiceAsync(orderId,type,ct),true)).RequireAuthorization(SecurityPermissions.InvoiceManage);return e;
    }
    private static FinanceResult<T> Map<T>(Result<T> result,bool created=false)=>new(result,created);private sealed class FinanceResult<T>(Result<T> result,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(result.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(result.Value,c.RequestAborted);}return Results.Problem(ApiProblemDetailsMapper.Create(result.Error,c.TraceIdentifier,c.Request.Path)).ExecuteAsync(c);}}
}
