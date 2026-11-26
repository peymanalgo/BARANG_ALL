using BARANG.Core.Domain.Common;
namespace BARANG.Core.Domain.Quality;

public sealed partial class DefectType
{
    public Result EditReference(string name, string category, DefectSeverity severity, bool blocking, bool active, long expected, DateTimeOffset now)
    {
        if (Version != expected) return Result.Failure(QualityErrors.Concurrency);
        Result<DefectType> validated = Create(CompanyId, Code, name, category, severity, blocking, now);
        if (validated.IsFailure || !Enum.IsDefined(severity)) return Result.Failure(QualityErrors.Invalid);
        Name = validated.Value.Name;
        CategoryCode = validated.Value.CategoryCode;
        Severity = severity;
        IsBlocking = blocking;
        return SetActive(active, expected, now);
    }
}
