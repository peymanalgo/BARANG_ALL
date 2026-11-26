using System.Data;
using System.Data.Common;
using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Workflow;

public sealed class CartableSavedFilterAdministration(CoreDbContext db, ICompanyContext company, ICurrentActor actor, TimeProvider clock) : ICartableSavedFilterAdministration
{
    private Guid Company => company.CompanyId is { } id && id != Guid.Empty ? id : throw new InvalidOperationException("Company context is required.");
    private Guid Actor => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : throw new InvalidOperationException("Actor context is required.");
    public async Task<Result<IReadOnlyList<CartableSavedFilterView>>> ListAsync(CartableSavedFilterTab tab, CancellationToken ct)
    { await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); List<CartableSavedFilterView> rows = []; await using DbCommand q = Cmd(c, "select id,name,definition_json,version from barang.cartable_saved_filters where company_id=@company and user_id=@user and tab=@tab order by name", ("company", Company), ("user", Actor), ("tab", tab.ToString())); await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new CartableSavedFilterView(r.GetGuid(0), r.GetString(1), JsonSerializer.Deserialize<CartableSavedFilterDefinition>(r.GetString(2)) ?? new(null, null, null, null, null, null, null, null, false, false, null, null, null), r.GetInt64(3))); return Result.Success<IReadOnlyList<CartableSavedFilterView>>(rows); }
    public async Task<Result<CartableSavedFilterView>> CreateAsync(SaveCartableFilterCommand command, CancellationToken ct)
    { if (string.IsNullOrWhiteSpace(command.Name)) return Result.Failure<CartableSavedFilterView>(Error.Validation("workflow.saved_filter.name_required")); Guid id = Guid.NewGuid(); DateTimeOffset now = clock.GetUtcNow(); await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "insert into barang.cartable_saved_filters(id,company_id,user_id,tab,name,definition_version,definition_json,created_at_utc,updated_at_utc,version) values(@id,@company,@user,@tab,@name,1,@definition::jsonb,@now,@now,1)", ("id", id), ("company", Company), ("user", Actor), ("tab", command.Tab.ToString()), ("name", command.Name.Trim()), ("definition", JsonSerializer.Serialize(command.Definition)), ("now", now)); await q.ExecuteNonQueryAsync(ct); return Result.Success(new CartableSavedFilterView(id, command.Name.Trim(), command.Definition, 1)); }
    public async Task<Result<CartableSavedFilterView>> UpdateAsync(Guid id, UpdateCartableFilterCommand command, CancellationToken ct)
    { await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "update barang.cartable_saved_filters set name=@name,definition_json=@definition::jsonb,updated_at_utc=@now,version=version+1 where company_id=@company and user_id=@user and id=@id and version=@version returning version", ("name", command.Name.Trim()), ("definition", JsonSerializer.Serialize(command.Definition)), ("now", clock.GetUtcNow()), ("company", Company), ("user", Actor), ("id", id), ("version", command.Version)); object? value = await q.ExecuteScalarAsync(ct); return value is null ? Result.Failure<CartableSavedFilterView>(Error.Concurrency("workflow.saved_filter.concurrency")) : Result.Success(new CartableSavedFilterView(id, command.Name.Trim(), command.Definition, Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture))); }
    public async Task<Result> DeleteAsync(Guid id, long version, CancellationToken ct)
    { await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "delete from barang.cartable_saved_filters where company_id=@company and user_id=@user and id=@id and version=@version", ("company", Company), ("user", Actor), ("id", id), ("version", version)); return await q.ExecuteNonQueryAsync(ct) == 1 ? Result.Success() : Result.Failure(Error.Concurrency("workflow.saved_filter.concurrency")); }
    private async Task SetCompany(DbConnection c, CancellationToken ct) { await using DbCommand q = c.CreateCommand(); q.CommandText = "select set_config('barang.company_id',cast(@company as text),false)"; DbParameter p = q.CreateParameter(); p.ParameterName = "company"; p.Value = Company; q.Parameters.Add(p); await q.ExecuteScalarAsync(ct); }
    private static DbCommand Cmd(DbConnection c, string sql, params (string Name, object? Value)[] values) { DbCommand q = c.CreateCommand(); q.CommandText = sql; foreach (var (name, value) in values) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; q.Parameters.Add(p); } return q; }
}
