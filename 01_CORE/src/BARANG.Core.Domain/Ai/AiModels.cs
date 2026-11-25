using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Ai;

public static class AiErrors
{
    public static Error SettingsInvalid => Error.Validation("ai.settings_invalid");
    public static Error SettingsNotFound => Error.NotFound("ai.settings_not_found");
    public static Error Concurrency => Error.Concurrency("ai.concurrency_conflict");
    public static Error ProviderUnreachable => Error.Infrastructure("ai.provider_unreachable");
    public static Error ProviderError => Error.Infrastructure("ai.provider_error");
    public static Error AccessDenied => Error.Authorization("ai.access_denied");
}

/// <summary>تنظیمات اتصال مدل زبانی — تک‌ردیفی برای هر شرکت (singleton per company).
/// کلید API به‌صورت رمزنگاری‌شده (AES-GCM) ذخیره می‌شود و هرگز خام به کلاینت برنمی‌گردد.</summary>
public sealed class AiModelSettings
{
    private AiModelSettings() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string ProviderName { get; private set; } = "";
    public string BaseUrl { get; private set; } = "";
    public string? ApiKeyCipher { get; private set; }
    public string? ApiKeyHint { get; private set; }
    public string ModelName { get; private set; } = "";
    public decimal Temperature { get; private set; }
    public int MaxOutputTokens { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public string? SystemPromptOverride { get; private set; }
    public bool ToolCallingEnabled { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<AiModelSettings> Create(Guid id, Guid companyId, string providerName, string baseUrl, string? apiKeyCipher, string? apiKeyHint, string modelName, decimal temperature, int maxOutputTokens, int timeoutSeconds, string? systemPromptOverride, bool toolCallingEnabled, bool isEnabled, DateTimeOffset now)
    {
        Result validated = Validate(providerName, baseUrl, modelName, temperature, maxOutputTokens, timeoutSeconds);
        if (validated.IsFailure) return Result.Failure<AiModelSettings>(validated.Error);
        return Result.Success(new AiModelSettings
        {
            Id = id,
            CompanyId = companyId,
            ProviderName = providerName.Trim(),
            BaseUrl = baseUrl.Trim().TrimEnd('/'),
            ApiKeyCipher = apiKeyCipher,
            ApiKeyHint = apiKeyHint,
            ModelName = modelName.Trim(),
            Temperature = temperature,
            MaxOutputTokens = maxOutputTokens,
            TimeoutSeconds = timeoutSeconds,
            SystemPromptOverride = NormalizeText(systemPromptOverride, 8000),
            ToolCallingEnabled = toolCallingEnabled,
            IsEnabled = isEnabled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1
        });
    }

    public Result Update(string providerName, string baseUrl, string? apiKeyCipher, string? apiKeyHint, string modelName, decimal temperature, int maxOutputTokens, int timeoutSeconds, string? systemPromptOverride, bool toolCallingEnabled, bool isEnabled, long expectedVersion, DateTimeOffset now)
    {
        if (expectedVersion != Version) return Result.Failure(AiErrors.Concurrency);
        Result validated = Validate(providerName, baseUrl, modelName, temperature, maxOutputTokens, timeoutSeconds);
        if (validated.IsFailure) return Result.Failure(validated.Error);
        ProviderName = providerName.Trim();
        BaseUrl = baseUrl.Trim().TrimEnd('/');
        if (apiKeyCipher is not null) { ApiKeyCipher = apiKeyCipher; ApiKeyHint = apiKeyHint; }
        ModelName = modelName.Trim();
        Temperature = temperature;
        MaxOutputTokens = maxOutputTokens;
        TimeoutSeconds = timeoutSeconds;
        SystemPromptOverride = NormalizeText(systemPromptOverride, 8000);
        ToolCallingEnabled = toolCallingEnabled;
        IsEnabled = isEnabled;
        UpdatedAtUtc = now;
        Version++;
        return Result.Success();
    }

    private static Result Validate(string providerName, string baseUrl, string modelName, decimal temperature, int maxOutputTokens, int timeoutSeconds)
    {
        if (providerName.Trim().Length is < 1 or > 100) return Result.Failure(AiErrors.SettingsInvalid);
        string url = baseUrl.Trim();
        if (url.Length is < 8 or > 500 || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "http" && uri.Scheme != "https")) return Result.Failure(AiErrors.SettingsInvalid);
        if (modelName.Trim().Length is < 1 or > 200) return Result.Failure(AiErrors.SettingsInvalid);
        if (temperature is < 0 or > 2) return Result.Failure(AiErrors.SettingsInvalid);
        if (maxOutputTokens is < 16 or > 200000) return Result.Failure(AiErrors.SettingsInvalid);
        if (timeoutSeconds is < 1 or > 600) return Result.Failure(AiErrors.SettingsInvalid);
        return Result.Success();
    }

    private static string? NormalizeText(string? value, int max)
    {
        string trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0) return null;
        if (trimmed.Length > max) trimmed = trimmed[..max];
        return trimmed;
    }
}

/// <summary>کانال گفتگو: پرسش‌وپاسخ پرسنل بک‌آفیس یا مشتری فروشگاه B2C —
/// جدا شدن صف مشتریان از پرسنل برای سهمیه و آمار.</summary>
public static class AiChannels
{
    public const string Staff = "staff";
    public const string Customer = "customer";
}

/// <summary>گفتگوی هر کاربر با دستیار — یک رشته فعال per user/company/channel.</summary>
public sealed class AiConversation
{
    private AiConversation() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Channel { get; private set; } = AiChannels.Staff;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public static AiConversation Create(Guid id, Guid companyId, Guid userId, string channel, DateTimeOffset now) => new() { Id = id, CompanyId = companyId, UserId = userId, Channel = channel is AiChannels.Customer ? AiChannels.Customer : AiChannels.Staff, CreatedAtUtc = now, UpdatedAtUtc = now };
    public void Touch(DateTimeOffset now) => UpdatedAtUtc = now;
}

/// <summary>پیام‌های گفتگو (user/assistant) + متادیتای اجرای ابزارها.</summary>
public sealed class AiChatMessage
{
    private AiChatMessage() { }
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Channel { get; private set; } = AiChannels.Staff;
    public string Role { get; private set; } = "";
    public string Content { get; private set; } = "";
    public string? ToolsUsed { get; private set; }
    public int? PromptTokens { get; private set; }
    public int? CompletionTokens { get; private set; }
    public string Culture { get; private set; } = "fa";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public static AiChatMessage Create(Guid id, Guid conversationId, Guid companyId, Guid userId, string channel, string role, string content, string? toolsUsed, int? promptTokens, int? completionTokens, string culture, DateTimeOffset now) => new()
    {
        Id = id,
        ConversationId = conversationId,
        CompanyId = companyId,
        UserId = userId,
        Channel = channel is AiChannels.Customer ? AiChannels.Customer : AiChannels.Staff,
        Role = role.Length is < 1 or > 20 ? "user" : role,
        Content = content.Length > 20000 ? content[..20000] : content,
        ToolsUsed = toolsUsed,
        PromptTokens = promptTokens,
        CompletionTokens = completionTokens,
        Culture = string.IsNullOrWhiteSpace(culture) ? "fa" : culture[..Math.Min(10, culture.Length)],
        CreatedAtUtc = now
    };
}

/// <summary>لاگ ممیزی تغییر تنظیمات و پرسش‌وپاسخ‌های دستیار برای بررسی مدیر.</summary>
public sealed class AiAuditEntry
{
    private AiAuditEntry() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorUsername { get; private set; } = "";
    public string Action { get; private set; } = "";
    public string Detail { get; private set; } = "";
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public static AiAuditEntry Create(Guid id, Guid companyId, Guid? actorUserId, string actorUsername, string action, string detail, DateTimeOffset now) => new()
    {
        Id = id,
        CompanyId = companyId,
        ActorUserId = actorUserId,
        ActorUsername = actorUsername.Length > 150 ? actorUsername[..150] : actorUsername,
        Action = action.Length > 100 ? action[..100] : action,
        Detail = detail.Length > 4000 ? detail[..4000] : detail,
        OccurredAtUtc = now
    };
}
