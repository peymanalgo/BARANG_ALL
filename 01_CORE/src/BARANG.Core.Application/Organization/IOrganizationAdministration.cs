using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public interface IOrganizationAdministration
{
    Task<Result<CompanyView>> CreateCompanyAsync(CreateCompanyCommand command, CancellationToken ct);
    Task<Result<CompanyView>> GetCompanyAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<CompanyView>>> ListCompaniesAsync(CancellationToken ct);
    Task<Result<CompanyView>> UpdateCompanyAsync(Guid id, string name, long version, CancellationToken ct);
    Task<Result<CompanyView>> SetCompanyActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<CompanySettingView>> CreateSettingAsync(CreateCompanySettingCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<CompanySettingView>>> ListSettingsAsync(CancellationToken ct);
    Task<Result<CompanySettingView>> GetSettingAsync(Guid id, CancellationToken ct);
    Task<Result<CompanySettingView>> UpdateSettingAsync(Guid id, string value, CompanySettingValueType type, long version, CancellationToken ct);
    Task<Result<CompanySettingView>> DeactivateSettingAsync(Guid id, long version, CancellationToken ct);
    Task<Result<BranchView>> CreateBranchAsync(CreateBranchCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<BranchView>>> ListBranchesAsync(CancellationToken ct);
    Task<Result<BranchView>> GetBranchAsync(Guid id, CancellationToken ct);
    Task<Result<BranchView>> UpdateBranchAsync(Guid id, string name, long version, CancellationToken ct);
    Task<Result<BranchView>> SetBranchActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<OrganizationalUnitView>> CreateUnitAsync(CreateOrganizationalUnitCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<OrganizationalUnitView>>> ListUnitsAsync(CancellationToken ct);
    Task<Result<OrganizationalUnitView>> GetUnitAsync(Guid id, CancellationToken ct);
    Task<Result<OrganizationalUnitView>> UpdateUnitAsync(Guid id, string name, Guid? branchId, Guid? parentId, long version, CancellationToken ct);
    Task<Result<OrganizationalUnitView>> SetUnitActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<WarehouseView>> CreateWarehouseAsync(CreateWarehouseCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<WarehouseView>>> ListWarehousesAsync(CancellationToken ct);
    Task<Result<WarehouseView>> GetWarehouseAsync(Guid id, CancellationToken ct);
    Task<Result<WarehouseView>> UpdateWarehouseAsync(Guid id, string name, WarehouseType type, Guid? branchId, long version, CancellationToken ct);
    Task<Result<WarehouseView>> SetWarehouseActiveAsync(Guid id, bool active, long version, CancellationToken ct);
}
