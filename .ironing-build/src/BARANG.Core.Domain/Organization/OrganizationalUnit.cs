using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class OrganizationalUnit
{
    private OrganizationalUnit() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<OrganizationalUnit> Create(Guid id, Guid companyId, Guid? branchId, Guid? parentId, string code, string displayName, DateTimeOffset utcNow)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        string? normalizedName = OrganizationText.NormalizeName(displayName);
        if (id == Guid.Empty || companyId == Guid.Empty || normalizedCode is null) return Result.Failure<OrganizationalUnit>(OrganizationErrors.InvalidCode);
        if (normalizedName is null) return Result.Failure<OrganizationalUnit>(OrganizationErrors.InvalidName);
        if (parentId == id) return Result.Failure<OrganizationalUnit>(OrganizationErrors.HierarchyCycle);
        return Result.Success(new OrganizationalUnit { Id = id, CompanyId = companyId, BranchId = branchId, ParentId = parentId, Code = normalizedCode, DisplayName = normalizedName, IsActive = true, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow, Version = 1 });
    }
    public Result Update(string displayName, Guid? branchId, Guid? parentId, DateTimeOffset utcNow) { if (parentId == Id) return Result.Failure(OrganizationErrors.HierarchyCycle); string? name = OrganizationText.NormalizeName(displayName); if (name is null) return Result.Failure(OrganizationErrors.InvalidName); DisplayName = name; BranchId = branchId; ParentId = parentId; Touch(utcNow); return Result.Success(); }
    public void Activate(DateTimeOffset utcNow) { if (!IsActive) { IsActive = true; Touch(utcNow); } }
    public void Deactivate(DateTimeOffset utcNow) { if (IsActive) { IsActive = false; Touch(utcNow); } }
    private void Touch(DateTimeOffset utcNow) { UpdatedAtUtc = utcNow; Version++; }
}
