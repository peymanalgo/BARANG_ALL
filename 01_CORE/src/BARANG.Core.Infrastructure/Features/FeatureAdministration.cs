using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Features;
using BARANG.Core.Domain.Ai;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Features;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARANG.Core.Infrastructure.Features;

/// <summary>مدیریت فیچرهای سیستم: فهرست با جست‌وجوی چهارزبانه، خاموش/روشن‌کردن با بهین‌یابی + لاگ ممیزی،
/// و فهرست عمومی پرچم‌ها برای مصرف ناشناس پنل/فروشگاه.</summary>
public sealed class FeatureAdministration(
    CoreDbContext db,
    ICompanyContext company,
    ICurrentActor actor,
    ITransactionExecutor transactions,
    IAuditWriter audit,
    TimeProvider time,
    ILogger<FeatureAdministration> logger) : IFeatureAdministration
{
    private static readonly Action<ILogger, string, string, Exception?> FeatureToggled = LoggerMessage.Define<string, string>(LogLevel.Information, new(3301, "FeatureToggled"), "System feature {FeatureCode} toggled to {IsEnabled}");

    private Guid CompanyId => company.RequireCompanyId();

    public async Task<Result<IReadOnlyList<SystemFeatureView>>> ListAsync(string? scope, string? search, CancellationToken ct)
    {
        string? normalizedScope = NormalizeScope(scope);
        if (scope is { Length: > 0 } && normalizedScope is null) return Result.Failure<IReadOnlyList<SystemFeatureView>>(FeatureErrors.InvalidScope);
        string? term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        List<SystemFeature> rows = await db.SystemFeatures.AsNoTracking()
            .Where(x => normalizedScope == null || x.Scope == normalizedScope)
            .Where(x => term == null
                || x.FeatureCode.Contains(term)
                || x.NameFa.Contains(term) || x.NameEn.Contains(term) || x.NameAr.Contains(term) || x.NameEs.Contains(term)
                || x.DescriptionFa.Contains(term) || x.DescriptionEn.Contains(term) || x.DescriptionAr.Contains(term) || x.DescriptionEs.Contains(term))
            .OrderBy(x => x.Scope).ThenBy(x => x.FeatureCode)
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<SystemFeatureView>>([.. rows.Select(View)]);
    }

    public Task<Result<SystemFeatureView>> SetEnabledAsync(ToggleFeatureCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        SystemFeature? feature = await db.SystemFeatures.SingleOrDefaultAsync(x => x.Id == command.Id, token);
        if (feature is null) return Result.Failure<SystemFeatureView>(FeatureErrors.NotFound);
        Result updated = feature.SetEnabled(command.IsEnabled, command.Version, time.GetUtcNow());
        if (updated.IsFailure) return Result.Failure<SystemFeatureView>(updated.Error);
        audit.Write(new("feature.toggled", "system_feature", feature.Id.ToString(), CompanyId: CompanyId, ActorUsername: actor.Actor.ActorUsername, ChangeKind: AuditChangeKind.Modified, Metadata: new Dictionary<string, object?> { ["featureCode"] = feature.FeatureCode, ["scope"] = feature.Scope, ["isEnabled"] = command.IsEnabled }));
        FeatureToggled(logger, feature.FeatureCode, command.IsEnabled ? "enabled" : "disabled", null);
        await db.SaveChangesAsync(token);
        return Result.Success(View(feature));
    }, ct);

    public async Task<Result<IReadOnlyList<FeatureFlagView>>> ListPublicAsync(string scope, CancellationToken ct)
    {
        string? normalizedScope = NormalizeScope(scope);
        if (normalizedScope is null) return Result.Failure<IReadOnlyList<FeatureFlagView>>(FeatureErrors.InvalidScope);
        List<FeatureFlagView> rows = await db.SystemFeatures.AsNoTracking()
            .Where(x => x.Scope == normalizedScope)
            .OrderBy(x => x.FeatureCode)
            .Select(x => new FeatureFlagView(x.FeatureCode, x.IsEnabled))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<FeatureFlagView>>(rows);
    }

    private static string? NormalizeScope(string? scope) => string.IsNullOrWhiteSpace(scope) ? null : scope.Trim().ToLowerInvariant() switch { "erp" => FeatureScopes.Erp, "store" => FeatureScopes.Store, _ => null };

    private static SystemFeatureView View(SystemFeature x) => new(x.Id, x.FeatureCode, x.Scope, x.NameFa, x.NameEn, x.NameAr, x.NameEs, x.DescriptionFa, x.DescriptionEn, x.DescriptionAr, x.DescriptionEs, x.IsEnabled, x.Version, x.UpdatedAtUtc);
}
