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
    public const string UserPasswordReset = "security.user.password_reset";
    public const string UserUnlocked = "security.user.unlocked";
    public const string UserDirectPermissionsChanged = "security.user_direct_permissions.changed";
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
    public const string RolePermissionsChanged = "security.role_permissions.changed";
    public const string RoleDeleted = "security.role.deleted";
    public const string BootstrapCompleted = "security.bootstrap.completed";
    public const string ConfigurationChanged = "system.configuration.changed";
    public const string OperationFailed = "system.operation.failed";
    public const string LookupTypeCreated = "lookup.type.created";
    public const string LookupTypeUpdated = "lookup.type.updated";
    public const string LookupTypeActivated = "lookup.type.activated";
    public const string LookupTypeDeactivated = "lookup.type.deactivated";
    public const string LookupValueCreated = "lookup.value.created";
    public const string LookupValueUpdated = "lookup.value.updated";
    public const string LookupValueActivated = "lookup.value.activated";
    public const string LookupValueDeactivated = "lookup.value.deactivated";
    public const string LookupTranslationUpdated = "lookup.translation.updated";
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerActivated = "customer.activated";
    public const string CustomerDeactivated = "customer.deactivated";
    public const string CustomerContactCreated = "customer.contact.created";
    public const string CustomerContactUpdated = "customer.contact.updated";
    public const string CustomerContactActivated = "customer.contact.activated";
    public const string CustomerContactDeactivated = "customer.contact.deactivated";
    public const string CustomerAddressCreated = "customer.address.created";
    public const string CustomerAddressUpdated = "customer.address.updated";
    public const string CustomerAddressActivated = "customer.address.activated";
    public const string CustomerAddressDeactivated = "customer.address.deactivated";
    public const string ProductCreated = "product.created";
    public const string ProductUpdated = "product.updated";
    public const string ProductActivated = "product.activated";
    public const string ProductDeactivated = "product.deactivated";
    public const string ProductVariantCreated = "product.variant.created";
    public const string ProductVariantUpdated = "product.variant.updated";
    public const string ProductVariantActivated = "product.variant.activated";
    public const string ProductVariantDeactivated = "product.variant.deactivated";
    public const string SupplierCreated = "supplier.created";
    public const string SupplierUpdated = "supplier.updated";
    public const string SupplierActivated = "supplier.activated";
    public const string SupplierDeactivated = "supplier.deactivated";
    public const string SupplierProductCreated = "supplier.product.created";
    public const string SupplierProductUpdated = "supplier.product.updated";
    public const string SupplierProductActivated = "supplier.product.activated";
    public const string SupplierProductDeactivated = "supplier.product.deactivated";
    public const string EquipmentCreated = "equipment.created";
    public const string EquipmentUpdated = "equipment.updated";
    public const string EquipmentActivated = "equipment.activated";
    public const string EquipmentDeactivated = "equipment.deactivated";
    public const string EquipmentStatusChanged = "equipment.status.changed";
}

[JsonConverter(typeof(JsonStringEnumConverter<AuditOutcome>))]
public enum AuditOutcome { Success, Failure }
public enum AuditChangeKind { Added, Modified, Archived, Deleted, Removed = Deleted }

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
