using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class Branch
{
    private Branch() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<Branch> Create(Guid id, Guid companyId, string code, string displayName, DateTimeOffset utcNow)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        string? normalizedName = OrganizationText.NormalizeName(displayName);
        if (id == Guid.Empty || companyId == Guid.Empty || normalizedCode is null) return Result.Failure<Branch>(OrganizationErrors.InvalidCode);
        if (normalizedName is null) return Result.Failure<Branch>(OrganizationErrors.InvalidName);
        return Result.Success(new Branch { Id = id, CompanyId = companyId, Code = normalizedCode, DisplayName = normalizedName, IsActive = true, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow, Version = 1 });
    }
    public Result Update(string displayName, DateTimeOffset utcNow) { string? name = OrganizationText.NormalizeName(displayName); if (name is null) return Result.Failure(OrganizationErrors.InvalidName); DisplayName = name; Touch(utcNow); return Result.Success(); }
    public void Activate(DateTimeOffset utcNow) { if (!IsActive) { IsActive = true; Touch(utcNow); } }
    public void Deactivate(DateTimeOffset utcNow) { if (IsActive) { IsActive = false; Touch(utcNow); } }
    private void Touch(DateTimeOffset utcNow) { UpdatedAtUtc = utcNow; Version++; }
}
