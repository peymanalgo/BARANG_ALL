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
    Func<string, CancellationToken, Task<bool>> HasAnyPermission,
    Guid? CustomerId = null)
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

/// <summary>ابزار دستیار مشتری B2C: فقط داده‌های خود مشتری (customerId از سرور)، بدون هیچ ابزار کارکنان.</summary>
public interface IAiCustomerTool : IAiTool { }

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

// ─── دستیار مشتری فروشگاه B2C ───
public sealed record AiCustomerAssistantRequest(string Message, string Culture, bool ResetConversation = false);

public sealed record AiCustomerAssistantReply(
    string Reply,
    bool NotConfigured,
    string? Error,
    IReadOnlyList<string> ToolsUsed,
    int QuotaUsed,
    int QuotaLimit,
    IReadOnlyList<AiAssistantTurnMessage> History)
{
    public static AiCustomerAssistantReply NotConfiguredReply(int quotaUsed, int quotaLimit, IReadOnlyList<AiAssistantTurnMessage> history) =>
        new("", true, null, [], quotaUsed, quotaLimit, history);
}

public interface IAiCustomerAssistantService
{
    Task<Result<AiCustomerAssistantReply>> ChatAsync(AiCustomerAssistantRequest request, CancellationToken ct);
    Task<Result<AiCustomerAssistantReply>> GetHistoryAsync(CancellationToken ct);
    Task<Result<AiCustomerAssistantReply>> ClearConversationAsync(CancellationToken ct);
}

// ─── طراحی تصویر با هوش مصنوعی (ویزارد B2C) ───
public sealed record AiDesignRequest(string Prompt, string Culture);

public sealed record AiDesignReply(
    bool Ok,
    bool QuotaExceeded,
    string? ImageUrl,
    int QuotaUsed,
    int QuotaLimit,
    int Remaining,
    string? Message,
    string? Error)
{
    public static AiDesignReply Exceeded(int quotaUsed, int quotaLimit, string message) =>
        new(false, true, null, quotaUsed, quotaLimit, 0, message, "ai.design_quota_exceeded");
    public static AiDesignReply Failed(string error, string message) =>
        new(false, false, null, 0, AiDesignServiceDefaults.QuotaLimit, 0, message, error);
    public static AiDesignReply Made(string imageUrl, int quotaUsed, int quotaLimit, string message) =>
        new(true, false, imageUrl, quotaUsed, quotaLimit, Math.Max(0, quotaLimit - quotaUsed), message, null);
}

/// <summary>مقادیر مشترک سرویس طراحی AI — برای پاسخ‌های خطای ثابت در قرارداد.</summary>
public static class AiDesignServiceDefaults
{
    public const int QuotaLimit = 5;
}

public interface IAiDesignService
{
    Task<Result<AiDesignReply>> DesignAsync(AiDesignRequest request, CancellationToken ct);
    Task<Result<AiDesignReply>> QuotaAsync(CancellationToken ct);
}
