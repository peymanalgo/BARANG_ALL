using System.Text.Json;
using System.Text.Json.Nodes;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Auditing;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Auditing;

public sealed class AuditWriter(
    CoreDbContext db, ICurrentActor actor, ICompanyContext company,
    ICorrelationContext correlation, IAuditRequestContext requestContext,
    TimeProvider time) : IAuditWriter
{
    public void Write(AuditWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid? actorUserId = request.ActorUserId ?? (Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : null);
        string metadata = AuditRedactor.Serialize(request.Metadata);
        Dictionary<string, object?> changes = [];
        if (request.ChangeKind is not null) changes["kind"] = request.ChangeKind.ToString();
        if (request.Changes is not null)
            foreach ((string key, AuditValueChange value) in request.Changes)
                changes[key] = new Dictionary<string, object?> { ["old"] = value.Old, ["new"] = value.New };

        db.AuditEvents.Add(AuditEvent.Create(
            time.GetUtcNow(), request.CompanyId ?? company.CompanyId, actorUserId,
            Bound(request.ActorUsername ?? actor.Actor.ActorUsername, 100), request.ActorType ?? actor.Actor.ActorType.ToString(),
            request.Action, request.EntityType, request.EntityId,
            correlation.CorrelationId ?? "system", requestContext.RequestId,
            actor.Actor.SourceChannel.ToString(), request.Outcome.ToString(),
            request.FailureCode, requestContext.IpAddress, requestContext.UserAgent,
            metadata, AuditRedactor.Serialize(changes)));
    }

    private static string? Bound(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maximumLength)];
}

public static class AuditRedactor
{
    private static readonly string[] SensitiveFragments =
        ["password", "passwordhash", "token", "accesstoken", "refreshtoken", "authorization", "secret", "apikey"];
    private const string Redacted = "[REDACTED]";

    public static string Serialize(IReadOnlyDictionary<string, object?>? values)
    {
        JsonObject root = [];
        if (values is not null)
            foreach ((string key, object? value) in values)
                root[key] = IsSensitive(key) ? Redacted : Sanitize(JsonSerializer.SerializeToNode(value));
        return root.ToJsonString();
    }

    private static JsonNode? Sanitize(JsonNode? node)
    {
        if (node is JsonObject value)
            foreach (string key in value.Select(x => x.Key).ToArray())
                value[key] = IsSensitive(key) ? Redacted : Sanitize(value[key]);
        else if (node is JsonArray array)
            for (int index = 0; index < array.Count; index++)
            {
                JsonNode? sanitized = Sanitize(array[index]);
                array[index] = sanitized?.DeepClone();
            }
        return node;
    }

    private static bool IsSensitive(string key)
    {
        string normalized = new(key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return SensitiveFragments.Any(normalized.Contains);
    }
}

public sealed class AuditReader(CoreDbContext db, ICompanyContext company) : IAuditReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<AuditEventView>> ListAsync(AuditQuery query, CancellationToken ct)
    {
        PagedRequest page = new(query.PageNumber, query.PageSize);
        // Keep the tenant predicate in the application query as well as PostgreSQL RLS.
        // This prevents a future privileged DbContext/query-filter change from widening
        // the audit read surface accidentally.
        Guid companyId = company.RequireCompanyId();
        IQueryable<AuditEvent> events = db.AuditEvents.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (query.FromUtc is not null) events = events.Where(x => x.OccurredAtUtc >= query.FromUtc.Value);
        // The upper bound is exclusive, so adjacent time windows cannot double-count an event.
        if (query.ToUtc is not null) events = events.Where(x => x.OccurredAtUtc < query.ToUtc.Value);
        if (query.ActorUserId is not null) events = events.Where(x => x.ActorUserId == query.ActorUserId);
        if (query.Action is not null) events = events.Where(x => x.Action == query.Action);
        if (query.EntityType is not null) events = events.Where(x => x.EntityType == query.EntityType);
        if (query.EntityId is not null) events = events.Where(x => x.EntityId == query.EntityId);
        if (query.CorrelationId is not null) events = events.Where(x => x.CorrelationId == query.CorrelationId);
        if (query.Outcome is not null) events = events.Where(x => x.Outcome == query.Outcome.ToString());
        long total = await events.LongCountAsync(ct);
        List<AuditEvent> items = await events.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct);
        return new PagedResult<AuditEventView>(items.Select(View), page.PageNumber, page.PageSize, total);
    }

    public async Task<AuditEventView?> FindAsync(Guid id, CancellationToken ct)
    {
        Guid companyId = company.RequireCompanyId();
        AuditEvent? value = await db.AuditEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
        return value is null ? null : View(value);
    }

    private static AuditEventView View(AuditEvent value) => new(
        value.Id, value.OccurredAtUtc, value.CompanyId, value.ActorUserId,
        value.ActorUsername, value.ActorType, value.Action, value.EntityType,
        value.EntityId, value.CorrelationId, value.RequestId, value.Source,
        Enum.Parse<AuditOutcome>(value.Outcome), value.FailureCode,
        Deserialize(value.MetadataJson), Deserialize(value.ChangesJson));

    private static Dictionary<string, object?> Deserialize(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions) ?? [];
}

public sealed class NullAuditRequestContext : IAuditRequestContext
{
    public string? RequestId => null;
    public string? IpAddress => null;
    public string? UserAgent => null;
}
