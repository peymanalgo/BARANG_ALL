using BARANG.Core.Application.Pagination;
using System.Text.Json.Serialization;

namespace BARANG.Core.Application.Auditing;

public static class AuditActions
{
    public const string LoginSucceeded = "authentication.login.succeeded";
    public const string LoginFailed = "authentication.login.failed";
    public const string Logout = "authentication.logout";
    public const string SessionRevoked = "authentication.session.revoked";
    public const string AuthorizationDenied = "authorization.denied";
    public const string CompanySelected = "company.context.selected";
    public const string UserCreated = "security.user.created";
    public const string UserUpdated = "security.user.updated";
    public const string UserActivated = "security.user.activated";
    public const string UserDeactivated = "security.user.deactivated";
    public const string UserPasswordChanged = "security.user.password_changed";
    public const string RoleCreated = "security.role.created";
    public const string RoleUpdated = "security.role.updated";
    public const string RoleActivated = "security.role.activated";
    public const string RoleDeactivated = "security.role.deactivated";
    public const string UserRoleAssigned = "security.user_role.assigned";
    public const string UserRoleRevoked = "security.user_role.revoked";
    public const string UserCompanyGranted = "security.user_company.granted";
    public const string UserCompanyRevoked = "security.user_company.revoked";
    public const string PermissionAssigned = "security.permission.assigned";
    public const string PermissionRevoked = "security.permission.revoked";
    public const string BootstrapCompleted = "security.bootstrap.completed";
    public const string ConfigurationChanged = "system.configuration.changed";
    public const string OperationFailed = "system.operation.failed";
}

[JsonConverter(typeof(JsonStringEnumConverter<AuditOutcome>))]
public enum AuditOutcome { Success, Failure }
public enum AuditChangeKind { Added, Modified, Archived }

public sealed record AuditValueChange(object? Old, object? New);

public sealed record AuditWriteRequest(
    string Action,
    string? EntityType = null,
    string? EntityId = null,
    AuditOutcome Outcome = AuditOutcome.Success,
    string? FailureCode = null,
    Guid? CompanyId = null,
    string? ActorUsername = null,
    AuditChangeKind? ChangeKind = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    IReadOnlyDictionary<string, AuditValueChange>? Changes = null,
    Guid? ActorUserId = null,
    string? ActorType = null);

public interface IAuditWriter { void Write(AuditWriteRequest request); }

public interface IAuditRequestContext
{
    string? RequestId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public sealed record AuditQuery(
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null,
    Guid? ActorUserId = null, string? Action = null,
    string? EntityType = null, string? EntityId = null,
    string? CorrelationId = null, AuditOutcome? Outcome = null,
    int PageNumber = 1, int PageSize = PagedRequest.DefaultPageSize);

public sealed record AuditEventView(
    Guid Id, DateTimeOffset OccurredAtUtc, Guid? CompanyId,
    Guid? ActorUserId, string? ActorUsername, string ActorType,
    string Action, string? EntityType, string? EntityId,
    string CorrelationId, string? RequestId, string Source,
    AuditOutcome Outcome, string? FailureCode,
    IReadOnlyDictionary<string, object?> Metadata,
    IReadOnlyDictionary<string, object?> Changes);

public interface IAuditReader
{
    Task<PagedResult<AuditEventView>> ListAsync(AuditQuery query, CancellationToken ct);
    Task<AuditEventView?> FindAsync(Guid id, CancellationToken ct);
}
