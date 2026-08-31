using BARANG.Core.Domain.Common;
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
}

public sealed class ApplicationUser
{
    private ApplicationUser() { }
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public UserAccountState AccountState { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public bool IsActive => AccountState == UserAccountState.Active;
    public static Result<ApplicationUser> Create(Guid id, string username, string displayName, string? email, string passwordHash, DateTimeOffset now)
    {
        string normalized = username.Trim().ToUpperInvariant();
        string name = displayName.Trim();
        if (id == Guid.Empty || normalized.Length is < 3 or > 100 || !normalized.All(static c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-') || name.Length is < 1 or > 200 || string.IsNullOrWhiteSpace(passwordHash)) return Result.Failure<ApplicationUser>(SecurityErrors.InvalidIdentity);
        string? normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        return Result.Success(new ApplicationUser { Id = id, Username = normalized, DisplayName = name, Email = normalizedEmail, PasswordHash = passwordHash, AccountState = UserAccountState.Active, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result UpdateProfile(string displayName, string? email, DateTimeOffset now) { string name = displayName.Trim(); if (name.Length is < 1 or > 200) return Result.Failure(SecurityErrors.InvalidIdentity); DisplayName = name; Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(); Touch(now); return Result.Success(); }
    public void SetActive(bool active, DateTimeOffset now) { UserAccountState next = active ? UserAccountState.Active : UserAccountState.Inactive; if (AccountState != next) { AccountState = next; Touch(now); } }
    public void SetPasswordHash(string hash, DateTimeOffset now) { ArgumentException.ThrowIfNullOrWhiteSpace(hash); PasswordHash = hash; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}
public enum UserAccountState { Active, Inactive, Locked }

public sealed class Role
{
    private Role() { }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayTitle { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<Role> Create(Guid id, string code, string title, bool isSystem, DateTimeOffset now) { string normalized = code.Trim().ToLowerInvariant(); string display = title.Trim(); if (id == Guid.Empty || normalized.Length is < 3 or > 100 || !PermissionCode.IsValid(normalized) || display.Length is < 1 or > 200) return Result.Failure<Role>(SecurityErrors.InvalidIdentity); return Result.Success(new Role { Id = id, Code = normalized, DisplayTitle = display, IsSystem = isSystem, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); }
    public Result UpdateTitle(string title, DateTimeOffset now) { string value = title.Trim(); if (value.Length is < 1 or > 200) return Result.Failure(SecurityErrors.InvalidIdentity); DisplayTitle = value; Touch(now); return Result.Success(); }
    public void SetActive(bool active, DateTimeOffset now) { if (IsActive != active) { IsActive = active; Touch(now); } }
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
    public const string AuditRead = "audit.read";
    public const string LookupRead = "lookup.read";
    public const string LookupManage = "lookup.manage";
    public const string CustomerRead = "customer.read";
    public const string CustomerManage = "customer.manage";
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
    public static IReadOnlyList<string> All { get; } = [OrganizationCompanyManage, OrganizationBranchManage, OrganizationSettingsManage, OrganizationUnitManage, OrganizationWarehouseManage, SecurityUserManage, SecurityRoleManage, AuditRead, LookupRead, LookupManage, CustomerRead, CustomerManage, ProductRead, ProductManage, SupplierRead, SupplierManage, EquipmentRead, EquipmentManage, ArtworkRead, ArtworkManage, FileRead, FileManage, FileUpload];
}

public sealed class RolePermission { private RolePermission() { } public Guid Id { get; private set; } public Guid RoleId { get; private set; } public Guid PermissionId { get; private set; } public DateTimeOffset AssignedAtUtc { get; private set; } public Guid? AssignedByUserId { get; private set; } public static RolePermission Create(Guid roleId, Guid permissionId, Guid? actor, DateTimeOffset now) => new() { Id = Guid.NewGuid(), RoleId = roleId, PermissionId = permissionId, AssignedByUserId = actor, AssignedAtUtc = now }; }
public sealed class UserCompany { private UserCompany() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public Guid CompanyId { get; private set; } public DateTimeOffset ValidFromUtc { get; private set; } public DateTimeOffset? ValidToUtc { get; private set; } public DateTimeOffset GrantedAtUtc { get; private set; } public Guid? GrantedByUserId { get; private set; } public bool IsEffective(DateTimeOffset now) => ValidFromUtc <= now && (ValidToUtc is null || ValidToUtc > now); public static Result<UserCompany> Create(Guid user, Guid company, DateTimeOffset from, DateTimeOffset? to, Guid? actor, DateTimeOffset now) { if (user == Guid.Empty || company == Guid.Empty || to <= from) return Result.Failure<UserCompany>(SecurityErrors.InvalidPeriod); return Result.Success(new UserCompany { Id = Guid.NewGuid(), UserId = user, CompanyId = company, ValidFromUtc = from, ValidToUtc = to, GrantedAtUtc = now, GrantedByUserId = actor }); } public void End(DateTimeOffset now) { if (ValidToUtc is null || ValidToUtc > now) ValidToUtc = now; } }
public sealed class UserRole { private UserRole() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public Guid RoleId { get; private set; } public Guid CompanyId { get; private set; } public DateTimeOffset ValidFromUtc { get; private set; } public DateTimeOffset? ValidToUtc { get; private set; } public DateTimeOffset AssignedAtUtc { get; private set; } public Guid? AssignedByUserId { get; private set; } public bool IsEffective(DateTimeOffset now) => ValidFromUtc <= now && (ValidToUtc is null || ValidToUtc > now); public static Result<UserRole> Create(Guid user, Guid role, Guid company, DateTimeOffset from, DateTimeOffset? to, Guid? actor, DateTimeOffset now) { if (user == Guid.Empty || role == Guid.Empty || company == Guid.Empty || to <= from) return Result.Failure<UserRole>(SecurityErrors.InvalidPeriod); return Result.Success(new UserRole { Id = Guid.NewGuid(), UserId = user, RoleId = role, CompanyId = company, ValidFromUtc = from, ValidToUtc = to, AssignedAtUtc = now, AssignedByUserId = actor }); } public void End(DateTimeOffset now) { if (ValidToUtc is null || ValidToUtc > now) ValidToUtc = now; } }

public sealed class AuthenticationSession { private AuthenticationSession() { } public Guid Id { get; private set; } public Guid UserId { get; private set; } public string TokenHash { get; private set; } = string.Empty; public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset ExpiresAtUtc { get; private set; } public DateTimeOffset? RevokedAtUtc { get; private set; } public static AuthenticationSession Create(Guid userId, string hash, DateTimeOffset now, DateTimeOffset expires) => new() { Id = Guid.NewGuid(), UserId = userId, TokenHash = hash, CreatedAtUtc = now, ExpiresAtUtc = expires }; public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now; }
public sealed class ApiClient { private ApiClient() { } public Guid Id { get; private set; } public string Code { get; private set; } = string.Empty; public string Name { get; private set; } = string.Empty; public bool IsActive { get; private set; } public string? CredentialFingerprint { get; private set; } public DateTimeOffset? CredentialExpiresAtUtc { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; } public static Result<ApiClient> Create(Guid id, string code, string name, DateTimeOffset now) { string c = code.Trim().ToUpperInvariant(); if (id == Guid.Empty || c.Length is < 3 or > 100 || name.Trim().Length == 0) return Result.Failure<ApiClient>(SecurityErrors.InvalidIdentity); return Result.Success(new ApiClient { Id = id, Code = c, Name = name.Trim(), IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); } }
public sealed class ApiClientCompany { private ApiClientCompany() { } public Guid Id { get; private set; } public Guid ApiClientId { get; private set; } public Guid CompanyId { get; private set; } }
public sealed class ApiClientPermission { private ApiClientPermission() { } public Guid Id { get; private set; } public Guid ApiClientId { get; private set; } public Guid PermissionId { get; private set; } }
#pragma warning restore CA1711
