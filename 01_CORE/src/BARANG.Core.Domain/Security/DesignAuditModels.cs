using BARANG.Core.Domain.Common; using BARANG.Core.Domain.Files;

namespace BARANG.Core.Domain.Security;

// قوانین ممیزی طراحی — در ERP تعریف می‌شوند و موتور ارزیابی، وضعیت طراحی سایت پی‌رنگ
// را به‌ازای هر قانون می‌سنجد (سبز/قرمز). انواع قانون: min_resolution (حداقل رزولوشن
// به‌تفکیک اندازه چاپ)، text_in_bounds (متن داخل ناحیه چاپ + حاشیه امن)، contrast
// (کنتراست متن با رنگ پارچه). پارامترها در ParamsJson (jsonb) نگهداری می‌شوند.
public sealed class DesignAuditRule
{
    private DesignAuditRule() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string RuleType { get; private set; } = string.Empty;
    public string TitleFa { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string TitleEs { get; private set; } = string.Empty;
    public string ParamsJson { get; private set; } = "{}";
    public string? PrintSizeCode { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static readonly string[] RuleTypes = ["min_resolution", "text_in_bounds", "contrast"];

    public static Result<DesignAuditRule> Create(Guid id, Guid company, string code, string ruleType,
        string titleFa, string titleEn, string titleAr, string titleEs, string paramsJson,
        string? printSizeCode, bool isActive, DateTimeOffset now)
    {
        if (id == Guid.Empty || company == Guid.Empty) return Result.Failure<DesignAuditRule>(SecurityErrors.InvalidIdentity);
        if (string.IsNullOrWhiteSpace(code) || code.Length > 60) return Result.Failure<DesignAuditRule>(FileErrors.InvalidData);
        if (!RuleTypes.Contains(ruleType)) return Result.Failure<DesignAuditRule>(FileErrors.InvalidData);
        if (string.IsNullOrWhiteSpace(titleFa)) return Result.Failure<DesignAuditRule>(FileErrors.InvalidData);
        return Result.Success(new DesignAuditRule
        {
            Id = id, CompanyId = company, Code = code.Trim(), RuleType = ruleType,
            TitleFa = titleFa.Trim(), TitleEn = titleEn.Trim(), TitleAr = titleAr.Trim(), TitleEs = titleEs.Trim(),
            ParamsJson = string.IsNullOrWhiteSpace(paramsJson) ? "{}" : paramsJson,
            PrintSizeCode = string.IsNullOrWhiteSpace(printSizeCode) ? null : printSizeCode.Trim().ToUpperInvariant(),
            IsActive = isActive, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1
        });
    }

    public Result Update(string ruleType, string titleFa, string titleEn, string titleAr, string titleEs,
        string paramsJson, string? printSizeCode, bool isActive, DateTimeOffset now, long expectedVersion)
    {
        if (Version != expectedVersion) return Result.Failure(Error.Concurrency("core.database.concurrency_conflict"));
        if (!RuleTypes.Contains(ruleType)) return Result.Failure(FileErrors.InvalidData);
        if (string.IsNullOrWhiteSpace(titleFa)) return Result.Failure(FileErrors.InvalidData);
        RuleType = ruleType; TitleFa = titleFa.Trim(); TitleEn = titleEn.Trim(); TitleAr = titleAr.Trim(); TitleEs = titleEs.Trim();
        ParamsJson = string.IsNullOrWhiteSpace(paramsJson) ? "{}" : paramsJson;
        PrintSizeCode = string.IsNullOrWhiteSpace(printSizeCode) ? null : printSizeCode.Trim().ToUpperInvariant();
        IsActive = isActive; UpdatedAtUtc = now; Version++;
        return Result.Success();
    }
}
