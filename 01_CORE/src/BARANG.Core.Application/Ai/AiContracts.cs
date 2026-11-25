using BARANG.Core.Domain.Common;
using System.Text.Json.Nodes;

namespace BARANG.Core.Application.Ai;

/// <summary>یک ابزار قابل فراخوانی توسط مدل زبانی. هر پیاده‌سازی باید مجوزهای فراخوان را کنترل کند.</summary>
public interface IAiTool
{
    string Name { get; }
    string Description { get; }
    /// <summary>JSON Schema آبجکت پارامترها (OpenAI function-calling format).</summary>
    JsonObject ParameterSchema { get; }
    /// <summary>هر یک از این مجوزها کافی است (ANY-of). خالی = فقط کاربر احراز هویت‌شده.</summary>
 IReadOnlyList<string> AnyOfPermissions { get; }
    /// <summary>پارامترهای required برای schema.</summary>
 IReadOnlyList<string> RequiredParameters { get; }
    Task<JsonObject> ExecuteAsync(AiToolContext context, JsonObject? arguments, CancellationToken ct);
}

/// <summary>زمینه اجرای ابزار: کاربر جاری، شرکت، فرهنگ و دسترسی مجوزها.</summary>
public sealed record AiToolContext(
    Guid CompanyId,
    Guid UserId,
    string Username,
    string Culture,
    Func<string, CancellationToken, Task<bool>> HasAnyPermission)
{
    /// <summary>کنترل مجوز با هر یک از کدهای داده‌شده کافی است.</summary>
    public Task<bool> CanUseAsync(string permissionCode, CancellationToken ct) => HasAnyPermission(permissionCode, ct);
}

/// <summary>رجیستری توسعه‌پذیر ابزارها — ابزار جدید کافی است IAiTool پیاده کند و در DI ثبت شود.</summary>
public interface IAiToolRegistry
{
    IReadOnlyList<IAiTool> Tools { get; }
    IAiTool? Find(string name);
}

public sealed record AiAssistantTurnMessage(string Role, string Content, string? Direction = null, DateTimeOffset? CreatedAtUtc = null, string? ToolsUsed = null);

public sealed record AiAssistantRequest(IReadOnlyList<AiAssistantTurnMessage> Messages, string Culture, bool ResetConversation = false);

public sealed record AiAssistantReply(
    string Reply,
    bool NotConfigured,
    string? Error,
    IReadOnlyList<string> ToolsUsed,
    int? PromptTokens,
    int? CompletionTokens,
    IReadOnlyList<AiAssistantTurnMessage> History)
{
    public static AiAssistantReply NotConfiguredReply(IReadOnlyList<AiAssistantTurnMessage> history) => new("", true, null, [], null, null, history);
}

public sealed record AiModelSettingsView(
    bool Exists,
    string ProviderName,
    string BaseUrl,
    string? ApiKeyHint,
    string ModelName,
    decimal Temperature,
    int MaxOutputTokens,
    int TimeoutSeconds,
    string? SystemPromptOverride,
    bool ToolCallingEnabled,
    bool IsEnabled,
    long Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveAiModelSettingsCommand(
    string ProviderName,
    string BaseUrl,
    string? ApiKey,
    string ModelName,
    decimal Temperature,
    int MaxOutputTokens,
    int TimeoutSeconds,
    string? SystemPromptOverride,
    bool ToolCallingEnabled,
    bool IsEnabled,
    long Version);

public sealed record AiTestConnectionResult(bool Ok, int LatencyMs, string? Error, string? ModelReply);

public sealed record AiAuditEntryView(Guid Id, string ActorUsername, string Action, string Detail, DateTimeOffset OccurredAtUtc);

public sealed record AiToolDescriptor(string Name, string Description, IReadOnlyList<string> AnyOfPermissions);

public interface IAiSettingsAdministration
{
    Task<Result<AiModelSettingsView>> GetSettingsAsync(CancellationToken ct);
    Task<Result<AiModelSettingsView>> SaveSettingsAsync(SaveAiModelSettingsCommand command, CancellationToken ct);
    Task<Result<AiTestConnectionResult>> TestConnectionAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<AiAuditEntryView>>> ListAuditAsync(int take, CancellationToken ct);
}

public interface IAiAssistantService
{
    Task<Result<AiAssistantReply>> ChatAsync(AiAssistantRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<AiAssistantTurnMessage>>> GetHistoryAsync(CancellationToken ct);
    Task<Result> ClearConversationAsync(CancellationToken ct);
}
