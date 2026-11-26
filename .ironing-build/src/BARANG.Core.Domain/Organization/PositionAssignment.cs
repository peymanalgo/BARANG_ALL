using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

/// <summary>Historical, dated incumbent assignment. Ending an assignment never changes the Position.</summary>
public sealed class PositionAssignment
{
    private PositionAssignment() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsPrimary { get; private set; }
    public PositionAssignmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<PositionAssignment> Create(Guid id, Guid companyId, Guid positionId, Guid personId,
        DateOnly? startDate, DateOnly? endDate, bool isPrimary, string? notes, DateTimeOffset now)
    {
        if (id == Guid.Empty || companyId == Guid.Empty || positionId == Guid.Empty || personId == Guid.Empty || endDate < startDate)
            return Result.Failure<PositionAssignment>(OrganizationErrors.InvalidAssignment);
        return Result.Success(new PositionAssignment
        {
            Id = id, CompanyId = companyId, PositionId = positionId, PersonId = personId,
            StartDate = startDate, EndDate = endDate, IsPrimary = isPrimary,
            Status = PositionAssignmentStatus.Active, Notes = Clean(notes),
            CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1
        });
    }

    public Result End(DateOnly? endDate, DateTimeOffset now)
    {
        if (endDate < StartDate) return Result.Failure(OrganizationErrors.InvalidAssignment);
        EndDate = endDate; Status = PositionAssignmentStatus.Ended; Touch(now); return Result.Success();
    }
    public void SetSuspended(DateTimeOffset now) { Status = PositionAssignmentStatus.Suspended; Touch(now); }
    public void SetActive(DateTimeOffset now) { Status = PositionAssignmentStatus.Active; Touch(now); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public enum PositionAssignmentStatus
{
    Active,
    Ended,
    Suspended
}




