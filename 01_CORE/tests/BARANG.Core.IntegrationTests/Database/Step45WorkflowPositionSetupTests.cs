using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class Step45WorkflowPositionSetupTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task CreatesCompanyScopedWorkflowPositionsForAcceptanceFixture()
    {
        if (!Guid.TryParse(Environment.GetEnvironmentVariable("BARANG_ACCEPTANCE_COMPANY_ID"), out Guid companyId))
            return;

        CancellationToken ct = TestContext.Current.CancellationToken;
        await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Company? existingCompany = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, ct);
        if (existingCompany is null)
        {
            db.Companies.Add(Company.Create(companyId, "STEP45B", "STEP 4.5 Browser Acceptance", now).Value);
            await db.SaveChangesAsync(ct);
        }
        Guid branchId = await db.Branches.Where(x => x.CompanyId == companyId).Select(x => x.Id).FirstOrDefaultAsync(ct);
        if (branchId == Guid.Empty)
        {
            branchId = Guid.NewGuid();
            db.Branches.Add(Branch.Create(branchId, companyId, "BROWSER-MAIN", "Browser Main", now).Value);
            await db.SaveChangesAsync(ct);
        }
        OrganizationalUnit? unit = await db.OrganizationalUnits.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Code == "BROWSER-OPS", ct);
        if (unit is null)
        {
            unit = OrganizationalUnit.Create(Guid.NewGuid(), companyId, branchId, null, "BROWSER-OPS", "Browser Operations", now).Value;
            db.OrganizationalUnits.Add(unit);
            await db.SaveChangesAsync(ct);
        }

        string[] positions = ["01", "FINC", "PROD", "DSGN", "WHSE", "PRNT", "PRES", "QCTL", "PACK", "SHIP", "SN"];
        foreach (string code in positions)
        {
            if (await db.Positions.AnyAsync(x => x.CompanyId == companyId && x.Code == code, ct)) continue;
            db.Positions.Add(Position.Create(Guid.NewGuid(), companyId, unit.Id, null, code, code, null, "STEP45 isolated workflow fixture", now).Value);
        }
        await db.SaveChangesAsync(ct);
    }
}
