using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Catalog;

public static class SupplierLifecycleErrors
{
    public static Error InvalidData => Error.Validation("supplier.lifecycle.invalid_data");
    public static Error NotFound => Error.NotFound("supplier.lifecycle.not_found");
    public static Error ReferenceNotFound => Error.BusinessRule("supplier.lifecycle.reference_not_found");
    public static Error Concurrency => Error.Concurrency("supplier.lifecycle.concurrency_conflict");
    public static Error Duplicate => Error.Conflict("supplier.lifecycle.duplicate");
    public static Error InvalidTransition => Error.BusinessRule("supplier.lifecycle.invalid_transition");
}

public static class SupplierLifecycleRules
{
    public static string? Text(string? value, int max, bool optional = true) { string v = value?.Trim() ?? ""; return v.Length == 0 && optional ? null : v.Length <= max ? v : null; }
    public static string? Code(string? value, int max = 40) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length >= 1 && v.Length <= max && v.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.') ? v : null; }
}

public sealed class SupplierEvaluation
{
    private SupplierEvaluation() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid SupplierId { get; private set; }
    public DateOnly EvaluationDate { get; private set; } public DateOnly? PeriodStart { get; private set; } public DateOnly? PeriodEnd { get; private set; }
    public Guid EvaluatorUserId { get; private set; } public decimal? OverallScore { get; private set; } public string ResultStatus { get; private set; } = "Pending";
    public string? Notes { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<SupplierEvaluation> Create(Guid company, Guid supplier, DateOnly date, DateOnly? start, DateOnly? end, Guid evaluator, string? notes, DateTimeOffset now)
    { string? n = SupplierLifecycleRules.Text(notes, 2000); if (company == Guid.Empty || supplier == Guid.Empty || evaluator == Guid.Empty || date == default || start > end || (!string.IsNullOrWhiteSpace(notes) && n is null)) return Result.Failure<SupplierEvaluation>(SupplierLifecycleErrors.InvalidData); return Result.Success(new SupplierEvaluation { Id = Guid.NewGuid(), CompanyId = company, SupplierId = supplier, EvaluationDate = date, PeriodStart = start, PeriodEnd = end, EvaluatorUserId = evaluator, Notes = n, CreatedAtUtc = now, Version = 1 }); }
    public Result Update(decimal? score, string status, string? notes, long expected) { if (expected != Version || score is < 0m or > 100m || status is not ("Pending" or "Approved" or "Rejected")) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); string? n = SupplierLifecycleRules.Text(notes, 2000); if (!string.IsNullOrWhiteSpace(notes) && n is null) return Result.Failure(SupplierLifecycleErrors.InvalidData); OverallScore = score; ResultStatus = status; Notes = n; Version++; return Result.Success(); }
    public Result UpdateDates(DateOnly date, DateOnly? start, DateOnly? end, long expected) { if (expected != Version || date == default || start > end) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); EvaluationDate = date; PeriodStart = start; PeriodEnd = end; Version++; return Result.Success(); }
}

public sealed class SupplierEvaluationCriterion
{
    private SupplierEvaluationCriterion() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid EvaluationId { get; private set; } public Guid CriterionId { get; private set; }
    public decimal Score { get; private set; } public string? Comment { get; private set; }
    public static Result<SupplierEvaluationCriterion> Create(Guid company, Guid evaluation, Guid criterion, decimal score, string? comment) { string? c = SupplierLifecycleRules.Text(comment, 1000); if (company == Guid.Empty || evaluation == Guid.Empty || criterion == Guid.Empty || score is < 0 or > 100 || !string.IsNullOrWhiteSpace(comment) && c is null) return Result.Failure<SupplierEvaluationCriterion>(SupplierLifecycleErrors.InvalidData); return Result.Success(new SupplierEvaluationCriterion { Id = Guid.NewGuid(), CompanyId = company, EvaluationId = evaluation, CriterionId = criterion, Score = score, Comment = c }); }
}

public sealed class SupplierQualityIncident
{
    private SupplierQualityIncident() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid SupplierId { get; private set; } public string IncidentType { get; private set; } = "Reject";
    public DateOnly IncidentDate { get; private set; } public decimal? Quantity { get; private set; } public string Status { get; private set; } = "Open"; public string? Description { get; private set; } public string? CorrectiveAction { get; private set; } public DateOnly? ResolvedDate { get; private set; } public Guid RecordedByUserId { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<SupplierQualityIncident> Create(Guid company, Guid supplier, string type, DateOnly date, decimal? quantity, string? description, Guid actor, DateTimeOffset now) { string? t = SupplierLifecycleRules.Code(type), d = SupplierLifecycleRules.Text(description, 2000); if (company == Guid.Empty || supplier == Guid.Empty || actor == Guid.Empty || t is null || date == default || quantity is <= 0 || !string.IsNullOrWhiteSpace(description) && d is null) return Result.Failure<SupplierQualityIncident>(SupplierLifecycleErrors.InvalidData); return Result.Success(new SupplierQualityIncident { Id = Guid.NewGuid(), CompanyId = company, SupplierId = supplier, IncidentType = t, IncidentDate = date, Quantity = quantity, Description = d, RecordedByUserId = actor, CreatedAtUtc = now, Version = 1 }); }
    public Result Resolve(string? action, DateOnly? date, long expected) { if (expected != Version) return Result.Failure(SupplierLifecycleErrors.Concurrency); string? a = SupplierLifecycleRules.Text(action, 2000); if (date is null || !string.IsNullOrWhiteSpace(action) && a is null) return Result.Failure(SupplierLifecycleErrors.InvalidData); Status = "Resolved"; CorrectiveAction = a; ResolvedDate = date; Version++; return Result.Success(); }
    public Result Update(string type, DateOnly date, decimal? quantity, string? description, long expected) { if (expected != Version) return Result.Failure(SupplierLifecycleErrors.Concurrency); string? t = SupplierLifecycleRules.Code(type), d = SupplierLifecycleRules.Text(description, 2000); if (t is null || date == default || quantity is <= 0 || !string.IsNullOrWhiteSpace(description) && d is null || Status == "Resolved") return Result.Failure(SupplierLifecycleErrors.InvalidData); IncidentType = t; IncidentDate = date; Quantity = quantity; Description = d; Version++; return Result.Success(); }
    public Result SetActive(bool active, long expected) { if (expected != Version || Status == "Resolved") return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidTransition); Status = active ? "Open" : "Inactive"; Version++; return Result.Success(); }
}

public sealed class SupplierLifecycleProfile
{
    private SupplierLifecycleProfile() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid SupplierId { get; private set; } public string ApprovalStatus { get; private set; } = "Pending"; public decimal? QualityScore { get; private set; } public Guid? QcLevelId { get; private set; } public string? HoldReason { get; private set; } public DateTimeOffset? HeldAtUtc { get; private set; } public Guid? HeldByUserId { get; private set; } public string? HoldNotes { get; private set; } public long Version { get; private set; }
    public static SupplierLifecycleProfile Create(Guid company, Guid supplier) => new() { Id = Guid.NewGuid(), CompanyId = company, SupplierId = supplier, Version = 1 };
    public Result SetApproval(string status, long expected) { if (expected != Version || status is not ("Approved" or "Pending" or "NotApproved" or "Blocked")) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); ApprovalStatus = status; Version++; return Result.Success(); }
    public Result SetHold(bool held, string? reason, string? notes, Guid actor, DateTimeOffset now, long expected) { if (expected != Version || actor == Guid.Empty) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); string? r = SupplierLifecycleRules.Text(reason, 500), n = SupplierLifecycleRules.Text(notes, 2000); if (held && r is null || !string.IsNullOrWhiteSpace(notes) && n is null) return Result.Failure(SupplierLifecycleErrors.InvalidData); HoldReason = held ? r : null; HoldNotes = held ? n : null; HeldAtUtc = held ? now : null; HeldByUserId = held ? actor : null; if (held) ApprovalStatus = "Blocked"; else if (ApprovalStatus == "Blocked") ApprovalStatus = "Pending"; Version++; return Result.Success(); }
    public void SetQuality(decimal? score, Guid? qcLevel) { QualityScore = score; QcLevelId = qcLevel; Version++; }
}

public sealed class SupplierContract
{
    private SupplierContract() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid SupplierId { get; private set; } public string ContractNumber { get; private set; } = ""; public Guid ContractTypeId { get; private set; } public DateOnly StartDate { get; private set; } public DateOnly? EndDate { get; private set; } public string Status { get; private set; } = "Draft"; public string? Description { get; private set; } public decimal? ContractValue { get; private set; } public string CurrencyCode { get; private set; } = "IRR"; public Guid? FileId { get; private set; } public Guid? FileVersionId { get; private set; } public long Version { get; private set; }
    public static Result<SupplierContract> Create(Guid company, Guid supplier, string number, Guid type, DateOnly start, DateOnly? end, string? description, decimal? value, string currency) { string? n = SupplierLifecycleRules.Code(number, 80), d = SupplierLifecycleRules.Text(description, 2000), c = SupplierLifecycleRules.Code(currency, 3); if (company == Guid.Empty || supplier == Guid.Empty || n is null || type == Guid.Empty || start == default || start > end || value is < 0 || c is null || !string.IsNullOrWhiteSpace(description) && d is null) return Result.Failure<SupplierContract>(SupplierLifecycleErrors.InvalidData); return Result.Success(new SupplierContract { Id = Guid.NewGuid(), CompanyId = company, SupplierId = supplier, ContractNumber = n, ContractTypeId = type, StartDate = start, EndDate = end, Description = d, ContractValue = value, CurrencyCode = c, Version = 1 }); }
    public Result Update(DateOnly? end, string status, string? description, decimal? value, long expected) { if (expected != Version || status is not ("Draft" or "Active" or "Expired" or "Cancelled") || value is < 0m) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); string? d = SupplierLifecycleRules.Text(description, 2000); if (!string.IsNullOrWhiteSpace(description) && d is null) return Result.Failure(SupplierLifecycleErrors.InvalidData); EndDate = end; Status = status; Description = d; ContractValue = value; Version++; return Result.Success(); }
    public Result Attach(Guid file, Guid? version, long expected) { if (expected != Version || file == Guid.Empty) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); FileId = file; FileVersionId = version; Version++; return Result.Success(); }
}

public sealed class SupplierDocument
{
    private SupplierDocument() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid SupplierId { get; private set; } public Guid? ContractId { get; private set; } public Guid DocumentTypeId { get; private set; } public string? DocumentNumber { get; private set; } public DateOnly? IssueDate { get; private set; } public DateOnly? ExpiryDate { get; private set; } public string Status { get; private set; } = "Active"; public string? Description { get; private set; } public string? IssuingAuthority { get; private set; } public Guid? FileId { get; private set; } public Guid? FileVersionId { get; private set; } public long Version { get; private set; }
    public static Result<SupplierDocument> Create(Guid company, Guid supplier, Guid type, Guid? contract, string? number, DateOnly? issue, DateOnly? expiry, string? description, string? authority) { string? n = SupplierLifecycleRules.Text(number, 100, true), d = SupplierLifecycleRules.Text(description, 2000), a = SupplierLifecycleRules.Text(authority, 300); if (company == Guid.Empty || supplier == Guid.Empty || type == Guid.Empty || expiry < issue || !string.IsNullOrWhiteSpace(description) && d is null || !string.IsNullOrWhiteSpace(authority) && a is null) return Result.Failure<SupplierDocument>(SupplierLifecycleErrors.InvalidData); return Result.Success(new SupplierDocument { Id = Guid.NewGuid(), CompanyId = company, SupplierId = supplier, ContractId = contract, DocumentTypeId = type, DocumentNumber = n, IssueDate = issue, ExpiryDate = expiry, Description = d, IssuingAuthority = a, Version = 1 }); }
    public Result Attach(Guid file, Guid? version, long expected) { if (expected != Version || file == Guid.Empty) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); FileId = file; FileVersionId = version; Version++; return Result.Success(); }
    public Result SetStatus(string status, long expected) { if (expected != Version || status is not ("Active" or "ExpiringSoon" or "Expired" or "Revoked")) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); Status = status; Version++; return Result.Success(); }
    public Result Update(Guid type, Guid? contract, string? number, DateOnly? issue, DateOnly? expiry, string? description, string? authority, long expected) { if (expected != Version || type == Guid.Empty || expiry < issue) return Result.Failure(expected != Version ? SupplierLifecycleErrors.Concurrency : SupplierLifecycleErrors.InvalidData); string? n = SupplierLifecycleRules.Text(number, 100), d = SupplierLifecycleRules.Text(description, 2000), a = SupplierLifecycleRules.Text(authority, 300); if (!string.IsNullOrWhiteSpace(description) && d is null || !string.IsNullOrWhiteSpace(authority) && a is null) return Result.Failure(SupplierLifecycleErrors.InvalidData); DocumentTypeId = type; ContractId = contract; DocumentNumber = n; IssueDate = issue; ExpiryDate = expiry; Description = d; IssuingAuthority = a; Version++; return Result.Success(); }
}
