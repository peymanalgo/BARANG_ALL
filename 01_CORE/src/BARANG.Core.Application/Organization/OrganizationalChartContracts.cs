using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public sealed record PersonView(Guid Id,Guid CompanyId,string PersonnelCode,string FirstName,string LastName,string DisplayName,string? NationalId,DateOnly? BirthDate,string? Email,string? Phone,string? Address,string? Notes,bool IsActive,long Version);
public sealed record AssignmentView(Guid Id,Guid CompanyId,Guid PersonId,string PersonName,Guid OrganizationalUnitId,Guid? ManagerAssignmentId,string PositionCode,string PositionTitle,bool IsPrimary,DateOnly? StartDate,DateOnly? EndDate,bool IsActive,long Version,string? Notes=null);
public sealed record OrganizationalChartView(IReadOnlyList<CompanyView> Companies,IReadOnlyList<BranchView> Branches,IReadOnlyList<OrganizationalUnitView> Units,IReadOnlyList<AssignmentView> Assignments, IReadOnlyList<PositionView>? Positions = null);
public sealed record CreatePersonCommand(string PersonnelCode,string FirstName,string LastName,string? NationalId,DateOnly? BirthDate,string? Email,string? Phone,string? Address,string? Notes);
public sealed record CreateAssignmentCommand(Guid PersonId,Guid OrganizationalUnitId,Guid? ManagerAssignmentId,string PositionCode,string PositionTitle,bool IsPrimary,DateOnly? StartDate,DateOnly? EndDate,string? Notes=null);

public interface IOrganizationalChartStore
{
    void Add(Person value); void Add(OrganizationalAssignment value); void Remove(Person value); void Remove(OrganizationalAssignment value);
    Task SyncAuthoritativeAssignmentAsync(OrganizationalAssignment value, CancellationToken ct);
    Task<Person?> FindPersonAsync(Guid id,CancellationToken ct); Task<IReadOnlyList<Person>> ListPersonsAsync(CancellationToken ct); Task<IReadOnlyList<Person>> SearchPersonsAsync(string? query,int take,CancellationToken ct); Task<bool> PersonnelCodeExistsAsync(string code,CancellationToken ct); Task<string> AllocateNextPersonnelCodeAsync(Guid companyId,CancellationToken ct); Task<bool> PersonHasProtectedDependenciesAsync(Guid personId,CancellationToken ct);
    Task<OrganizationalAssignment?> FindAssignmentAsync(Guid id,CancellationToken ct); Task<IReadOnlyList<OrganizationalAssignment>> ListAssignmentsAsync(Guid? personId,CancellationToken ct); Task<bool> ActiveAssignmentExistsAsync(Guid personId,Guid unitId,CancellationToken ct); Task<bool> UnitHasActiveAssignmentsAsync(Guid unitId,CancellationToken ct); Task<bool> UnitHasAssignmentsAsync(Guid unitId,CancellationToken ct); Task<bool> AssignmentHasActiveReportsAsync(Guid assignmentId,CancellationToken ct); Task<bool> AssignmentHasReportsAsync(Guid assignmentId,CancellationToken ct); Task<bool> ReportingCycleAsync(Guid assignmentId,Guid? managerId,CancellationToken ct); Task<bool> PersonReportingCycleAsync(Guid personId,Guid? managerId,CancellationToken ct);
    Task<IReadOnlyList<Position>> ListPositionsAsync(CancellationToken ct); Task<IReadOnlyList<PositionAssignment>> ListPositionAssignmentsAsync(CancellationToken ct);
}

public interface IOrganizationalChartAdministration
{
    Task<Result<OrganizationalChartView>> GetCompanyChartAsync(Guid companyId, CancellationToken ct);
    Task<Result<IReadOnlyList<PersonView>>> SearchPersonsAsync(string? query,int take,CancellationToken ct); Task<Result<PersonView>> GetPersonAsync(Guid id,CancellationToken ct); Task<Result<PersonView>> CreatePersonAsync(CreatePersonCommand command,CancellationToken ct); Task<Result<PersonView>> UpdatePersonAsync(Guid id,CreatePersonCommand command,long version,CancellationToken ct); Task<Result<PersonView>> SetPersonActiveAsync(Guid id,bool active,long version,CancellationToken ct); Task<Result> DeletePersonAsync(Guid id,long version,CancellationToken ct);
    Task<Result<OrganizationalChartView>> GetChartAsync(Guid? highlightedPersonId,CancellationToken ct); Task<Result<AssignmentView>> AssignPersonAsync(CreateAssignmentCommand command,CancellationToken ct); Task<Result<AssignmentView>> ChangeAssignmentAsync(Guid id,CreateAssignmentCommand command,long version,CancellationToken ct); Task<Result<AssignmentView>> SetAssignmentActiveAsync(Guid id,bool active,long version,CancellationToken ct); Task<Result> RemoveAssignmentAsync(Guid id,long version,CancellationToken ct);
}







