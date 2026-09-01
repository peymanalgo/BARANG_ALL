using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Application.Security;

public sealed record UserView(Guid Id, string Username, string? Email, string DisplayName, UserAccountState AccountState, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long Version);
public sealed record RoleView(Guid Id, string Code, string DisplayTitle, bool IsSystem, bool IsActive, long Version);
public sealed record PermissionView(Guid Id, string Code, string TechnicalDescription, string Module);
public sealed record UserCompanyView(Guid Id, Guid UserId, Guid CompanyId, DateTimeOffset ValidFromUtc, DateTimeOffset? ValidToUtc);
public sealed record UserRoleView(Guid Id, Guid UserId, Guid RoleId, Guid CompanyId, DateTimeOffset ValidFromUtc, DateTimeOffset? ValidToUtc);
public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAtUtc, UserView User);
public sealed record CurrentUserView(Guid UserId, string Username, string DisplayName, UserAccountState AccountState, Guid? CurrentCompanyId, IReadOnlyList<string> EffectiveRoleCodes);
public sealed record MyCompanyView(Guid CompanyId, string Code, string DisplayName);
public sealed record CompanySelectionView(Guid CompanyId, string Code, string DisplayName, IReadOnlyList<string> EffectiveRoleCodes);
public sealed record CreateUserCommand(string Username, string DisplayName, string? Email, string Password);

public interface IPasswordCredentialService { Result<string> HashPassword(string password); bool Verify(string password, string? hash); }
public interface ISessionTokenService { (string Token, string Hash) Create(); string Hash(string token); }
public interface ISecurityStore
{
    void Add(ApplicationUser value); void Add(Role value); void Add(RolePermission value); void Add(UserCompany value); void Add(UserRole value); void Add(AuthenticationSession value);
    Task<ApplicationUser?> FindUserAsync(Guid id, CancellationToken ct); Task<ApplicationUser?> FindUserByUsernameAsync(string username, CancellationToken ct); Task<IReadOnlyList<ApplicationUser>> ListUsersAsync(CancellationToken ct); Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
    Task<Role?> FindRoleAsync(Guid id, CancellationToken ct); Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken ct); Task<bool> RoleCodeExistsAsync(string code, CancellationToken ct);
    Task<Permission?> FindPermissionAsync(Guid id, CancellationToken ct); Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken ct); Task<IReadOnlyList<Permission>> ListRolePermissionsAsync(Guid roleId, CancellationToken ct);
    Task<UserCompany?> FindEffectiveCompanyAccessAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct); Task<IReadOnlyList<UserCompany>> ListCompanyAccessAsync(Guid userId, CancellationToken ct); Task<UserCompany?> FindOpenCompanyAccessAsync(Guid userId, Guid companyId, CancellationToken ct);
    Task<UserRole?> FindOpenUserRoleAsync(Guid userId, Guid roleId, Guid companyId, CancellationToken ct); Task<IReadOnlyList<UserRole>> ListUserRolesAsync(Guid userId, CancellationToken ct);
    Task<RolePermission?> FindRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct); Task<bool> UserHasPermissionAsync(Guid userId, Guid companyId, string permission, DateTimeOffset now, CancellationToken ct);
    Task<AuthenticationSession?> FindSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken ct); Task<IReadOnlyList<AuthenticationSession>> ListActiveSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<MyCompanyView>> ListEffectiveCompaniesAsync(Guid userId, DateTimeOffset now, CancellationToken ct); Task<MyCompanyView?> FindEffectiveCompanyAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct); Task<bool> HasEffectiveCompanyAccessAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct); Task<bool> CompanyIsActiveAsync(Guid companyId, CancellationToken ct); Task<IReadOnlyList<string>> ListEffectiveRoleCodesAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct);
    Task<bool> AnyUsersAsync(CancellationToken ct);
    Task<bool> ApiClientHasCompanyAsync(Guid clientId,Guid companyId,CancellationToken ct);Task<bool> ApiClientHasPermissionAsync(Guid clientId,string permission,CancellationToken ct);
}

public interface IPermissionAuthorizationService { Task<Result> AuthorizeAsync(string permissionCode, CancellationToken ct); }
public interface ISecurityAdministration
{
    Task<Result<UserView>> CreateUserAsync(CreateUserCommand command, CancellationToken ct); Task<Result<IReadOnlyList<UserView>>> ListUsersAsync(CancellationToken ct); Task<Result<UserView>> GetUserAsync(Guid id, CancellationToken ct); Task<Result<UserView>> UpdateUserAsync(Guid id, string name, string? email, long version, CancellationToken ct); Task<Result<UserView>> SetUserActiveAsync(Guid id, bool active, long version, CancellationToken ct); Task<Result> SetPasswordAsync(Guid id, string password, CancellationToken ct);
    Task<Result<RoleView>> CreateRoleAsync(string code, string title, CancellationToken ct); Task<Result<IReadOnlyList<RoleView>>> ListRolesAsync(CancellationToken ct); Task<Result<RoleView>> GetRoleAsync(Guid id, CancellationToken ct); Task<Result<RoleView>> UpdateRoleAsync(Guid id, string title, long version, CancellationToken ct); Task<Result<RoleView>> SetRoleActiveAsync(Guid id, bool active, long version, CancellationToken ct);
    Task<Result<IReadOnlyList<PermissionView>>> ListPermissionsAsync(CancellationToken ct); Task<Result<IReadOnlyList<PermissionView>>> ListRolePermissionsAsync(Guid roleId, CancellationToken ct); Task<Result> GrantPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct); Task<Result> RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct);
    Task<Result<UserCompanyView>> GrantCompanyAsync(Guid userId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct); Task<Result> RevokeCompanyAsync(Guid userId, Guid companyId, CancellationToken ct); Task<Result<IReadOnlyList<UserCompanyView>>> ListCompaniesAsync(Guid userId, CancellationToken ct);
    Task<Result<UserRoleView>> AssignRoleAsync(Guid userId, Guid roleId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct); Task<Result> RevokeRoleAsync(Guid userId, Guid roleId, Guid companyId, CancellationToken ct); Task<Result<IReadOnlyList<UserRoleView>>> ListUserRolesAsync(Guid userId, CancellationToken ct);
    Task<Result<LoginResult>> LoginAsync(string username, string password, CancellationToken ct); Task<Result> LogoutAsync(string tokenHash, CancellationToken ct);
    Task<Result<CurrentUserView>> GetCurrentUserAsync(CancellationToken ct); Task<Result<IReadOnlyList<MyCompanyView>>> ListMyCompaniesAsync(CancellationToken ct); Task<Result<CompanySelectionView>> SelectMyCompanyAsync(Guid companyId, CancellationToken ct);
    Task<Result<UserView>> BootstrapAsync(Guid companyId, CreateUserCommand command, CancellationToken ct);
}

public interface IRequestIdentityResolver { Task<ActorContext?> ResolveAsync(string accessToken, CancellationToken ct); }
