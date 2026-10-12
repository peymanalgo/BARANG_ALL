using BARANG.Core.Application.Organization;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class OrganizationalChartStore(CoreDbContext context) : IOrganizationalChartStore
{
    public void Add(Person value)=>context.Persons.Add(value);
    public void Add(OrganizationalAssignment value)
    {
        context.OrganizationalAssignments.Add(value);
        Result<Position> position = Position.Create(value.Id, value.CompanyId, value.OrganizationalUnitId, value.ManagerAssignmentId, value.PositionCode, value.PositionTitle, null, value.Notes, value.CreatedAtUtc);
        if (position.IsSuccess)
        {
            context.Positions.Add(position.Value);
            Result<PositionAssignment> assignment = PositionAssignment.Create(Guid.NewGuid(), value.CompanyId, value.Id, value.PersonId, value.StartDate, value.EndDate, value.IsPrimary, value.Notes, value.CreatedAtUtc);
            if (assignment.IsSuccess) context.PositionAssignments.Add(assignment.Value);
        }
    }
    public void Remove(Person value)=>context.Persons.Remove(value); public void Remove(OrganizationalAssignment value)=>context.OrganizationalAssignments.Remove(value);
    public async Task SyncAuthoritativeAssignmentAsync(OrganizationalAssignment value, CancellationToken ct)
    {
        Position? position = await context.Positions.SingleOrDefaultAsync(x => x.Id == value.Id && x.CompanyId == value.CompanyId, ct);
        if (position is null) return;
        position.Update(value.OrganizationalUnitId, value.ManagerAssignmentId, value.PositionCode, value.PositionTitle, position.SemanticRole, value.Notes, value.UpdatedAtUtc);
        PositionAssignment? assignment = await context.PositionAssignments.SingleOrDefaultAsync(x => x.PositionId == value.Id && x.CompanyId == value.CompanyId && x.Status == PositionAssignmentStatus.Active, ct);
        if (assignment is not null)
        {
            if (!value.IsActive) assignment.End(value.EndDate, value.UpdatedAtUtc); else assignment.SetActive(value.UpdatedAtUtc);
        }
    }
    public Task<Person?> FindPersonAsync(Guid id,CancellationToken ct)=>context.Persons.SingleOrDefaultAsync(x=>x.Id==id,ct); public async Task<IReadOnlyList<Person>> ListPersonsAsync(CancellationToken ct)=>await context.Persons.AsNoTracking().OrderBy(x=>x.DisplayName).ToListAsync(ct);
    public async Task<IReadOnlyList<Person>> SearchPersonsAsync(string? query,int take,CancellationToken ct){IQueryable<Person> q=context.Persons.AsNoTracking();if(!string.IsNullOrWhiteSpace(query)){string term=query.Trim();q=q.Where(x=>EF.Functions.ILike(x.DisplayName,$"%{term}%")||EF.Functions.ILike(x.PersonnelCode,$"%{term}%")||(x.NationalId!=null&&EF.Functions.ILike(x.NationalId,$"%{term}%"))||(x.Phone!=null&&EF.Functions.ILike(x.Phone,$"%{term}%")));}return await q.OrderBy(x=>x.DisplayName).Take(take).ToListAsync(ct);}
    public Task<bool> PersonnelCodeExistsAsync(string code,CancellationToken ct)=>context.Persons.AnyAsync(x=>x.PersonnelCode==code,ct);
    public async Task<string> AllocateNextPersonnelCodeAsync(Guid companyId,CancellationToken ct)
    {
        if(context.Database.IsNpgsql())
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({companyId.ToString()}, 7264319));",ct);

        string[] existing=await context.Persons.AsNoTracking().Where(x=>x.CompanyId==companyId).Select(x=>x.PersonnelCode).ToArrayAsync(ct);
        long last=existing.Select(code=>long.TryParse(code,NumberStyles.None,CultureInfo.InvariantCulture,out long value)&&value>0?value:0).DefaultIfEmpty().Max();
        return checked(last+1).ToString(CultureInfo.InvariantCulture);
    }
    public async Task<bool> PersonHasProtectedDependenciesAsync(Guid personId,CancellationToken ct) =>
        await context.Persons.AnyAsync(x=>x.Id==personId&&x.Version>1,ct) ||
        await context.OrganizationalAssignments.AnyAsync(x=>x.PersonId==personId,ct) ||
        await context.Customers.AnyAsync(x=>x.SalesRepresentativePersonId==personId,ct) ||
        await context.AuditEvents.IgnoreQueryFilters().AnyAsync(x=>x.EntityType=="person"&&x.EntityId==personId.ToString()&&x.Action!="organization.person.created",ct);
    public Task<OrganizationalAssignment?> FindAssignmentAsync(Guid id,CancellationToken ct)=>context.OrganizationalAssignments.SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<IReadOnlyList<Position>> ListPositionsAsync(CancellationToken ct)=>await context.Positions.AsNoTracking().OrderBy(x=>x.Code).ToListAsync(ct);
    public async Task<IReadOnlyList<PositionAssignment>> ListPositionAssignmentsAsync(CancellationToken ct)=>await context.PositionAssignments.AsNoTracking().OrderBy(x=>x.StartDate).ToListAsync(ct);
    public async Task<IReadOnlyList<OrganizationalAssignment>> ListAssignmentsAsync(Guid? personId,CancellationToken ct)=>await context.OrganizationalAssignments.AsNoTracking().Where(x=>personId==null||x.PersonId==personId).OrderBy(x=>x.PositionTitle).ToListAsync(ct);
    public Task<bool> UnitHasActiveAssignmentsAsync(Guid unitId,CancellationToken ct)=>context.OrganizationalAssignments.AnyAsync(x=>x.OrganizationalUnitId==unitId&&x.IsActive,ct);
    public Task<bool> UnitHasAssignmentsAsync(Guid unitId,CancellationToken ct)=>context.OrganizationalAssignments.AnyAsync(x=>x.OrganizationalUnitId==unitId,ct);
    public Task<bool> AssignmentHasActiveReportsAsync(Guid assignmentId,CancellationToken ct)=>context.OrganizationalAssignments.AnyAsync(x=>x.ManagerAssignmentId==assignmentId&&x.IsActive,ct);
    public Task<bool> AssignmentHasReportsAsync(Guid assignmentId,CancellationToken ct)=>context.OrganizationalAssignments.AnyAsync(x=>x.ManagerAssignmentId==assignmentId,ct);
    public Task<bool> ActiveAssignmentExistsAsync(Guid personId,Guid unitId,CancellationToken ct)=>context.OrganizationalAssignments.AnyAsync(x=>x.PersonId==personId&&x.OrganizationalUnitId==unitId&&x.IsActive,ct);
    public async Task<bool> PersonReportingCycleAsync(Guid personId,Guid? managerId,CancellationToken ct){Guid? cursor=managerId;HashSet<Guid>visited=[];while(cursor is{} id&&visited.Add(id)){var node=await context.OrganizationalAssignments.Where(x=>x.Id==id).Select(x=>new{x.PersonId,x.ManagerAssignmentId}).SingleOrDefaultAsync(ct);if(node is null)return false;if(node.PersonId==personId)return true;cursor=node.ManagerAssignmentId;}return cursor is not null;}
    public async Task<bool> ReportingCycleAsync(Guid assignmentId,Guid? managerId,CancellationToken ct){Guid? cursor=managerId;HashSet<Guid> visited=[];while(cursor is {} id){if(id==assignmentId||!visited.Add(id))return true;cursor=await context.OrganizationalAssignments.Where(x=>x.Id==id).Select(x=>x.ManagerAssignmentId).SingleOrDefaultAsync(ct);}return false;}
}


