using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Quality;

/// <summary>Versioned, mutually exclusive quantities recorded for one inspection.</summary>
public sealed class QCInspectionDisposition
{
    private QCInspectionDisposition() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid InspectionId { get; private set; }
    public decimal InspectableQuantity { get; private set; }
    public string UomCode { get; private set; } = "";
    public decimal AcceptedQuantity { get; private set; }
    public decimal RejectedQuantity { get; private set; }
    public decimal ReproductionRequiredQuantity { get; private set; }
    public string AcceptedReason { get; private set; } = "";
    public string? RejectedReason { get; private set; }
    public string? ReproductionReason { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Result<QCInspectionDisposition> Create(Guid company, Guid inspection, decimal inspectable,
        string uomCode, decimal accepted, decimal rejected, decimal reproduction, string acceptedReason,
        string? rejectedReason, string? reproductionReason, Guid actor, DateTimeOffset now)
    {
        bool pending = accepted == 0 && rejected == 0 && reproduction == 0 && string.IsNullOrWhiteSpace(acceptedReason);
        if (company == Guid.Empty || inspection == Guid.Empty || actor == Guid.Empty || inspectable <= 0 ||
            accepted < 0 || rejected < 0 || reproduction < 0 || (!pending && accepted + rejected + reproduction != inspectable) ||
            string.IsNullOrWhiteSpace(uomCode) || (!pending && string.IsNullOrWhiteSpace(acceptedReason)) ||
            (rejected > 0 && string.IsNullOrWhiteSpace(rejectedReason)) ||
            (reproduction > 0 && string.IsNullOrWhiteSpace(reproductionReason)))
            return Result.Failure<QCInspectionDisposition>(Error.Validation("qc.disposition_invalid"));

        return Result.Success(new QCInspectionDisposition
        {
            Id = Guid.NewGuid(), CompanyId = company, InspectionId = inspection,
            InspectableQuantity = inspectable, UomCode = uomCode.Trim().ToUpperInvariant(),
            AcceptedQuantity = accepted, RejectedQuantity = rejected,
            ReproductionRequiredQuantity = reproduction, AcceptedReason = acceptedReason?.Trim() ?? "",
            RejectedReason = rejectedReason?.Trim(), ReproductionReason = reproductionReason?.Trim(),
            RecordedByUserId = actor, RecordedAtUtc = now, Version = 1
        });
    }

    public Result Replace(decimal accepted, decimal rejected, decimal reproduction, string acceptedReason,
        string? rejectedReason, string? reproductionReason, long expectedVersion, Guid actor, DateTimeOffset now)
    {
        if (expectedVersion != Version)
            return Result.Failure(Error.Concurrency("qc.concurrency_conflict"));
        if (actor == Guid.Empty || accepted < 0 || rejected < 0 || reproduction < 0 ||
            accepted + rejected + reproduction != InspectableQuantity || string.IsNullOrWhiteSpace(acceptedReason) ||
            (rejected > 0 && string.IsNullOrWhiteSpace(rejectedReason)) ||
            (reproduction > 0 && string.IsNullOrWhiteSpace(reproductionReason)))
            return Result.Failure(Error.Validation("qc.disposition_invalid"));

        AcceptedQuantity = accepted; RejectedQuantity = rejected; ReproductionRequiredQuantity = reproduction;
        AcceptedReason = acceptedReason.Trim(); RejectedReason = rejectedReason?.Trim();
        ReproductionReason = reproductionReason?.Trim(); RecordedByUserId = actor; RecordedAtUtc = now; Version++;
        return Result.Success();
    }
}
