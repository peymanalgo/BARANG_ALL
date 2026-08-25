using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public sealed record CompanyView(Guid Id, string Code, string DisplayName, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record CompanySettingView(Guid Id, Guid CompanyId, string Key, string Value, CompanySettingValueType ValueType, bool IsActive, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record BranchView(Guid Id, Guid CompanyId, string Code, string DisplayName, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record OrganizationalUnitView(Guid Id, Guid CompanyId, Guid? BranchId, Guid? ParentId, string Code, string DisplayName, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record WarehouseView(Guid Id, Guid CompanyId, Guid? BranchId, string Code, string Name, WarehouseType Type, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);

public sealed record CreateCompanyCommand(string Code, string DisplayName);
public sealed record CreateCompanySettingCommand(string Key, string Value, CompanySettingValueType ValueType);
public sealed record CreateBranchCommand(string Code, string DisplayName);
public sealed record CreateOrganizationalUnitCommand(string Code, string DisplayName, Guid? BranchId, Guid? ParentId);
public sealed record CreateWarehouseCommand(string Code, string Name, WarehouseType Type, Guid? BranchId);
