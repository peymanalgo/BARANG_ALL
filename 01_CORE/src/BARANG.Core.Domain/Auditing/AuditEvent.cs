namespace BARANG.Core.Domain.Auditing;

public sealed class AuditEvent
{
    private AuditEvent() { }

    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorUsername { get; private set; }
    public string ActorType { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? RequestId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Outcome { get; private set; } = string.Empty;
    public string? FailureCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string MetadataJson { get; private set; } = "{}";
    public string ChangesJson { get; private set; } = "{}";

    public static AuditEvent Create(
        DateTimeOffset occurredAtUtc, Guid? companyId, Guid? actorUserId,
        string? actorUsername, string actorType, string action,
        string? entityType, string? entityId, string correlationId,
        string? requestId, string source, string outcome, string? failureCode,
        string? ipAddress, string? userAgent, string metadataJson, string changesJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        return new AuditEvent
        {
            Id = Guid.NewGuid(), OccurredAtUtc = occurredAtUtc.ToUniversalTime(),
            CompanyId = companyId, ActorUserId = actorUserId,
            ActorUsername = actorUsername, ActorType = actorType, Action = action,
            EntityType = entityType, EntityId = entityId,
            CorrelationId = correlationId, RequestId = requestId, Source = source,
            Outcome = outcome, FailureCode = failureCode, IpAddress = ipAddress,
            UserAgent = userAgent, MetadataJson = metadataJson, ChangesJson = changesJson
        };
    }
}
