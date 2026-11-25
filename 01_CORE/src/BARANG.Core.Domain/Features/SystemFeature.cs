using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Features;

public static class FeatureErrors
{
    public static Error NotFound => Error.NotFound("features.not_found");
    public static Error Concurrency => Error.Concurrency("features.concurrency");
    public static Error InvalidScope => Error.Validation("features.invalid_scope");
}

/// <summary>اسکوپِ فیچر: «erp» برای پنل مدیریت (Backoffice) و «store» برای سایت پی‌رنگ (B2C).</summary>
public static class FeatureScopes
{
    public const string Erp = "erp";
    public const string Store = "store";
    public static bool IsValid(string scope) => scope is Erp or Store;
}

/// <summary>کد فیچرهای سیستم — هر فیچر جدید یک ردیف در یکی از دو تب «فیچرهای ERP / فیچرهای پی‌رنگ» است.</summary>
public static class FeatureCodes
{
    public const string AiAssistant = "ai.assistant";
}

/// <summary>فیچر سیستمی شرکت — کلید خاموش/روشن هر قابلیت روی ERP یا فروشگاه B2C.
/// فیچرِ خاموش یعنی آن قابلیت از سایت/پنل حذف می‌شود (مثلاً «دستیار هوشمند»). یگانه روی (شرکت، کد فیچر، اسکوپ).</summary>
public sealed class SystemFeature
{
    private SystemFeature() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string FeatureCode { get; private set; } = "";
    public string Scope { get; private set; } = FeatureScopes.Erp;
    public string NameFa { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEs { get; private set; } = "";
    public string DescriptionFa { get; private set; } = "";
    public string DescriptionEn { get; private set; } = "";
    public string DescriptionAr { get; private set; } = "";
    public string DescriptionEs { get; private set; } = "";
    public bool IsEnabled { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<SystemFeature> Create(Guid id, Guid companyId, string featureCode, string scope,
        string nameFa, string nameEn, string nameAr, string nameEs,
        string descriptionFa, string descriptionEn, string descriptionAr, string descriptionEs,
        bool isEnabled, DateTimeOffset now)
    {
        string code = featureCode.Trim();
        if (id == Guid.Empty || companyId == Guid.Empty || code.Length is 0 or > 64 || !FeatureScopes.IsValid(scope))
            return Result.Failure<SystemFeature>(FeatureErrors.InvalidScope);
        return Result.Success(new SystemFeature
        {
            Id = id,
            CompanyId = companyId,
            FeatureCode = code,
            Scope = scope,
            NameFa = Normalize(nameFa, 200),
            NameEn = Normalize(nameEn, 200),
            NameAr = Normalize(nameAr, 200),
            NameEs = Normalize(nameEs, 200),
            DescriptionFa = Normalize(descriptionFa, 600),
            DescriptionEn = Normalize(descriptionEn, 600),
            DescriptionAr = Normalize(descriptionAr, 600),
            DescriptionEs = Normalize(descriptionEs, 600),
            IsEnabled = isEnabled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1
        });
    }

    /// <summary>خاموش/روشن‌کردن فیچر با کنترل بهین‌یابی ( optimistic concurrency).</summary>
    public Result SetEnabled(bool isEnabled, long expectedVersion, DateTimeOffset now)
    {
        if (expectedVersion != Version) return Result.Failure(FeatureErrors.Concurrency);
        IsEnabled = isEnabled;
        UpdatedAtUtc = now;
        Version++;
        return Result.Success();
    }

    private static string Normalize(string? value, int max)
    {
        string v = (value ?? "").Trim();
        return v.Length <= max ? v : v[..max];
    }
}
