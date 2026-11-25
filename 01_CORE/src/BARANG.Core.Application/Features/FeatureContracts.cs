using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Features;

public sealed record SystemFeatureView(
    Guid Id,
    string FeatureCode,
    string Scope,
    string NameFa,
    string NameEn,
    string NameAr,
    string NameEs,
    string DescriptionFa,
    string DescriptionEn,
    string DescriptionAr,
    string DescriptionEs,
    bool IsEnabled,
    long Version,
    DateTimeOffset UpdatedAtUtc);

/// <summary>پرچم عمومی برای سایت/پنل: فقط کد فیچر و وضعیت — بدون جزئیات متنی.</summary>
public sealed record FeatureFlagView(string FeatureCode, bool IsEnabled);

public sealed record ToggleFeatureCommand(Guid Id, bool IsEnabled, long Version);

public interface IFeatureAdministration
{
    /// <summary>فهرست فیچرهای شرکت جاری؛ scope اختیاری ('erp'|'store') و search روی نام/توضیح هر ۴ زبان.</summary>
    Task<Result<IReadOnlyList<SystemFeatureView>>> ListAsync(string? scope, string? search, CancellationToken ct);

    /// <summary>خاموش/روشن‌کردن فیچر با بهین‌یابی + لاگ ممیزی.</summary>
    Task<Result<SystemFeatureView>> SetEnabledAsync(ToggleFeatureCommand command, CancellationToken ct);

    /// <summary>پرچم‌های عمومی یک اسکوپ برای مصرف ناشناس (B2C / پنل).</summary>
    Task<Result<IReadOnlyList<FeatureFlagView>>> ListPublicAsync(string scope, CancellationToken ct);
}

public static class FeatureContracts
{
    /// <summary>متن پاسخ دوستانه وقتی فیچر «دستیار هوشمند» خاموش است — چهار زبانه (ERP و B2C).</summary>
    public static string AssistantDisabledText(string culture) => culture.StartsWith("fa", StringComparison.Ordinal)
        ? "دستیار هوشمند در حال حاضر غیرفعال است."
        : culture.StartsWith("ar", StringComparison.Ordinal)
            ? "المساعد الذكي معطّل حاليًا."
            : culture.StartsWith("es", StringComparison.Ordinal)
                ? "El asistente inteligente está desactivado en este momento."
                : "The AI assistant is currently disabled.";
}
