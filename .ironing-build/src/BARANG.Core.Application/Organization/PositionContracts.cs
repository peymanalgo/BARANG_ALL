using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Application.Organization;

public sealed record PositionView(Guid Id, Guid CompanyId, Guid OrganizationalUnitId, string UnitName,
    Guid? ReportsToPositionId, string Code, string Title, string? SemanticRole, bool IsActive, long Version,
    IReadOnlyList<PositionIncumbentView> Incumbents);
public sealed record PositionIncumbentView(Guid AssignmentId, Guid PersonId, string PersonName,
    DateOnly? StartDate, DateOnly? EndDate, bool IsPrimary, PositionAssignmentStatus Status, long Version);
public sealed record PositionAssignmentHistoryView(Guid Id, Guid PositionId, Guid PersonId, string PersonName,
    DateOnly? StartDate, DateOnly? EndDate, bool IsPrimary, PositionAssignmentStatus Status, long Version);
public sealed record CreatePositionCommand(Guid OrganizationalUnitId, Guid? ReportsToPositionId, string Code,
    string Title, string? SemanticRole, string? Notes);
public sealed record AssignPositionCommand(Guid PositionId, Guid PersonId, DateOnly? StartDate,
    DateOnly? EndDate, bool IsPrimary, string? Notes);
public sealed record PositionDeleteView(PositionView Position, bool Archived, string MessageCode);

public interface IPositionAdministration
{
    Task<Result<IReadOnlyList<PositionView>>> ListAsync(string? search, int take, CancellationToken ct);
    Task<Result<PositionView>> CreateAsync(CreatePositionCommand command, CancellationToken ct);
    Task<Result<PositionView>> UpdateAsync(Guid id, CreatePositionCommand command, long version, CancellationToken ct);
    Task<Result<PositionView>> SetActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<PositionDeleteView>> DeleteAsync(Guid id, long version, CancellationToken ct);
    Task<Result<PositionAssignmentHistoryView>> AssignAsync(AssignPositionCommand command, CancellationToken ct);
    Task<Result<PositionAssignmentHistoryView>> EndAssignmentAsync(Guid id, DateOnly? endDate, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<PositionAssignmentHistoryView>>> HistoryAsync(Guid positionId, CancellationToken ct);
}




