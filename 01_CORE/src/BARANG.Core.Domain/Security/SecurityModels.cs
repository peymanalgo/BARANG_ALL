using BARANG.Core.Domain.Common;
using System.Text.Json.Serialization;
#pragma warning disable CA1711 // Permission and RolePermission are the exact RBAC domain concepts.

namespace BARANG.Core.Domain.Security;

public static class SecurityErrors
{
    public static Error InvalidIdentity => Error.Validation("security.identity.invalid");
    public static Error InvalidPeriod => Error.Validation("security.assignment.invalid_period");
    public static Error InvalidPassword => Error.Validation("security.password.invalid");
    public static Error InvalidCredentials => Error.Authentication("security.login.invalid_credentials");
    public static Error AccessDenied => Error.Authorization("security.access.denied");
    public static Error UserContextUnavailable => Error.Authentication("security.user_context.unavailable");
    public static Error CompanyNotAuthorized => Error.Authorization("security.company_access.denied");
    public static Error CompanyInactive => Error.BusinessRule("security.company.inactive");
    public static Error NotFound => Error.NotFound("security.resource.not_found");
    public static Error Conflict => Error.Conflict("security.resource.conflict");
    public static Error DuplicatePersonAccount => Error.Conflict("security.user.person_already_linked");
    public static Error CriticalAdministrator => Error.BusinessRule("security.critical_administrator.protected");
    public static Error SystemRoleProtected => Error.BusinessRule("security.system_role.protected");
    public static Error PrivilegeEscalation => Error.Authorization("security.permission.delegation_denied");
    public static Error AccountLocked => Error.Authentication("security.login.account_locked");
}

public sealed class ApplicationUser
{
    private ApplicationUser() { }
    public Guid Id { get; private set; }
    public Guid? PersonId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public UserAccountState AccountState { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string SecurityStamp { get; private set; } = string.Empty;
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public bool IsActive => AccountState == UserAccountState.Active;
    public static Result<ApplicationUser> CreateIdentity(Guid id, Guid? personId, string username, string displayName, string? email, DateTimeOffset now)
    {
        string normalized = username.Trim().ToUpperInvariant();
        string name = displayName.Trim();
        if (id == Guid.Empty || personId is null || personId == Guid.Empty || normalized.Length is < 3 or > 100 || !normalized.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') || name.Length is < 1 or > 200) return Result.Failure<ApplicationUser>(SecurityErrors.InvalidIdentity);
        string? normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        return Result.Success(new ApplicationUser { Id = id, PersonId = personId, Username = normalized, DisplayName = name, Email = normalizedEmail, AccountState = UserAccountState.Active, SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public static Result<ApplicationUser> Create(Guid id, string username, string displayName, string? email, string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return Result.Failure<ApplicationUser>(SecurityErrors.InvalidIdentity);
        string normalized = username.Trim().ToUpperInvariant();
        string name = displayName.Trim();
        if (id == Guid.Empty || normalized.Length is < 3 or > 100 || !normalized.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') || name.Length is < 1 or > 200) return Result.Failure<ApplicationUser>(SecurityErrors.InvalidIdentity);
        return Result.Success(new ApplicationUser { Id = id, Username = normalized, DisplayName = name, Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(), AccountState = UserAccountState.Active, PasswordHash = passwordHash, SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result UpdateProfile(string displayName, string? email, DateTimeOffset now) { string name = displayName.Trim(); if (name.Length is < 1 or > 200) return Result.Failure(SecurityErrors.InvalidIdentity); DisplayName = name; Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(); Touch(now); return Result.Success(); }
    public Result SetUsername(string username, DateTimeOffset now) { string normalized = username.Trim().ToUpperInvariant(); if (normalized.Length is < 3 or > 100 || !normalized.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')) return Result.Failure(SecurityErrors.InvalidIdentity); if (Username != normalized) { Username = normalized; Touch(now); } return Result.Success(); }
    public void SetActive(bool active, DateTimeOffset now) { UserAccountState next = active ? UserAccountState.Active : UserAccountState.Inactive; if (AccountState != next || !active && LockoutEndUtc is not null) { AccountState = next; if (!active) { LockoutEndUtc = null; AccessFailedCount = 0; } Touch(now); } }
    public void SetPasswordHash(string hash, DateTimeOffset now) { ArgumentException.ThrowIfNullOrWhiteSpace(hash); PasswordHash = hash; Touch(now); }
    public void SetSecurityStamp(string stamp, DateTimeOffset now) { ArgumentException.ThrowIfNullOrWhiteSpace(stamp); SecurityStamp = stamp; Touch(now); }
    public void SetLockoutEnd(DateTimeOffset? lockoutEndUtc, DateTimeOffset now) { LockoutEndUtc = lockoutEndUtc; if (lockoutEndUtc is { } end && end > now) AccountState = UserAccountState.Locked; else if (AccountState == UserAccountState.Locked) AccountState = UserAccountState.Active; Touch(now); }
    public int IncrementAccessFailed(DateTimeOffset now) { AccessFailedCount++; Touch(now); return AccessFailedCount; }
    public void ResetAccessFailed(DateTimeOffset now) { if (AccessFailedCount != 0) { AccessFailedCount = 0; Touch(now); } }
    public void RecordLogin(DateTimeOffset now) { LastLoginAtUtc = now; Touch(now); }
    public void TouchSecurity(DateTimeOffset now) => Touch(now);
    public Result LinkPerson(Guid personId, DateTimeOffset now) { if (personId == Guid.Empty || PersonId is not null && PersonId != personId) return Result.Failure(SecurityErrors.Conflict); PersonId = personId; Touch(now); return Result.Success(); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}
[JsonConverter(typeof(JsonStringEnumConverter<UserAccountState>))]
public enum UserAccountState { Active, Inactive, Locked }

[JsonConverter(typeof(JsonStringEnumConverter<RoleScope>))]
public enum RoleScope { Company, System }

public sealed class Role
{
    private Role() { }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayTitle { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public RoleScope Scope { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<Role> Create(Guid id, string code, string title, bool isSystem, DateTimeOffset now) => Create(id, code, title, null, isSystem ? RoleScope.System : RoleScope.Company, isSystem, now);
    public static Result<Role> Create(Guid id, string code, string title, string? description, RoleScope scope, bool isSystem, DateTimeOffset now) { string normalized = code.Trim().ToLowerInvariant(); string display = title.Trim(); string? details = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); if (id == Guid.Empty || normalized.Length is < 3 or > 100 || !PermissionCode.IsValid(normalized) || display.Length is < 1 or > 200 || details?.Length > 1000 || isSystem && scope != RoleScope.System) return Result.Failure<Role>(SecurityErrors.InvalidIdentity); return Result.Success(new Role { Id = id, Code = normalized, DisplayTitle = display, Description = details, Scope = scope, IsSystem = isSystem, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); }
    public Result Update(string title, string? description, DateTimeOffset now) { string value = title.Trim(); string? details = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); if (value.Length is < 1 or > 200 || details?.Length > 1000) return Result.Failure(SecurityErrors.InvalidIdentity); DisplayTitle = value; Description = details; Touch(now); return Result.Success(); }
    public Result UpdateTitle(string title, DateTimeOffset now) => Update(title, Description, now);
    public void SetActive(bool active, DateTimeOffset now) { if (IsActive != active) { IsActive = active; Touch(now); } }
    public void TouchPermissions(DateTimeOffset now) => Touch(now);
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}

public sealed class Permission
{
    private Permission() { }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TechnicalDescription { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public static Permission Create(Guid id, string code, string description, string module) { if (id == Guid.Empty || !PermissionCode.IsValid(code)) throw new ArgumentException("Invalid permission.", nameof(code)); return new Permission { Id = id, Code = code, TechnicalDescription = description, Module = module }; }
}
public static class PermissionCode
{
    public static bool IsValid(string value) => value.Length is >= 3 and <= 150 && value[0] is >= 'a' and <= 'z' && value.All(static c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.');
}
public static class SecurityPermissions
{
    public const string OrganizationCompanyManage = "organization.company.manage";
    public const string OrganizationBranchManage = "organization.branch.manage";
    public const string OrganizationSettingsManage = "organization.settings.manage";
    public const string OrganizationUnitManage = "organization.unit.manage";
    public const string OrganizationWarehouseManage = "organization.warehouse.manage";
    public const string SecurityUserManage = "security.user.manage";
    public const string SecurityRoleManage = "security.role.manage";
    public const string SecurityUserRead = "security.user.read";
    public const string SecurityUserCreate = "security.user.create";
    public const string SecurityUserEdit = "security.user.edit";
    public const string SecurityUserPasswordReset = "security.user.password.reset";
    public const string SecurityUserStatusManage = "security.user.status.manage";
    public const string SecurityUserRoleManage = "security.user.role.manage";
    public const string SecurityRoleRead = "security.role.read";
    public const string SecurityPermissionRead = "security.permission.read";
    public const string SecurityRolePermissionManage = "security.role.permission.manage";
    public const string SecurityUserPermissionManage = "security.user.permission.manage";
    public const string AuditRead = "audit.read";
    public const string LookupRead = "lookup.read";
    public const string LookupManage = "lookup.manage";
    public const string CustomerRead = "customer.read";
    public const string CustomerManage = "customer.manage";
    public const string CustomerCreate = "customer.create";
    public const string CustomerEdit = "customer.edit";
    public const string CustomerStatusManage = "customer.status.manage";
    public const string CustomerFinancialRead = "customer.financial.read";
    public const string CustomerFinancialManage = "customer.financial.manage";
    public const string ProductRead = "product.read";
    public const string ProductManage = "product.manage";
    public const string SupplierRead = "supplier.read";
    public const string SupplierManage = "supplier.manage";
    public const string EquipmentRead = "equipment.read";
    public const string EquipmentManage = "equipment.manage";
    public const string ArtworkRead = "artwork.read";
    public const string ArtworkManage = "artwork.manage";
    public const string FileRead = "file.read";
    public const string FileManage = "file.manage";
    public const string FileUpload = "file.upload";
    public const string OrderRead="order.read";public const string OrderCreate="order.create";public const string OrderManage="order.manage";public const string OrderPriorityManage="order.priority.manage";public const string OrderSubmit="order.submit";public const string OrderCancel="order.cancel";public const string OrderChangeRequestManage="order.change.manage";
    public const string PricingRead="pricing.read";public const string PricingManage="pricing.manage";public const string PricingDiscountApply="pricing.discount.apply";
    public const string FinanceRead="finance.read";public const string PaymentManage="payment.manage";public const string PaymentReverse="payment.reverse";public const string FinanceApprove="finance.approve";public const string InvoiceManage="invoice.manage";
    public const string InventoryRead="inventory.read";public const string InventoryManage="inventory.manage";public const string InventoryAdjust="inventory.adjust";public const string PurchasingRead="purchasing.read";public const string PurchasingManage="purchasing.manage";
    public const string ProductionRead="production.read";public const string ProductionPlan="production.plan";public const string ProductionAssign="production.assign";public const string ProductionExecute="production.execute";
    public const string QCRead="qc.read";public const string QCInspect="qc.inspect";public const string QCReview="qc.review";public const string QCManage="qc.manage";public const string ReproductionManage="reproduction.manage";
    public const string LogisticsRead="logistics.read";public const string PackagingManage="packaging.manage";public const string ShippingManage="shipping.manage";public const string DeliveryManage="delivery.manage";
    public const string WorkflowRead="workflow.read";public const string WorkflowTaskClaim="workflow.task.claim";public const string WorkflowTaskComplete="workflow.task.complete";public const string WorkflowManage="workflow.manage";
    public const string NotificationRead="notification.read";public const string NotificationManage="notification.manage";public const string BackgroundJobReview="backgroundjob.review";
    public const string ClientCatalogRead="catalog.read";public const string ClientCustomerRead="customer.profile.read";public const string ClientCustomerWrite="customer.profile.write";public const string ClientOrderRead="client.order.read";public const string ClientOrderWrite="client.order.write";public const string ClientFileUpload="client.file.upload";public const string ClientPricingPreview="pricing.preview";public const string ClientTrackingRead="tracking.read";
    public static IReadOnlyList<string> ClientScopes { get; }=[ClientCatalogRead,ClientCustomerRead,ClientCustomerWrite,ClientOrderRead,ClientOrderWrite,ClientFileUpload,ClientPricingPreview,ClientTrackingRead];
    public static IReadOnlyList<string> All { get; } = [OrganizationCompanyManage, OrganizationBranchManage, OrganizationSettingsManage, OrganizationUnitManage, OrganizationWarehouseManage, SecurityUserManage, SecurityRoleManage, AuditRead, LookupRead, LookupManage, CustomerRead, CustomerManage, ProductRead, ProductManage, SupplierRead, SupplierManage, EquipmentRead, EquipmentManage, ArtworkRead,ArtworkManage,FileRead,FileManage,FileUpload,OrderRead,OrderCreate,OrderManage,OrderSubmit,OrderCancel,OrderChangeRequestManage,PricingRead,PricingManage,PricingDiscountApply,FinanceRead,PaymentManage,PaymentReverse,FinanceApprove,InvoiceManage,InventoryRead,InventoryManage,InventoryAdjust,PurchasingRead,PurchasingManage,ProductionRead,ProductionPlan,ProductionAssign,ProductionExecute,QCRead,QCInspect,QCReview,QCManage,ReproductionManage,LogisticsRead,PackagingManage,ShippingManage,DeliveryManage,WorkflowRead,WorkflowTaskClaim,WorkflowTaskComplete,WorkflowManage,NotificationRead,NotificationManage,BackgroundJobReview,ClientCatalogRead,ClientCustomerRead,ClientCustomerWrite,ClientOrderRead,ClientOrderWrite,ClientFileUpload,ClientPricingPreview,ClientTrackingRead,SecurityUserRead,SecurityUserCreate,SecurityUserEdit,SecurityUserPasswordReset,SecurityUserStatusManage,SecurityUserRoleManage,SecurityRoleRead,SecurityPermissionRead,SecurityRolePermissionManage,SecurityUserPermissionManage,CustomerCreate,CustomerEdit,CustomerStatusManage,CustomerFinancialRead,CustomerFinancialManage];
    public static IReadOnlyList<SecurityPermissionDefinition> Catalog { get; } = All.Select(SecurityPermissionDefinition.FromCode).ToArray();
}

[JsonConverter(typeof(JsonStringEnumConverter<PermissionCapability>))]
public enum PermissionCapability { View, Create, Edit, Delete, Special }
public sealed record SecurityPermissionDefinition(string Code, string Module, string FeaturePath, string Action, PermissionCapability Capability)
{
    public static SecurityPermissionDefinition FromCode(string code)
    {
        string[] parts = code.Split('.');
        string action = parts[^1];
        PermissionCapability capability = action switch { "read" or "view" => PermissionCapability.View, "create" => PermissionCapability.Create, "edit" or "update" => PermissionCapability.Edit, "delete" => PermissionCapability.Delete, _ => PermissionCapability.Special };
        string feature = parts.Length > 1 ? string.Join('.', parts[..^1]) : code;
        return new(code, parts[0], feature, action, capability);
    }
}

public sealed class RolePermission { private RolePermission() { } public Guid Id { get; private set; } public Guid RoleId { get; private set; } public Guid PermissionId { get; private set; } public DateTimeOffset AssignedAtUtc { get; private set; } public Guid? AssignedByUserId { get; private set; } public static RolePermission Create(Guid roleId, Guid permissionId, Guid? actor, DateTimeOffset now) => new() { Id = Guid.NewGuid(), RoleId = roleId, PermissionId = permissionId, AssignedByUserId = actor, AssignedAtUtc = now }; }
public sealed class UserPermission { private UserPermission() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public Guid PermissionId { get; private set; } public Guid CompanyId { get; private set; } public DateTimeOffset AssignedAtUtc { get; private set; } public Guid? AssignedByUserId { get; private set; } public static Result<UserPermission> Create(Guid userId, Guid permissionId, Guid companyId, Guid? actor, DateTimeOffset now) => userId == Guid.Empty || permissionId == Guid.Empty || companyId == Guid.Empty ? Result.Failure<UserPermission>(SecurityErrors.InvalidIdentity) : Result.Success(new UserPermission { Id = Guid.NewGuid(), UserId = userId, PermissionId = permissionId, CompanyId = companyId, AssignedByUserId = actor, AssignedAtUtc = now }); }
public sealed class UserCompany { private UserCompany() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public Guid CompanyId { get; private set; } public DateTimeOffset ValidFromUtc { get; private set; } public DateTimeOffset? ValidToUtc { get; private set; } public DateTimeOffset GrantedAtUtc { get; private set; } public Guid? GrantedByUserId { get; private set; } public bool IsEffective(DateTimeOffset now) => ValidFromUtc <= now && (ValidToUtc is null || ValidToUtc > now); public static Result<UserCompany> Create(Guid user, Guid company, DateTimeOffset from, DateTimeOffset? to, Guid? actor, DateTimeOffset now) { if (user == Guid.Empty || company == Guid.Empty || to <= from) return Result.Failure<UserCompany>(SecurityErrors.InvalidPeriod); return Result.Success(new UserCompany { Id = Guid.NewGuid(), UserId = user, CompanyId = company, ValidFromUtc = from, ValidToUtc = to, GrantedAtUtc = now, GrantedByUserId = actor }); } public void End(DateTimeOffset now) { if (ValidToUtc is null || ValidToUtc > now) ValidToUtc = now; } }
public sealed class UserRole { private UserRole() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public Guid RoleId { get; private set; } public Guid CompanyId { get; private set; } public DateTimeOffset ValidFromUtc { get; private set; } public DateTimeOffset? ValidToUtc { get; private set; } public DateTimeOffset AssignedAtUtc { get; private set; } public Guid? AssignedByUserId { get; private set; } public bool IsEffective(DateTimeOffset now) => ValidFromUtc <= now && (ValidToUtc is null || ValidToUtc > now); public static Result<UserRole> Create(Guid user, Guid role, Guid company, DateTimeOffset from, DateTimeOffset? to, Guid? actor, DateTimeOffset now) { if (user == Guid.Empty || role == Guid.Empty || company == Guid.Empty || to <= from) return Result.Failure<UserRole>(SecurityErrors.InvalidPeriod); return Result.Success(new UserRole { Id = Guid.NewGuid(), UserId = user, RoleId = role, CompanyId = company, ValidFromUtc = from, ValidToUtc = to, AssignedAtUtc = now, AssignedByUserId = actor }); } public void End(DateTimeOffset now) { if (ValidToUtc is null || ValidToUtc > now) ValidToUtc = now; } }

public sealed class AuthenticationSession { private AuthenticationSession() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public string TokenHash { get; private set; } = string.Empty; public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset ExpiresAtUtc { get; private set; } public DateTimeOffset? RevokedAtUtc { get; private set; } public static AuthenticationSession Create(Guid userId, string hash, DateTimeOffset now, DateTimeOffset expires) => new() { Id = Guid.NewGuid(), UserId = userId, TokenHash = hash, CreatedAtUtc = now, ExpiresAtUtc = expires }; public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now; }
public sealed class ApiClient { private ApiClient() { } public Guid Id { get; private set; } public string Code { get; private set; } = string.Empty; public string Name { get; private set; } = string.Empty; public bool IsActive { get; private set; } public string? CredentialFingerprint { get; private set; } public DateTimeOffset? CredentialExpiresAtUtc { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; } public static Result<ApiClient> Create(Guid id, string code, string name, DateTimeOffset now) { string c = code.Trim().ToUpperInvariant(); if (id == Guid.Empty || c.Length is < 3 or > 100 || name.Trim().Length == 0) return Result.Failure<ApiClient>(SecurityErrors.InvalidIdentity); return Result.Success(new ApiClient { Id = id, Code = c, Name = name.Trim(), IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); } public Result RotateCredential(string fingerprint,DateTimeOffset?expires,long expected,DateTimeOffset now){if(expected!=Version)return Result.Failure(Error.Concurrency("security.api_client.concurrency"));if(fingerprint.Length!=64||(expires is DateTimeOffset end&&end<=now))return Result.Failure(SecurityErrors.InvalidIdentity);CredentialFingerprint=fingerprint;CredentialExpiresAtUtc=expires;UpdatedAtUtc=now;Version++;return Result.Success();}public Result SetActive(bool active,long expected,DateTimeOffset now){if(expected!=Version)return Result.Failure(Error.Concurrency("security.api_client.concurrency"));IsActive=active;UpdatedAtUtc=now;Version++;return Result.Success();} }
public sealed class ApiClientCompany { private ApiClientCompany() { } public Guid Id { get; private set; } public Guid ApiClientId { get; private set; } public Guid CompanyId { get; private set; } public static ApiClientCompany Create(Guid client,Guid company)=>new(){Id=Guid.NewGuid(),ApiClientId=client,CompanyId=company}; }
public sealed class ApiClientPermission { private ApiClientPermission() { } public Guid Id { get; private set; } public Guid ApiClientId { get; private set; } public Guid PermissionId { get; private set; } public static ApiClientPermission Create(Guid client,Guid permission)=>new(){Id=Guid.NewGuid(),ApiClientId=client,PermissionId=permission}; }
#pragma warning restore CA1711
