using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public interface IOrganizationStore
{
    void AddCompany(Company company);
    void AddSetting(CompanySetting setting);
    void AddBranch(Branch branch);
    void AddUnit(OrganizationalUnit unit);
    void AddWarehouse(Warehouse warehouse);
    Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct);
    Task<Company?> FindActiveCompanyAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Company>> ListCompaniesAsync(CancellationToken ct);
    Task<bool> CompanyCodeExistsAsync(string code, CancellationToken ct);
    Task<CompanySetting?> FindSettingAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CompanySetting>> ListSettingsAsync(CancellationToken ct);
    Task<bool> SettingKeyExistsAsync(string key, CancellationToken ct);
    Task<Branch?> FindBranchAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Branch>> ListBranchesAsync(CancellationToken ct);
    Task<bool> BranchCodeExistsAsync(string code, CancellationToken ct);
    Task<OrganizationalUnit?> FindUnitAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalUnit>> ListUnitsAsync(CancellationToken ct);
    Task<bool> UnitCodeExistsAsync(string code, CancellationToken ct);
    Task<bool> UnitWouldCreateCycleAsync(Guid unitId, Guid? parentId, CancellationToken ct);
    Task<Warehouse?> FindWarehouseAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(CancellationToken ct);
    Task<bool> WarehouseCodeExistsAsync(string code, CancellationToken ct);
}
