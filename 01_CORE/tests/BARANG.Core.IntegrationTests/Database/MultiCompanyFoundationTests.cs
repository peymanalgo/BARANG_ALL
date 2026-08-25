using System.Net;
using System.Net.Http.Json;
using BARANG.Core.Application.Organization;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class MultiCompanyFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task CompanyContextCannotReadOrMutateAnotherCompanyResources()
    {
        SeededOrganization seed = await SeedAsync();
        using IDisposable scope = database.CompanyContext.BeginScope(seed.CompanyA);
        await using CoreDbContext context = database.CreateCoreDbContext();
        OrganizationAdministration service = CreateService(context);

        Assert.True((await service.GetBranchAsync(seed.BranchB, TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.UpdateBranchAsync(seed.BranchB, "Guessed", 1, TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.GetSettingAsync(seed.SettingB, TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.GetUnitAsync(seed.UnitB, TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.GetWarehouseAsync(seed.WarehouseB, TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.CreateUnitAsync(new("CROSS_BRANCH", "Cross", seed.BranchB, null), TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.CreateUnitAsync(new("CROSS_PARENT", "Cross", null, seed.UnitB), TestContext.Current.CancellationToken)).IsFailure);
        Assert.True((await service.CreateWarehouseAsync(new("CROSS_WH", "Cross", WarehouseType.RawMaterials, seed.BranchB), TestContext.Current.CancellationToken)).IsFailure);
    }

    [Fact]
    public async Task QueryFiltersReturnOnlyCurrentCompanyRows()
    {
        SeededOrganization seed = await SeedAsync();
        using IDisposable scope = database.CompanyContext.BeginScope(seed.CompanyA);
        await using CoreDbContext context = database.CreateCoreDbContext();
        OrganizationAdministration service = CreateService(context);

        Assert.Single((await service.ListBranchesAsync(TestContext.Current.CancellationToken)).Value);
        Assert.Single((await service.ListSettingsAsync(TestContext.Current.CancellationToken)).Value);
        Assert.Single((await service.ListUnitsAsync(TestContext.Current.CancellationToken)).Value);
        Assert.Single((await service.ListWarehousesAsync(TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task OrganizationalUnitRejectsIndirectHierarchyCycle()
    {
        SeededOrganization seed = await SeedAsync();
        using IDisposable scope = database.CompanyContext.BeginScope(seed.CompanyA);
        await using CoreDbContext context = database.CreateCoreDbContext();
        OrganizationAdministration service = CreateService(context);
        OrganizationalUnitView root = (await service.CreateUnitAsync(new("ROOT_A", "Root", null, null), TestContext.Current.CancellationToken)).Value;
        OrganizationalUnitView child = (await service.CreateUnitAsync(new("CHILD_A", "Child", null, root.Id), TestContext.Current.CancellationToken)).Value;

        Result<OrganizationalUnitView> result = await service.UpdateUnitAsync(root.Id, root.DisplayName, null, child.Id, root.Version, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrganizationErrors.HierarchyCycle, result.Error);
    }

    [Fact]
    public async Task RowLevelSecurityAndPooledConnectionContextDoNotLeak()
    {
        SeededOrganization seed = await SeedAsync();
        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await SetCompanyAsync(connection, transaction, seed.CompanyA);
            Assert.Equal(0, await CountBranchAsync(connection, transaction, seed.BranchB));
            Assert.Equal(1, await CountBranchAsync(connection, transaction, seed.BranchA));
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            Assert.Equal(0, await CountAllBranchesAsync(connection, transaction));
            await SetCompanyAsync(connection, transaction, seed.CompanyB);
            Assert.Equal(0, await CountBranchAsync(connection, transaction, seed.BranchA));
            Assert.Equal(1, await CountBranchAsync(connection, transaction, seed.BranchB));
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task CompositeForeignKeysRejectCrossCompanyAssignments()
    {
        SeededOrganization seed = await SeedAsync();
        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetCompanyAsync(connection, transaction, seed.CompanyA);
        await using NpgsqlCommand command = new("INSERT INTO barang.warehouses (id, company_id, branch_id, code, name, type, is_active, created_at_utc, updated_at_utc, version) VALUES (@id, @company, @branch, 'INVALID', 'Invalid', 'RawMaterials', true, now(), now(), 1);", connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid()); command.Parameters.AddWithValue("company", seed.CompanyA); command.Parameters.AddWithValue("branch", seed.BranchB);
        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(async () => await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    [Fact]
    public async Task OrganizationApiRequiresAuthentication()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/organization/companies", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<SeededOrganization> SeedAsync()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid companyA; Guid companyB;
        await using (CoreDbContext context = database.CreateCoreDbContext())
        {
            OrganizationAdministration service = CreateService(context);
            companyA = (await service.CreateCompanyAsync(new("COMPANY_A", "Company A"), TestContext.Current.CancellationToken)).Value.Id;
            companyB = (await service.CreateCompanyAsync(new("COMPANY_B", "Company B"), TestContext.Current.CancellationToken)).Value.Id;
        }
        ScopedIds a = await SeedScopedAsync(companyA, "A"); ScopedIds b = await SeedScopedAsync(companyB, "B");
        return new(companyA, companyB, a.Branch, b.Branch, b.Setting, b.Unit, b.Warehouse);
    }

    private async Task<ScopedIds> SeedScopedAsync(Guid companyId, string suffix)
    {
        using IDisposable scope = database.CompanyContext.BeginScope(companyId);
        await using CoreDbContext context = database.CreateCoreDbContext();
        OrganizationAdministration service = CreateService(context);
        Guid branch = (await service.CreateBranchAsync(new($"BRANCH_{suffix}", $"Branch {suffix}"), TestContext.Current.CancellationToken)).Value.Id;
        Guid setting = (await service.CreateSettingAsync(new($"SETTING_{suffix}", "true", CompanySettingValueType.Boolean), TestContext.Current.CancellationToken)).Value.Id;
        Guid unit = (await service.CreateUnitAsync(new($"UNIT_{suffix}", $"Unit {suffix}", branch, null), TestContext.Current.CancellationToken)).Value.Id;
        Guid warehouse = (await service.CreateWarehouseAsync(new($"WAREHOUSE_{suffix}", $"Warehouse {suffix}", WarehouseType.RawMaterials, branch), TestContext.Current.CancellationToken)).Value.Id;
        return new(branch, setting, unit, warehouse);
    }

    private OrganizationAdministration CreateService(CoreDbContext context) => new(new OrganizationStore(context), new EfTransactionExecutor(context, database.CompanyContext), database.CompanyContext, TimeProvider.System);
    private static async Task SetCompanyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid companyId) { await using NpgsqlCommand command = new("SELECT set_config('barang.company_id', @company_id, true);", connection, transaction); command.Parameters.AddWithValue("company_id", companyId.ToString()); await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); }
    private static async Task<long> CountBranchAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid branchId) { await using NpgsqlCommand command = new("SELECT count(*) FROM barang.branches WHERE id = @id;", connection, transaction); command.Parameters.AddWithValue("id", branchId); return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture); }
    private static async Task<long> CountAllBranchesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction) { await using NpgsqlCommand command = new("SELECT count(*) FROM barang.branches;", connection, transaction); return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture); }
    private sealed record ScopedIds(Guid Branch, Guid Setting, Guid Unit, Guid Warehouse);
    private sealed record SeededOrganization(Guid CompanyA, Guid CompanyB, Guid BranchA, Guid BranchB, Guid SettingB, Guid UnitB, Guid WarehouseB);
}
