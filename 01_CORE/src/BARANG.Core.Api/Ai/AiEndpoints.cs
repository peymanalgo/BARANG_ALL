using BARANG.Core.Application.Ai;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BARANG.Core.Api.Ai;

/// <summary>Endpointهای دستیار هوش مصنوعی: تنظیمات مدل زبانی (مجوز ai.settings.*) و گفتگو (هر کاربر احراز هویت‌شده، rate-limited).</summary>
public static class AiEndpoints
{
    private static readonly System.Text.Json.JsonSerializerOptions WireOptions = new(System.Text.Json.JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, Exception?> LogSettingsBody = LoggerMessage.Define<string>(LogLevel.Information, new(3201, "AiSettingsPutBody"), "PUT ai/settings raw body: {Body}");

    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder settings = endpoints.MapGroup("/api/v1/ai/settings").RequireAuthorization();
        settings.MapGet("/", async (IAiSettingsAdministration admin, CancellationToken ct) => Map(await admin.GetSettingsAsync(ct))).RequireAuthorization(SecurityPermissions.AiSettingsRead);
        settings.MapPut("/", async (HttpContext http, IAiSettingsAdministration admin, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            string raw = await new StreamReader(http.Request.Body).ReadToEndAsync(ct);
            LogSettingsBody(loggerFactory.CreateLogger("AiEndpoints"), raw, null);
            SaveAiModelSettingsRequest? request = System.Text.Json.JsonSerializer.Deserialize<SaveAiModelSettingsRequest>(raw, WireOptions);
            if (request is null) return Results.BadRequest(new { error = "ai.bind_null" });
            return Map(await admin.SaveSettingsAsync(new(request.ProviderName, request.BaseUrl, request.ApiKey, request.ModelName, request.Temperature, request.MaxOutputTokens, request.TimeoutSeconds, request.SystemPromptOverride, request.ToolCallingEnabled, request.IsEnabled, request.Version), ct));
        }).RequireAuthorization(SecurityPermissions.AiSettingsManage);
        settings.MapPost("/test", async (IAiSettingsAdministration admin, CancellationToken ct) => Map(await admin.TestConnectionAsync(ct))).RequireAuthorization(SecurityPermissions.AiSettingsManage);
        settings.MapGet("/audit", async (int? take, IAiSettingsAdministration admin, CancellationToken ct) => Map(await admin.ListAuditAsync(take ?? 50, ct))).RequireAuthorization(SecurityPermissions.AiSettingsRead);

        RouteGroupBuilder assistant = endpoints.MapGroup("/api/v1/ai").RequireAuthorization();
        assistant.MapPost("/assistant", async (AiAssistantHttpRequest request, IAiAssistantService service, CancellationToken ct) =>
        {
            Result<AiAssistantReply> result = await service.ChatAsync(new([.. (request.Messages ?? []).Select(m => new AiAssistantTurnMessage(m.Role, m.Content))], request.Culture ?? "fa", request.ResetConversation), ct);
            return Map(result);
        }).RequireRateLimiting("ai-chat");
        assistant.MapGet("/conversation", async (IAiAssistantService service, CancellationToken ct) => Map(await service.GetHistoryAsync(ct))).RequireRateLimiting("ai-chat");
        assistant.MapPost("/conversation/clear", async (IAiAssistantService service, CancellationToken ct) => MapVoid(await service.ClearConversationAsync(ct))).RequireRateLimiting("ai-chat");
        return endpoints;
    }

    private static AiResult<T> Map<T>(Result<T> result) => new(result);

    private static AiVoidResult MapVoid(Result result) => new(result);

    private sealed class AiVoidResult(Result result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (result.IsSuccess)
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return httpContext.Response.WriteAsJsonAsync(new { error = result.Error.Code, description = result.Error.Description }, httpContext.RequestAborted);
        }
    }

    private sealed class AiResult<T>(Result<T> result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
            object payload = result.IsSuccess
                ? result.Value!
                : new { error = result.Error.Code, description = result.Error.Description };
            return httpContext.Response.WriteAsJsonAsync(payload, httpContext.RequestAborted);
        }
    }
}

public sealed record SaveAiModelSettingsRequest(string ProviderName, string BaseUrl, string? ApiKey, string ModelName, decimal Temperature, int MaxOutputTokens, int TimeoutSeconds, string? SystemPromptOverride, bool ToolCallingEnabled, bool IsEnabled, long Version);
public sealed record AiAssistantHttpMessage(string Role, string Content);
public sealed record AiAssistantHttpRequest(IReadOnlyList<AiAssistantHttpMessage>? Messages, string? Culture, bool ResetConversation);
