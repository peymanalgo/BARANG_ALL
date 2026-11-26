using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Catalog;

public sealed record SupplierEvaluationCommand(DateOnly EvaluationDate, DateOnly? PeriodStart, DateOnly? PeriodEnd, decimal? OverallScore, string ResultStatus, string? Notes, IReadOnlyList<SupplierEvaluationCriterionCommand> Criteria, long Version = 0);
public sealed record SupplierEvaluationCriterionCommand(Guid CriterionId, decimal Score, string? Comment);
public sealed record SupplierQualityIncidentCommand(string IncidentType, DateOnly IncidentDate, decimal? Quantity, string? Description, long Version = 0);
public sealed record SupplierQualityResolveCommand(string? CorrectiveAction, DateOnly ResolvedDate, long Version);
public sealed record SupplierApprovalCommand(string Status, long Version, Guid? QcLevelId = null);
public sealed record SupplierHoldCommand(bool Held, string? Reason, string? Notes, long Version);
public sealed record SupplierContractCommand(string ContractNumber, Guid ContractTypeId, DateOnly StartDate, DateOnly? EndDate, string? Description, decimal? ContractValue, string CurrencyCode, string Status = "Draft", long Version = 0);
public sealed record SupplierDocumentCommand(Guid DocumentTypeId, Guid? ContractId, string? DocumentNumber, DateOnly? IssueDate, DateOnly? ExpiryDate, string? Description, string? IssuingAuthority, long Version = 0);
public sealed record SupplierFileCommand(Guid FileId, Guid? FileVersionId, long Version);
public sealed record SupplierLifecycleProfileView(Guid SupplierId, string ApprovalStatus, decimal? QualityScore, Guid? QcLevelId, string? HoldReason, DateTimeOffset? HeldAtUtc, long Version);
public sealed record SupplierEvaluationView(SupplierEvaluation Evaluation, IReadOnlyList<SupplierEvaluationCriterion> Criteria);
public sealed record SupplierContractView(SupplierContract Contract, bool IsExpired, bool IsExpiringSoon);
public sealed record SupplierDocumentView(SupplierDocument Document, bool IsExpired, bool IsExpiringSoon);
public sealed record SupplierContractFileCommand(Guid FileId, Guid? FileVersionId, long Version);
public sealed record SupplierDocumentStatusCommand(string Status, long Version);
public interface ISupplierLifecycleAdministration
{
    Task<Result<SupplierLifecycleProfileView>> ProfileAsync(Guid supplier, CancellationToken ct);
    Task<Result<SupplierLifecycleProfileView>> SetApprovalAsync(Guid supplier, SupplierApprovalCommand command, CancellationToken ct);
    Task<Result<SupplierLifecycleProfileView>> SetHoldAsync(Guid supplier, SupplierHoldCommand command, CancellationToken ct);
    Task<Result<SupplierEvaluationView>> CreateEvaluationAsync(Guid supplier, SupplierEvaluationCommand command, CancellationToken ct);
    Task<Result<SupplierEvaluationView>> UpdateEvaluationAsync(Guid supplier, Guid id, SupplierEvaluationCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierEvaluationView>>> EvaluationsAsync(Guid supplier, CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierQualityIncident>>> IncidentsAsync(Guid supplier, CancellationToken ct);
    Task<Result<SupplierQualityIncident>> CreateIncidentAsync(Guid supplier, SupplierQualityIncidentCommand command, CancellationToken ct);
    Task<Result<SupplierQualityIncident>> ResolveIncidentAsync(Guid supplier, Guid id, SupplierQualityResolveCommand command, CancellationToken ct);
    Task<Result<SupplierQualityIncident>> UpdateIncidentAsync(Guid supplier, Guid id, SupplierQualityIncidentCommand command, CancellationToken ct);
    Task<Result> SetIncidentActiveAsync(Guid supplier, Guid id, bool active, long version, CancellationToken ct);
    Task<Result<SupplierContractView>> SaveContractAsync(Guid supplier, Guid? id, SupplierContractCommand command, CancellationToken ct);
    Task<Result<SupplierContractView>> AttachContractFileAsync(Guid supplier, Guid id, SupplierContractFileCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierContractView>>> ContractsAsync(Guid supplier, CancellationToken ct);
    Task<Result> DeleteContractAsync(Guid supplier, Guid id, long version, CancellationToken ct);
    Task<Result<SupplierDocumentView>> SaveDocumentAsync(Guid supplier, Guid? id, SupplierDocumentCommand command, CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierDocumentView>>> DocumentsAsync(Guid supplier, CancellationToken ct);
    Task<Result<SupplierDocumentView>> AttachDocumentFileAsync(Guid supplier, Guid id, SupplierFileCommand command, CancellationToken ct);
    Task<Result<SupplierDocumentView>> UpdateDocumentAsync(Guid supplier, Guid id, SupplierDocumentCommand command, CancellationToken ct);
    Task<Result<SupplierDocumentView>> SetDocumentStatusAsync(Guid supplier, Guid id, SupplierDocumentStatusCommand command, CancellationToken ct);
    Task<Result> DeleteDocumentAsync(Guid supplier, Guid id, long version, CancellationToken ct);
}
