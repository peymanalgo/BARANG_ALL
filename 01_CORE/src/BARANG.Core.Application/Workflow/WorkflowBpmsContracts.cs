using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Workflow;

/// <summary>Read-side BPMS features over the existing workflow engine tables
/// (definitions/versions, instances, runtime tasks, history). No new schema.</summary>
public interface IWorkflowBpmsQueries
{
    /// <summary>KPIs: instance counts by status, task load, avg cycle time per definition version, bottleneck nodes by dwell/wait.</summary>
    Task<Result<object>> MonitoringSummaryAsync(CancellationToken ct);
    /// <summary>Paged instance list with order/customer, current open nodes, cycle time and overdue task counts.</summary>
    Task<Result<object>> InstancesAsync(string? status, int page, int pageSize, CancellationToken ct);
    /// <summary>Instance header + task timeline (actor/date/duration/outcome) + history events.</summary>
    Task<Result<object>> InstanceTimelineAsync(Guid id, CancellationToken ct);
    /// <summary>Node/edge diff summary between two definition versions.</summary>
    Task<Result<object>> VersionDiffAsync(Guid fromVersionId, Guid toVersionId, CancellationToken ct);
}

/// <summary>Write-side BPMS administration: definition version rollback (copy an old version into a new draft).</summary>
public interface IWorkflowBpmsAdministration
{
    /// <summary>Copies the given (published/superseded) version's graph into a new or existing Draft so it can be re-published (rollback path).</summary>
    Task<Result<object>> RestoreVersionDraftAsync(Guid versionId, CancellationToken ct);
}
