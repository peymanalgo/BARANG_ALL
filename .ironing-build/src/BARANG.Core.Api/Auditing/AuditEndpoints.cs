using BARANG.Core.Application.Auditing;
using BARANG.Core.Domain.Security;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Application.Pagination;

namespace BARANG.Core.Api.Auditing;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder audit = endpoints.MapGroup("/api/v1/audit/events")
            .RequireAuthorization(SecurityPermissions.AuditRead);
        audit.MapGet("/", async ([AsParameters] AuditQueryRequest request, IAuditReader reader, ITransactionExecutor transactions, CancellationToken ct) =>
        {
            if (request.PageNumber < 1 || request.PageSize is < 1 or > 100 || request.FromUtc > request.ToUtc)
                return Results.BadRequest();
            Result<PagedResult<AuditEventView>> result = await transactions.ExecuteAsync(
                async token => Result.Success(await reader.ListAsync(request.ToQuery(), token)), ct);
            return Results.Ok(result.Value);
        });
        audit.MapGet("/{id:guid}", async (Guid id, IAuditReader reader, ITransactionExecutor transactions, CancellationToken ct) =>
        {
            Result<AuditEventView?> result = await transactions.ExecuteAsync(
                async token => Result.Success(await reader.FindAsync(id, token)), ct);
            return result.Value is { } value ? Results.Ok(value) : Results.NotFound();
        });
        return endpoints;
    }
}

public sealed record AuditQueryRequest(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc, Guid? ActorUserId,
    string? Action, string? EntityType, string? EntityId,
    string? CorrelationId, AuditOutcome? Outcome,
    int PageNumber = 1, int PageSize = 20)
{
    public AuditQuery ToQuery() => new(FromUtc, ToUtc, ActorUserId, Action,
        EntityType, EntityId, CorrelationId, Outcome, PageNumber, PageSize);
}
