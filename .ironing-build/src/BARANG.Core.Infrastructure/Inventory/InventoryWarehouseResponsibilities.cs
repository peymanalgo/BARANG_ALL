using System.Data.Common;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BARANG.Core.Infrastructure.Inventory;

public sealed partial class InventoryAdministration
{
    public Task<Result<IReadOnlyList<WarehouseResponsibilityView>>> WarehouseResponsibilitiesAsync(Guid warehouseId, CancellationToken cancellationToken) => tx.ExecuteAsync(async t =>
    {
        await using DbCommand q = ResponsibilityCommand("select r.id,r.warehouse_id,r.application_user_id,u.username,u.display_name,r.is_active,r.version from barang.warehouse_responsibilities r join barang.application_users u on u.id=r.application_user_id where r.company_id=@company and r.warehouse_id=@warehouse order by u.display_name", ("company", Company), ("warehouse", warehouseId));
        await using DbDataReader reader = await q.ExecuteReaderAsync(t); List<WarehouseResponsibilityView> rows = [];
        while (await reader.ReadAsync(t)) rows.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5), reader.GetInt64(6)));
        return Result.Success<IReadOnlyList<WarehouseResponsibilityView>>(rows);
    }, cancellationToken);

    public Task<Result<WarehouseResponsibilityView>> AddWarehouseResponsibilityAsync(CreateWarehouseResponsibilityCommand command, CancellationToken cancellationToken) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == command.WarehouseId && x.IsActive, t) || !await db.ApplicationUsers.AnyAsync(x => x.Id == command.ApplicationUserId && x.AccountState == BARANG.Core.Domain.Security.UserAccountState.Active, t) || !await db.UserCompanies.AnyAsync(x => x.UserId == command.ApplicationUserId && x.CompanyId == Company && x.ValidFromUtc <= Now && (x.ValidToUtc == null || x.ValidToUtc > Now), t))
            return Result.Failure<WarehouseResponsibilityView>(Error.BusinessRule("inventory.warehouse_responsibility.invalid_scope"));
        Guid id = Guid.NewGuid();
        await using (DbCommand insert = ResponsibilityCommand("insert into barang.warehouse_responsibilities(id,company_id,warehouse_id,application_user_id,is_active,created_at_utc,updated_at_utc,version) values(@id,@company,@warehouse,@user,true,@now,@now,1)", ("id", id), ("company", Company), ("warehouse", command.WarehouseId), ("user", command.ApplicationUserId), ("now", Now)))
        {
            try { await insert.ExecuteNonQueryAsync(t); } catch (DbException) { return Result.Failure<WarehouseResponsibilityView>(Error.Conflict("inventory.warehouse_responsibility.duplicate")); }
        }
        await using DbCommand read = ResponsibilityCommand("select r.id,r.warehouse_id,r.application_user_id,u.username,u.display_name,r.is_active,r.version from barang.warehouse_responsibilities r join barang.application_users u on u.id=r.application_user_id where r.company_id=@company and r.id=@id", ("company", Company), ("id", id));
        await using DbDataReader reader = await read.ExecuteReaderAsync(t); await reader.ReadAsync(t);
        return Result.Success(new WarehouseResponsibilityView(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5), reader.GetInt64(6)));
    }, cancellationToken);

    public Task<Result> SetWarehouseResponsibilityActiveAsync(Guid id, bool active, long version, CancellationToken cancellationToken) => tx.ExecuteAsync(async t =>
    {
        await using DbCommand q = ResponsibilityCommand("update barang.warehouse_responsibilities set is_active=@active,updated_at_utc=@now,version=version+1 where company_id=@company and id=@id and version=@version", ("active", active), ("now", Now), ("company", Company), ("id", id), ("version", version));
        return await q.ExecuteNonQueryAsync(t) == 1 ? Result.Success() : Result.Failure(Error.Concurrency("inventory.warehouse_responsibility.concurrency"));
    }, cancellationToken);

    private DbCommand ResponsibilityCommand(string sql, params (string Name, object? Value)[] values)
    {
        DbCommand q = db.Database.GetDbConnection().CreateCommand(); q.CommandText = sql; q.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        foreach ((string name, object? value) in values) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; q.Parameters.Add(p); }
        return q;
    }
}
