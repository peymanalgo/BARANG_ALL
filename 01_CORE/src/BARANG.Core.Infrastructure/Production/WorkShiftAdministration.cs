using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Production;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Production;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CA1862

namespace BARANG.Core.Infrastructure.Production;

public sealed class WorkShiftAdministration(CoreDbContext db,ITransactionExecutor tx,ICompanyContext company,IAuditWriter audit,TimeProvider time):IWorkShiftAdministration
{
 private Guid Company=>company.RequireCompanyId(); private DateTimeOffset Now=>time.GetUtcNow();
 public Task<Result<IReadOnlyList<WorkShiftView>>> ListAsync(bool includeInactive,CancellationToken ct)=>tx.ExecuteAsync(async t=>{List<WorkShift> rows=await db.WorkShifts.AsNoTracking().Where(x=>includeInactive||x.IsActive).OrderBy(x=>x.StartTime).ThenBy(x=>x.Code).ToListAsync(t);return Result.Success<IReadOnlyList<WorkShiftView>>(rows.Select(View).ToArray());},ct);
 public Task<Result<WorkShiftView>> CreateAsync(CreateWorkShiftCommand command,CancellationToken ct)=>tx.ExecuteAsync(async t=>{if(await db.WorkShifts.AnyAsync(x=>x.Code==command.Code.Trim().ToUpperInvariant(),t))return Result.Failure<WorkShiftView>(ProductionErrors.Invalid);Result<WorkShift> made=WorkShift.Create(Guid.NewGuid(),Company,command.Code,command.Title,command.StartTime,command.EndTime,Now);if(made.IsFailure)return Result.Failure<WorkShiftView>(made.Error);db.WorkShifts.Add(made.Value);Log("workshift.created",made.Value.Id);await db.SaveChangesAsync(t);return Result.Success(View(made.Value));},ct);
 public Task<Result<WorkShiftView>> UpdateAsync(Guid id,UpdateWorkShiftCommand command,CancellationToken ct)=>tx.ExecuteAsync(async t=>{WorkShift? x=await db.WorkShifts.SingleOrDefaultAsync(s=>s.Id==id,t);if(x is null)return Result.Failure<WorkShiftView>(ProductionErrors.NotFound);Result r=x.Update(command.Title,command.StartTime,command.EndTime,command.Version,Now);if(r.IsFailure)return Result.Failure<WorkShiftView>(r.Error);Log("workshift.updated",x.Id);await db.SaveChangesAsync(t);return Result.Success(View(x));},ct);
 public Task<Result<WorkShiftView>> SetActiveAsync(Guid id,WorkShiftVersionCommand command,CancellationToken ct)=>tx.ExecuteAsync(async t=>{WorkShift? x=await db.WorkShifts.SingleOrDefaultAsync(s=>s.Id==id,t);if(x is null)return Result.Failure<WorkShiftView>(ProductionErrors.NotFound);if(x.Version!=command.Version)return Result.Failure<WorkShiftView>(ProductionErrors.Concurrency);x.SetActive(!x.IsActive,Now);Log("workshift.activation",x.Id);await db.SaveChangesAsync(t);return Result.Success(View(x));},ct);
 private void Log(string action,Guid id)=>audit.Write(new(action,"work_shift",id.ToString(),ChangeKind:AuditChangeKind.Modified));
 private static WorkShiftView View(WorkShift x)=>new(x.Id,x.Code,x.Title,x.StartTime,x.EndTime,x.IsActive,x.Version);
}
