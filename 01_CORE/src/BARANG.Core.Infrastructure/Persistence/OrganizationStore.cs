using BARANG.Core.Application.Organization;
using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class OrganizationStore(CoreDbContext context) : IOrganizationStore
{
    public void AddCompany(Company company) => context.Companies.Add(company);
    public void AddSetting(CompanySetting setting) => context.CompanySettings.Add(setting);
    public void AddBranch(Branch branch) => context.Branches.Add(branch);
    public void AddUnit(OrganizationalUnit unit) => context.OrganizationalUnits.Add(unit);
    public void AddWarehouse(Warehouse warehouse) => context.Warehouses.Add(warehouse);
    public Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct) => context.Companies.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Company?> FindActiveCompanyAsync(Guid id, CancellationToken ct) => context.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
    public async Task<IReadOnlyList<Company>> ListCompaniesAsync(CancellationToken ct) => await context.Companies.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public Task<bool> CompanyCodeExistsAsync(string code, CancellationToken ct) => context.Companies.AnyAsync(x => x.Code == code, ct);
    public Task<CompanySetting?> FindSettingAsync(Guid id, CancellationToken ct) => context.CompanySettings.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<CompanySetting>> ListSettingsAsync(CancellationToken ct) => await context.CompanySettings.AsNoTracking().OrderBy(x => x.Key).ToListAsync(ct);
    public Task<bool> SettingKeyExistsAsync(string key, CancellationToken ct) => context.CompanySettings.AnyAsync(x => x.Key == key, ct);
    public Task<Branch?> FindBranchAsync(Guid id, CancellationToken ct) => context.Branches.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Branch>> ListBranchesAsync(CancellationToken ct) => await context.Branches.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public Task<bool> BranchCodeExistsAsync(string code, CancellationToken ct) => context.Branches.AnyAsync(x => x.Code == code, ct);
    public Task<OrganizationalUnit?> FindUnitAsync(Guid id, CancellationToken ct) => context.OrganizationalUnits.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<OrganizationalUnit>> ListUnitsAsync(CancellationToken ct) => await context.OrganizationalUnits.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public Task<bool> UnitCodeExistsAsync(string code, CancellationToken ct) => context.OrganizationalUnits.AnyAsync(x => x.Code == code, ct);
    public async Task<bool> UnitWouldCreateCycleAsync(Guid unitId, Guid? parentId, CancellationToken ct)
    {
        Guid? cursor = parentId;
        HashSet<Guid> visited = [];
        while (cursor is { } id)
        {
            if (id == unitId || !visited.Add(id)) return true;
            cursor = await context.OrganizationalUnits.Where(x => x.Id == id).Select(x => x.ParentId).SingleOrDefaultAsync(ct);
        }
        return false;
    }
    public Task<Warehouse?> FindWarehouseAsync(Guid id, CancellationToken ct) => context.Warehouses.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(CancellationToken ct) => await context.Warehouses.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public Task<bool> WarehouseCodeExistsAsync(string code, CancellationToken ct) => context.Warehouses.AnyAsync(x => x.Code == code, ct);
}
