using BARANG.Core.Application.Ai;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Ai;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>مدیریت تنظیمات مدل زبانی: ذخیره با کلید رمزنگاری‌شده، تست اتصال و لاگ ممیزی تغییرات.</summary>
public sealed class AiModelAdministration(
    CoreDbContext db,
    ICompanyContext company,
    ICurrentActor actor,
    ITransactionExecutor transactions,
    IAuditWriter audit,
    AiLlmClient llm,
    TimeProvider time,
    IConfiguration configuration,
    ILogger<AiModelAdministration> logger) : IAiSettingsAdministration
{
    private static readonly Action<ILogger, string, Exception?> SettingsSaved = LoggerMessage.Define<string>(LogLevel.Information, new(3101, "AiSettingsSaved"), "AI model settings changed by {Username}");

    private string EncryptionKey => configuration["Ai:SettingsEncryptionKey"] ?? string.Empty;
    private Guid CompanyId => company.RequireCompanyId();

    public async Task<Result<AiModelSettingsView>> GetSettingsAsync(CancellationToken ct)
    {
        AiModelSettings? settings = await db.AiModelSettings.FirstOrDefaultAsync(ct);
        return Result.Success(View(settings));
    }

    public Task<Result<AiModelSettingsView>> SaveSettingsAsync(SaveAiModelSettingsCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid companyId = CompanyId;
        DateTimeOffset now = time.GetUtcNow();
        AiModelSettings? settings = await db.AiModelSettings.FirstOrDefaultAsync(token);
        string? cipher = null;
        string? hint = null;
        if (!string.IsNullOrWhiteSpace(command.ApiKey))
        {
            cipher = AiSecretCipher.Encrypt(command.ApiKey.Trim(), EncryptionKey);
            hint = AiSecretCipher.Mask(command.ApiKey.Trim());
        }
        if (settings is null)
        {
            Result<AiModelSettings> created = AiModelSettings.Create(Guid.NewGuid(), companyId, command.ProviderName, command.BaseUrl, cipher, hint, command.ModelName, command.Temperature, command.MaxOutputTokens, command.TimeoutSeconds, command.SystemPromptOverride, command.ToolCallingEnabled, command.IsEnabled, now);
            if (created.IsFailure) return Result.Failure<AiModelSettingsView>(created.Error);
            db.AiModelSettings.Add(created.Value);
            settings = created.Value;
        }
        else
        {
            Result updated = settings.Update(command.ProviderName, command.BaseUrl, cipher, hint, command.ModelName, command.Temperature, command.MaxOutputTokens, command.TimeoutSeconds, command.SystemPromptOverride, command.ToolCallingEnabled, command.IsEnabled, command.Version, now);
            if (updated.IsFailure) return Result.Failure<AiModelSettingsView>(updated.Error);
        }
        db.AiAuditEntries.Add(AiAuditEntry.Create(Guid.NewGuid(), companyId, ActorId(), actor.Actor.ActorUsername ?? "", "settings.saved", DescribeChange(command), now));
        audit.Write(new(AuditActions.ConfigurationChanged, "ai_model_settings", settings.Id.ToString(), CompanyId: companyId, ActorUsername: actor.Actor.ActorUsername, ChangeKind: AuditChangeKind.Modified, Metadata: new Dictionary<string, object?> { ["provider"] = command.ProviderName, ["model"] = command.ModelName, ["enabled"] = command.IsEnabled }));
        LogSettingsSaved(logger, actor.Actor.ActorUsername ?? "unknown");
        await db.SaveChangesAsync(token);
        return Result.Success(View(settings));
    }, ct);

    public async Task<Result<AiTestConnectionResult>> TestConnectionAsync(CancellationToken ct)
    {
        AiModelSettings? settings = await db.AiModelSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return Result.Failure<AiTestConnectionResult>(AiErrors.SettingsNotFound);
        string? apiKey = AiSecretCipher.TryDecrypt(settings.ApiKeyCipher, EncryptionKey);
        (bool ok, int latency, string? reply, string? error) = await llm.PingAsync(settings.BaseUrl, apiKey, settings.ModelName, TimeSpan.FromSeconds(Math.Min(settings.TimeoutSeconds, 30)), ct);
        DateTimeOffset now = time.GetUtcNow();
        db.AiAuditEntries.Add(AiAuditEntry.Create(Guid.NewGuid(), CompanyId, ActorId(), actor.Actor.ActorUsername ?? "", "settings.tested", $"{(ok ? "ok" : "failed")} · {latency}ms{(error is null ? "" : " · " + error)}", now));
        await db.SaveChangesAsync(ct);
        return Result.Success(new AiTestConnectionResult(ok, latency, error, reply));
    }

    public async Task<Result<IReadOnlyList<AiAuditEntryView>>> ListAuditAsync(int take, CancellationToken ct)
    {
        int limit = Math.Clamp(take, 1, 200);
        List<AiAuditEntryView> rows = await db.AiAuditEntries
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(limit)
            .Select(x => new AiAuditEntryView(x.Id, x.ActorUsername, x.Action, x.Detail, x.OccurredAtUtc))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<AiAuditEntryView>>(rows);
    }

    private static AiModelSettingsView View(AiModelSettings? settings) => settings is null
        ? new AiModelSettingsView(false, "zai", "", null, "", 0.3m, 2048, 60, null, true, false, 0, DateTimeOffset.UtcNow)
        : new(true, settings.ProviderName, settings.BaseUrl, settings.ApiKeyHint, settings.ModelName, settings.Temperature, settings.MaxOutputTokens, settings.TimeoutSeconds, settings.SystemPromptOverride, settings.ToolCallingEnabled, settings.IsEnabled, settings.Version, settings.UpdatedAtUtc);

    private static string DescribeChange(SaveAiModelSettingsCommand command) => $"provider={command.ProviderName} · model={command.ModelName} · baseUrl={command.BaseUrl} · apiKey={(string.IsNullOrWhiteSpace(command.ApiKey) ? "unchanged" : "rotated")} · enabled={command.IsEnabled} · tools={command.ToolCallingEnabled}";

    private Guid? ActorId() => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : null;

    private static void LogSettingsSaved(ILogger<AiModelAdministration> localLogger, string username) => SettingsSaved.Invoke(localLogger, username, null);
}
