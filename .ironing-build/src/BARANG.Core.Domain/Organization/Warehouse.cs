using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class Warehouse
{
    private Warehouse() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WarehouseType Type { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<Warehouse> Create(Guid id, Guid companyId, Guid? branchId, string code, string name, WarehouseType type, DateTimeOffset utcNow)
    {
        string? normalizedCode = OrganizationText.NormalizeCode(code);
        string? normalizedName = OrganizationText.NormalizeName(name);
        if (id == Guid.Empty || companyId == Guid.Empty || normalizedCode is null || !Enum.IsDefined(type)) return Result.Failure<Warehouse>(OrganizationErrors.InvalidCode);
        if (normalizedName is null) return Result.Failure<Warehouse>(OrganizationErrors.InvalidName);
        return Result.Success(new Warehouse { Id = id, CompanyId = companyId, BranchId = branchId, Code = normalizedCode, Name = normalizedName, Type = type, IsActive = true, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow, Version = 1 });
    }
    public Result Update(string name, Guid? branchId, WarehouseType type, DateTimeOffset utcNow) { string? normalizedName = OrganizationText.NormalizeName(name); if (normalizedName is null || !Enum.IsDefined(type)) return Result.Failure(OrganizationErrors.InvalidName); Name = normalizedName; BranchId = branchId; Type = type; Touch(utcNow); return Result.Success(); }
    public void Activate(DateTimeOffset utcNow) { if (!IsActive) { IsActive = true; Touch(utcNow); } }
    public void Deactivate(DateTimeOffset utcNow) { if (IsActive) { IsActive = false; Touch(utcNow); } }
    private void Touch(DateTimeOffset utcNow) { UpdatedAtUtc = utcNow; Version++; }
}

public enum WarehouseType { RawMaterials, WorkInProcess, FinishedGoods, Shipping, Returns, Scrap }
