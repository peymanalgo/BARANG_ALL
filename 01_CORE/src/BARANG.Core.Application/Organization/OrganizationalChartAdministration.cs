using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public sealed class OrganizationalChartAdministration(IOrganizationalChartStore chart,IOrganizationStore organization,ITransactionExecutor transactions,ICompanyContext company,TimeProvider time,IAuditWriter audit):IOrganizationalChartAdministration
{
    public Task<Result<OrganizationalChartView>> GetCompanyChartAsync(Guid companyId, CancellationToken ct) =>
        InCompanyTransaction(async token =>
        {
            if (companyId == Guid.Empty || companyId != company.RequireCompanyId())
                return Result.Failure<OrganizationalChartView>(OrganizationErrors.CrossCompanyReference);
            Company? selected = await organization.FindCompanyAsync(companyId, token);
            if (selected is null) return Result.Failure<OrganizationalChartView>(OrganizationErrors.NotFound);
            var branches = (await organization.ListBranchesAsync(token)).Where(row => row.CompanyId == companyId).ToArray();
            var units = (await organization.ListUnitsAsync(token)).Where(row => row.CompanyId == companyId).ToArray();
            HashSet<Guid> unitIds = units.Select(row => row.Id).ToHashSet();
            var people = (await chart.ListPersonsAsync(token)).Where(row => row.CompanyId == companyId).ToDictionary(row => row.Id, row => row.DisplayName);
            var assignments = (await chart.ListAssignmentsAsync(null, token))
                .Where(row => row.CompanyId == companyId && unitIds.Contains(row.OrganizationalUnitId)).ToArray();
            var positions = (await chart.ListPositionsAsync(token)).Where(row => row.CompanyId == companyId && unitIds.Contains(row.OrganizationalUnitId)).ToArray();
            var positionAssignments = (await chart.ListPositionAssignmentsAsync(token)).Where(row => row.CompanyId == companyId && positions.Any(p => p.Id == row.PositionId)).ToArray();
            var positionViews = positions.Select(position => new PositionView(position.Id, position.CompanyId, position.OrganizationalUnitId,
                units.FirstOrDefault(x => x.Id == position.OrganizationalUnitId)?.DisplayName ?? string.Empty, position.ReportsToPositionId,
                position.Code, position.Title, position.SemanticRole, position.IsActive, position.Version,
                positionAssignments.Where(a => a.PositionId == position.Id).Select(a => new PositionIncumbentView(a.Id, a.PersonId, people.GetValueOrDefault(a.PersonId, string.Empty), a.StartDate, a.EndDate, a.IsPrimary, a.Status, a.Version)).ToArray())).ToArray();
            return Result.Success(new OrganizationalChartView(
                [OrganizationAdministration.ToCompanyView(selected)],
                branches.Select(OrganizationAdministration.ToBranchView).ToArray(),
                units.Select(OrganizationAdministration.ToUnitView).ToArray(),
                assignments.Select(row => View(row, people.GetValueOrDefault(row.PersonId, string.Empty))).ToArray(), positionViews));
        }, ct);
    public Task<Result<IReadOnlyList<PersonView>>> SearchPersonsAsync(string? query,int take,CancellationToken ct)=>InCompanyTransaction(async token=>Result.Success<IReadOnlyList<PersonView>>((await chart.SearchPersonsAsync(query,Math.Clamp(take,1,100),token)).Select(View).ToArray()),ct);
    public Task<Result<PersonView>> GetPersonAsync(Guid id,CancellationToken ct)=>InCompanyTransaction(async token=>{Person? p=await chart.FindPersonAsync(id,token);return p is null?Result.Failure<PersonView>(OrganizationErrors.NotFound):Result.Success(View(p));},ct);
    public Task<Result<PersonView>> CreatePersonAsync(CreatePersonCommand command,CancellationToken ct)=>transactions.ExecuteAsync(async token=>{Guid companyId=company.RequireCompanyId();string personnelCode=await chart.AllocateNextPersonnelCodeAsync(companyId,token);Result<Person> made=Person.Create(Guid.NewGuid(),companyId,personnelCode,command.FirstName,command.LastName,command.NationalId,command.BirthDate,command.Email,command.Phone,command.Address,command.Notes,time.GetUtcNow());if(made.IsFailure)return Result.Failure<PersonView>(made.Error);chart.Add(made.Value);return Result.Success(View(made.Value));},ct);
    public Task<Result<PersonView>> UpdatePersonAsync(Guid id,CreatePersonCommand command,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>{company.RequireCompanyId();Person? p=await chart.FindPersonAsync(id,token);if(p is null)return Result.Failure<PersonView>(OrganizationErrors.NotFound);if(p.Version!=version)return Result.Failure<PersonView>(OrganizationErrors.Concurrency);Result changed=p.Update(command.FirstName,command.LastName,command.NationalId,command.BirthDate,command.Email,command.Phone,command.Address,command.Notes,time.GetUtcNow());return changed.IsFailure?Result.Failure<PersonView>(changed.Error):Result.Success(View(p));},ct);
    public Task<Result<PersonView>> SetPersonActiveAsync(Guid id,bool active,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>{company.RequireCompanyId();Person? p=await chart.FindPersonAsync(id,token);if(p is null)return Result.Failure<PersonView>(OrganizationErrors.NotFound);if(p.Version!=version)return Result.Failure<PersonView>(OrganizationErrors.Concurrency);p.SetActive(active,time.GetUtcNow());return Result.Success(View(p));},ct);
    public Task<Result> DeletePersonAsync(Guid id,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>
    {
        company.RequireCompanyId();
        Person? person=await chart.FindPersonAsync(id,token);
        if(person is null)return Result.Failure(OrganizationErrors.NotFound);
        if(person.Version!=version)return Result.Failure(OrganizationErrors.Concurrency);
        if(await chart.PersonHasProtectedDependenciesAsync(id,token))return Result.Failure(OrganizationErrors.ProtectedDependency);
        chart.Remove(person);
        audit.Write(new AuditWriteRequest("organization.person.deleted","person",id.ToString(),CompanyId:person.CompanyId,ChangeKind:AuditChangeKind.Archived));
        return Result.Success();
    },ct);
    public Task<Result<OrganizationalChartView>> GetChartAsync(Guid? highlightedPersonId,CancellationToken ct) => GetCompanyChartAsync(company.RequireCompanyId(),ct);
    public Task<Result<AssignmentView>> AssignPersonAsync(CreateAssignmentCommand command,CancellationToken ct)=>transactions.ExecuteAsync(async token=>{Guid companyId=company.RequireCompanyId();Person? p=await chart.FindPersonAsync(command.PersonId,token);OrganizationalUnit? u=await organization.FindUnitAsync(command.OrganizationalUnitId,token);if(p is null||u is null||p.CompanyId!=companyId||u.CompanyId!=companyId)return Result.Failure<AssignmentView>(OrganizationErrors.CrossCompanyReference);if(!p.IsActive||!u.IsActive)return Result.Failure<AssignmentView>(OrganizationErrors.InvalidAssignment);if(await chart.ActiveAssignmentExistsAsync(command.PersonId,command.OrganizationalUnitId,token))return Result.Failure<AssignmentView>(OrganizationErrors.DuplicateAssignment);if(command.ManagerAssignmentId is {} managerId){var manager=await chart.FindAssignmentAsync(managerId,token);if(manager is null||!manager.IsActive||manager.CompanyId!=companyId||manager.PersonId==command.PersonId||await chart.PersonReportingCycleAsync(command.PersonId,command.ManagerAssignmentId,token))return Result.Failure<AssignmentView>(OrganizationErrors.ReportingCycle);}Result<OrganizationalAssignment> made=OrganizationalAssignment.Create(Guid.NewGuid(),companyId,command.PersonId,command.OrganizationalUnitId,command.ManagerAssignmentId,command.PositionCode,command.PositionTitle,command.IsPrimary,command.StartDate,command.EndDate,time.GetUtcNow());if(made.IsFailure)return Result.Failure<AssignmentView>(made.Error);made.Value.SetNotes(command.Notes);chart.Add(made.Value);audit.Write(new AuditWriteRequest("organization.assignment.created","organizational-assignment",made.Value.Id.ToString(),CompanyId:companyId,ChangeKind:AuditChangeKind.Added));return Result.Success(View(made.Value,p.DisplayName));},ct);
    public Task<Result<AssignmentView>> ChangeAssignmentAsync(Guid id,CreateAssignmentCommand command,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>{Guid companyId=company.RequireCompanyId();var a=await chart.FindAssignmentAsync(id,token);var p=await chart.FindPersonAsync(command.PersonId,token);var u=await organization.FindUnitAsync(command.OrganizationalUnitId,token);if(a is null||p is null||u is null)return Result.Failure<AssignmentView>(OrganizationErrors.NotFound);if(a.Version!=version)return Result.Failure<AssignmentView>(OrganizationErrors.Concurrency);if(a.PersonId!=command.PersonId||a.CompanyId!=companyId||p.CompanyId!=companyId||u.CompanyId!=companyId)return Result.Failure<AssignmentView>(OrganizationErrors.CrossCompanyReference);if(!a.IsActive||!p.IsActive||!u.IsActive)return Result.Failure<AssignmentView>(OrganizationErrors.InvalidAssignment);if(a.OrganizationalUnitId!=command.OrganizationalUnitId&&await chart.ActiveAssignmentExistsAsync(command.PersonId,command.OrganizationalUnitId,token))return Result.Failure<AssignmentView>(OrganizationErrors.DuplicateAssignment);if(command.ManagerAssignmentId is {} managerId){var manager=await chart.FindAssignmentAsync(managerId,token);if(manager is null||!manager.IsActive||manager.CompanyId!=companyId||manager.PersonId==command.PersonId)return Result.Failure<AssignmentView>(OrganizationErrors.ReportingCycle);}if(await chart.ReportingCycleAsync(id,command.ManagerAssignmentId,token)||await chart.PersonReportingCycleAsync(command.PersonId,command.ManagerAssignmentId,token))return Result.Failure<AssignmentView>(OrganizationErrors.ReportingCycle);Result changed=a.Change(command.OrganizationalUnitId,command.ManagerAssignmentId,command.PositionCode,command.PositionTitle,command.IsPrimary,command.StartDate,command.EndDate,time.GetUtcNow());if(changed.IsFailure)return Result.Failure<AssignmentView>(changed.Error);a.SetNotes(command.Notes);await chart.SyncAuthoritativeAssignmentAsync(a,token);audit.Write(new AuditWriteRequest("organization.assignment.updated","organizational-assignment",a.Id.ToString(),CompanyId:a.CompanyId,ChangeKind:AuditChangeKind.Modified));return Result.Success(View(a,p.DisplayName));},ct);
    public Task<Result<AssignmentView>> SetAssignmentActiveAsync(Guid id,bool active,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>
    {
        Guid companyId=company.RequireCompanyId();
        OrganizationalAssignment? assignment=await chart.FindAssignmentAsync(id,token);
        if(assignment is null)return Result.Failure<AssignmentView>(OrganizationErrors.NotFound);
        if(assignment.Version!=version)return Result.Failure<AssignmentView>(OrganizationErrors.Concurrency);
        Person? person=await chart.FindPersonAsync(assignment.PersonId,token);
        OrganizationalUnit? unit=await organization.FindUnitAsync(assignment.OrganizationalUnitId,token);
        if(person is null||unit is null||assignment.CompanyId!=companyId||person.CompanyId!=companyId||unit.CompanyId!=companyId)return Result.Failure<AssignmentView>(OrganizationErrors.CrossCompanyReference);
        if(active)
        {
            if(!person.IsActive||!unit.IsActive)return Result.Failure<AssignmentView>(OrganizationErrors.InvalidAssignment);
            if(!assignment.IsActive&&await chart.ActiveAssignmentExistsAsync(assignment.PersonId,assignment.OrganizationalUnitId,token))return Result.Failure<AssignmentView>(OrganizationErrors.DuplicateAssignment);
            if(assignment.ManagerAssignmentId is{} managerId)
            {
                OrganizationalAssignment? manager=await chart.FindAssignmentAsync(managerId,token);
                if(manager is null||!manager.IsActive||manager.CompanyId!=companyId||manager.PersonId==assignment.PersonId||await chart.ReportingCycleAsync(id,managerId,token)||await chart.PersonReportingCycleAsync(assignment.PersonId,managerId,token))return Result.Failure<AssignmentView>(OrganizationErrors.ReportingCycle);
            }
            assignment.Activate(time.GetUtcNow()); await chart.SyncAuthoritativeAssignmentAsync(assignment,token);
        }
        else
        {
            if(await chart.AssignmentHasActiveReportsAsync(id,token))return Result.Failure<AssignmentView>(OrganizationErrors.ActiveDependents);
            assignment.Deactivate(time.GetUtcNow()); await chart.SyncAuthoritativeAssignmentAsync(assignment,token);
        }
        audit.Write(new AuditWriteRequest(active?"organization.assignment.activated":"organization.assignment.deactivated","organizational-assignment",assignment.Id.ToString(),CompanyId:assignment.CompanyId,ChangeKind:AuditChangeKind.Modified));
        return Result.Success(View(assignment,person.DisplayName));
    },ct);
    public Task<Result> RemoveAssignmentAsync(Guid id,long version,CancellationToken ct)=>transactions.ExecuteAsync(async token=>
    {
        company.RequireCompanyId();
        OrganizationalAssignment? assignment=await chart.FindAssignmentAsync(id,token);
        if(assignment is null)return Result.Failure(OrganizationErrors.NotFound);
        if(assignment.Version!=version)return Result.Failure(OrganizationErrors.Concurrency);
        if(await chart.AssignmentHasReportsAsync(id,token))return Result.Failure(OrganizationErrors.DeleteHasChildren);
        chart.Remove(assignment);
        audit.Write(new AuditWriteRequest("organization.assignment.deleted","organizational-assignment",assignment.Id.ToString(),CompanyId:assignment.CompanyId,ChangeKind:AuditChangeKind.Archived));
        return Result.Success();
    },ct);
    private static PersonView View(Person p)=>new(p.Id,p.CompanyId,p.PersonnelCode,p.FirstName,p.LastName,p.DisplayName,p.NationalId,p.BirthDate,p.Email,p.Phone,p.Address,p.Notes,p.IsActive,p.Version);
    private static AssignmentView View(OrganizationalAssignment a,string name)=>new(a.Id,a.CompanyId,a.PersonId,name,a.OrganizationalUnitId,a.ManagerAssignmentId,a.PositionCode,a.PositionTitle,a.IsPrimary,a.StartDate,a.EndDate,a.IsActive,a.Version,a.Notes);
    private Task<Result<T>> InCompanyTransaction<T>(Func<CancellationToken,Task<Result<T>>> operation,CancellationToken ct){company.RequireCompanyId();return transactions.ExecuteAsync(operation,ct);}
}










