using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

/// <summary>Stable organizational position definition. A position can be vacant.</summary>
public sealed class Position
{
    private Position() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
    public Guid? ReportsToPositionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? SemanticRole { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<Position> Create(Guid id, Guid companyId, Guid unitId, Guid? reportsToPositionId,
        string code, string title, string? semanticRole, string? notes, DateTimeOffset now)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        string? normalizedTitle = OrganizationText.NormalizeName(title);
        if (id == Guid.Empty || companyId == Guid.Empty || unitId == Guid.Empty || normalizedCode is null || normalizedTitle is null)
            return Result.Failure<Position>(OrganizationErrors.InvalidAssignment);
        if (reportsToPositionId == id) return Result.Failure<Position>(OrganizationErrors.ReportingCycle);
        return Result.Success(new Position
        {
            Id = id, CompanyId = companyId, OrganizationalUnitId = unitId,
            ReportsToPositionId = reportsToPositionId, Code = normalizedCode,
            Title = normalizedTitle, SemanticRole = Clean(semanticRole), Notes = Clean(notes),
            IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1
        });
    }

    public Result Update(Guid unitId, Guid? reportsToPositionId, string code, string title,
        string? semanticRole, string? notes, DateTimeOffset now)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        string? normalizedTitle = OrganizationText.NormalizeName(title);
        if (unitId == Guid.Empty || normalizedCode is null || normalizedTitle is null)
            return Result.Failure(OrganizationErrors.InvalidAssignment);
        if (reportsToPositionId == Id) return Result.Failure(OrganizationErrors.ReportingCycle);
        OrganizationalUnitId = unitId; ReportsToPositionId = reportsToPositionId;
        Code = normalizedCode; Title = normalizedTitle; SemanticRole = Clean(semanticRole); Notes = Clean(notes);
        Touch(now); return Result.Success();
    }

    public void SetActive(bool active, DateTimeOffset now) { if (IsActive != active) { IsActive = active; Touch(now); } }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}




