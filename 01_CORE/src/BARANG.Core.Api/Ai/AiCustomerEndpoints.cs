using BARANG.Core.Application.Ai;
using BARANG.Core.Domain.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace BARANG.Core.Api.Ai;

/// <summary>Endpointهای دستیار مشتری فروشگاه B2C (زیر /api/v1/client): احراز هویت کلاینت لازم است،
/// مشتری از توکن حل می‌شود (نه از ورودی)، سهمیه ۱۰ پرسش/۲۴ساعت سرور-side کنترل می‌شود و
/// فقط ابزارهای «داده‌های خود مشتری» در دسترس مدل قرار می‌گیرند.</summary>
public static class AiCustomerEndpoints
{
    private static readonly System.Text.Json.JsonSerializerOptions WireOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapAiCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder assistant = endpoints.MapGroup("/api/v1/client/ai").RequireAuthorization();
        assistant.MapPost("/assistant", async (AiCustomerAssistantHttpRequest request, IAiCustomerAssistantService service, CancellationToken ct) =>
        {
            Result<AiCustomerAssistantReply> result = await service.ChatAsync(new(request.Message ?? "", request.Culture ?? "fa", request.ResetConversation), ct);
            return Map(result);
        }).RequireRateLimiting("ai-customer-chat");
        assistant.MapGet("/conversation", async (IAiCustomerAssistantService service, CancellationToken ct) => Map(await service.GetHistoryAsync(ct))).RequireRateLimiting("ai-customer-chat");
        assistant.MapPost("/conversation/clear", async (IAiCustomerAssistantService service, CancellationToken ct) => Map(await service.ClearConversationAsync(ct))).RequireRateLimiting("ai-customer-chat");
        return endpoints;
    }

    private static AiResult<T> Map<T>(Result<T> result) => new(result);

    public sealed record AiCustomerAssistantHttpRequest(string? Message, string? Culture, bool ResetConversation);

    private sealed class AiResult<T>(Result<T> result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            // مشتری نیست (ai.access_denied) → 403 تا B2C کاربر را به ورود/پروفایل برساند
            httpContext.Response.StatusCode = result.IsSuccess
                ? StatusCodes.Status200OK
                : result.Error.Code == "ai.access_denied"
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status400BadRequest;
            object payload = result.IsSuccess
                ? result.Value!
                : new { error = result.Error.Code, description = result.Error.Description };
            return httpContext.Response.WriteAsJsonAsync(payload, httpContext.RequestAborted);
        }
    }
}
