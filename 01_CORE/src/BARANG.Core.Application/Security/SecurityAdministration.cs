using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Application.Security;

public sealed class SecurityAdministration(ISecurityStore store, IPasswordCredentialService passwords, ISessionTokenService tokens, ITransactionExecutor transactions, ICurrentActor actor, ICompanyContext company, ICorrelationContext correlation, TimeProvider time) : ISecurityAdministration, IPermissionAuthorizationService
{
    public Task<Result<UserView>> CreateUserAsync(CreateUserCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token => { Result<string> hash = passwords.HashPassword(command.Password); if (hash.IsFailure) return Result.Failure<UserView>(hash.Error); Result<ApplicationUser> user = ApplicationUser.Create(Guid.NewGuid(), command.Username, command.DisplayName, command.Email, hash.Value, time.GetUtcNow()); if (user.IsFailure) return Result.Failure<UserView>(user.Error); if (await store.UsernameExistsAsync(user.Value.Username, token)) return Result.Failure<UserView>(SecurityErrors.Conflict); store.Add(user.Value); Audit("security.user.created", "user", user.Value.Id); return Result.Success(View(user.Value)); }, ct);
    public async Task<Result<IReadOnlyList<UserView>>> ListUsersAsync(CancellationToken ct) => Result.Success<IReadOnlyList<UserView>>((await store.ListUsersAsync(ct)).Select(View).ToArray());
    public async Task<Result<UserView>> GetUserAsync(Guid id, CancellationToken ct) => Found(await store.FindUserAsync(id, ct), View);
    public Task<Result<UserView>> UpdateUserAsync(Guid id, string name, string? email, long version, CancellationToken ct) => ChangeUser(id, version, u => u.UpdateProfile(name, email, time.GetUtcNow()), "security.user.changed", ct);
    public Task<Result<UserView>> SetUserActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeUser(id, version, u => { u.SetActive(active, time.GetUtcNow()); return Result.Success(); }, active ? "security.user.activated" : "security.user.deactivated", ct);
    public Task<Result> SetPasswordAsync(Guid id, string password, CancellationToken ct) => transactions.ExecuteAsync(async token => { ApplicationUser? user = await store.FindUserAsync(id, token); if (user is null) return Result.Failure(SecurityErrors.NotFound); Result<string> hash = passwords.HashPassword(password); if (hash.IsFailure) return Result.Failure(hash.Error); user.SetPasswordHash(hash.Value, time.GetUtcNow()); foreach (AuthenticationSession session in await store.ListActiveSessionsAsync(id, time.GetUtcNow(), token)) session.Revoke(time.GetUtcNow()); Audit("security.user.password_set", "user", id); return Result.Success(); }, ct);
    public Task<Result<RoleView>> CreateRoleAsync(string code, string title, CancellationToken ct) => transactions.ExecuteAsync(async token => { Result<Role> role = Role.Create(Guid.NewGuid(), code, title, false, time.GetUtcNow()); if (role.IsFailure) return Result.Failure<RoleView>(role.Error); if (await store.RoleCodeExistsAsync(role.Value.Code, token)) return Result.Failure<RoleView>(SecurityErrors.Conflict); store.Add(role.Value); Audit("security.role.created", "role", role.Value.Id); return Result.Success(View(role.Value)); }, ct);
    public async Task<Result<IReadOnlyList<RoleView>>> ListRolesAsync(CancellationToken ct) => Result.Success<IReadOnlyList<RoleView>>((await store.ListRolesAsync(ct)).Select(View).ToArray());
    public async Task<Result<RoleView>> GetRoleAsync(Guid id, CancellationToken ct) => Found(await store.FindRoleAsync(id, ct), View);
    public Task<Result<RoleView>> UpdateRoleAsync(Guid id, string title, long version, CancellationToken ct) => ChangeRole(id, version, r => r.UpdateTitle(title, time.GetUtcNow()), "security.role.changed", ct);
    public Task<Result<RoleView>> SetRoleActiveAsync(Guid id, bool active, long version, CancellationToken ct) => ChangeRole(id, version, r => { r.SetActive(active, time.GetUtcNow()); return Result.Success(); }, active ? "security.role.activated" : "security.role.deactivated", ct);
    public async Task<Result<IReadOnlyList<PermissionView>>> ListPermissionsAsync(CancellationToken ct) => Result.Success<IReadOnlyList<PermissionView>>((await store.ListPermissionsAsync(ct)).Select(View).ToArray());
    public async Task<Result<IReadOnlyList<PermissionView>>> ListRolePermissionsAsync(Guid roleId, CancellationToken ct) => Result.Success<IReadOnlyList<PermissionView>>((await store.ListRolePermissionsAsync(roleId, ct)).Select(View).ToArray());
    public Task<Result> GrantPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct) => transactions.ExecuteAsync(async token => { if (await store.FindRoleAsync(roleId, token) is null || await store.FindPermissionAsync(permissionId, token) is null) return Result.Failure(SecurityErrors.NotFound); if (await store.FindRolePermissionAsync(roleId, permissionId, token) is not null) return Result.Failure(SecurityErrors.Conflict); store.Add(RolePermission.Create(roleId, permissionId, ActorId(), time.GetUtcNow())); Audit("security.permission.assigned", "role", roleId); return Result.Success(); }, ct);
    public Task<Result> RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct) => transactions.ExecuteAsync(async token => { RolePermission? value = await store.FindRolePermissionAsync(roleId, permissionId, token); if (value is null) return Result.Failure(SecurityErrors.NotFound); if (store is IRemovalStore removal) removal.Remove(value); Audit("security.permission.removed", "role", roleId); return Result.Success(); }, ct);
    public Task<Result<UserCompanyView>> GrantCompanyAsync(Guid userId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct) => transactions.ExecuteAsync(async token => { if (await store.FindUserAsync(userId, token) is null || await store.FindOpenCompanyAccessAsync(userId, companyId, token) is not null) return Result.Failure<UserCompanyView>(SecurityErrors.Conflict); Result<UserCompany> access = UserCompany.Create(userId, companyId, time.GetUtcNow(), validTo, ActorId(), time.GetUtcNow()); if (access.IsFailure) return Result.Failure<UserCompanyView>(access.Error); store.Add(access.Value); Audit("security.company_access.granted", "user", userId, companyId); return Result.Success(View(access.Value)); }, ct);
    public Task<Result> RevokeCompanyAsync(Guid userId, Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token => { UserCompany? access = await store.FindOpenCompanyAccessAsync(userId, companyId, token); if (access is null) return Result.Failure(SecurityErrors.NotFound); access.End(time.GetUtcNow()); Audit("security.company_access.revoked", "user", userId, companyId); return Result.Success(); }, ct);
    public async Task<Result<IReadOnlyList<UserCompanyView>>> ListCompaniesAsync(Guid userId, CancellationToken ct) => Result.Success<IReadOnlyList<UserCompanyView>>((await store.ListCompanyAccessAsync(userId, ct)).Select(View).ToArray());
    public Task<Result<UserRoleView>> AssignRoleAsync(Guid userId, Guid roleId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct) => transactions.ExecuteAsync(async token => { if (await store.FindEffectiveCompanyAccessAsync(userId, companyId, time.GetUtcNow(), token) is null || await store.FindRoleAsync(roleId, token) is not { IsActive: true } || await store.FindOpenUserRoleAsync(userId, roleId, companyId, token) is not null) return Result.Failure<UserRoleView>(SecurityErrors.AccessDenied); Result<UserRole> assignment = UserRole.Create(userId, roleId, companyId, time.GetUtcNow(), validTo, ActorId(), time.GetUtcNow()); if (assignment.IsFailure) return Result.Failure<UserRoleView>(assignment.Error); store.Add(assignment.Value); Audit("security.role.assigned", "user", userId, companyId); return Result.Success(View(assignment.Value)); }, ct);
    public Task<Result> RevokeRoleAsync(Guid userId, Guid roleId, Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token => { UserRole? role = await store.FindOpenUserRoleAsync(userId, roleId, companyId, token); if (role is null) return Result.Failure(SecurityErrors.NotFound); role.End(time.GetUtcNow()); Audit("security.role.revoked", "user", userId, companyId); return Result.Success(); }, ct);
    public async Task<Result<IReadOnlyList<UserRoleView>>> ListUserRolesAsync(Guid userId, CancellationToken ct) => Result.Success<IReadOnlyList<UserRoleView>>((await store.ListUserRolesAsync(userId, ct)).Select(View).ToArray());
    public async Task<Result<LoginResult>> LoginAsync(string username, string password, CancellationToken ct)
    {
        ApplicationUser? user = await store.FindUserByUsernameAsync(username.Trim().ToUpperInvariant(), ct);
        bool passwordValid = passwords.Verify(password, user?.PasswordHash);
        if (!passwordValid || user is not { IsActive: true })
        {
            await transactions.ExecuteAsync(_ => { Audit("security.login.failed", "login", null); return Task.FromResult(Result.Success()); }, ct);
            return Result.Failure<LoginResult>(SecurityErrors.InvalidCredentials);
        }
        return await transactions.ExecuteAsync(_ => { (string raw, string hash) = tokens.Create(); DateTimeOffset expires = time.GetUtcNow().AddMinutes(30); store.Add(AuthenticationSession.Create(user.Id, hash, time.GetUtcNow(), expires)); Audit("security.login.succeeded", "user", user.Id); return Task.FromResult(Result.Success(new LoginResult(raw, expires, View(user)))); }, ct);
    }
    public Task<Result> LogoutAsync(string tokenHash, CancellationToken ct) => transactions.ExecuteAsync(async token => { AuthenticationSession? session = await store.FindSessionAsync(tokenHash, time.GetUtcNow(), token); if (session is null) return Result.Failure(SecurityErrors.NotFound); session.Revoke(time.GetUtcNow()); return Result.Success(); }, ct);
    public Task<Result<CurrentUserView>> GetCurrentUserAsync(CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure<CurrentUserView>(SecurityErrors.UserContextUnavailable);
        ApplicationUser? user = await store.FindUserAsync(userId, token);
        if (user is not { IsActive: true }) return Result.Failure<CurrentUserView>(SecurityErrors.UserContextUnavailable);
        Guid? companyId = company.CompanyId;
        IReadOnlyList<string> roles = companyId is { } selected ? await store.ListEffectiveRoleCodesAsync(userId, selected, time.GetUtcNow(), token) : [];
        return Result.Success(new CurrentUserView(user.Id, user.Username, user.DisplayName, user.AccountState, companyId, roles));
    }, ct);
    public Task<Result<IReadOnlyList<MyCompanyView>>> ListMyCompaniesAsync(CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure<IReadOnlyList<MyCompanyView>>(SecurityErrors.UserContextUnavailable);
        IReadOnlyList<MyCompanyView> companies = await store.ListEffectiveCompaniesAsync(userId, time.GetUtcNow(), token);
        return Result.Success(companies);
    }, ct);
    public Task<Result<CompanySelectionView>> SelectMyCompanyAsync(Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure<CompanySelectionView>(SecurityErrors.UserContextUnavailable);
        DateTimeOffset now = time.GetUtcNow();
        if (!await store.HasEffectiveCompanyAccessAsync(userId, companyId, now, token)) return Result.Failure<CompanySelectionView>(SecurityErrors.CompanyNotAuthorized);
        if (!await store.CompanyIsActiveAsync(companyId, token)) return Result.Failure<CompanySelectionView>(SecurityErrors.CompanyInactive);
        MyCompanyView selected = (await store.FindEffectiveCompanyAsync(userId, companyId, now, token))!;
        IReadOnlyList<string> roles = await store.ListEffectiveRoleCodesAsync(userId, companyId, now, token);
        return Result.Success(new CompanySelectionView(selected.CompanyId, selected.Code, selected.DisplayName, roles));
    }, ct);
    public Task<Result<UserView>> BootstrapAsync(Guid companyId, CreateUserCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token => { if (await store.AnyUsersAsync(token)) return Result.Failure<UserView>(SecurityErrors.Conflict); Result<string> hash = passwords.HashPassword(command.Password); if (hash.IsFailure) return Result.Failure<UserView>(hash.Error); ApplicationUser user = ApplicationUser.Create(Guid.NewGuid(), command.Username, command.DisplayName, command.Email, hash.Value, time.GetUtcNow()).Value; Role role = Role.Create(Guid.NewGuid(), "system.administrator", "System administrator", true, time.GetUtcNow()).Value; store.Add(user); store.Add(role); foreach (Permission permission in await store.ListPermissionsAsync(token)) store.Add(RolePermission.Create(role.Id, permission.Id, user.Id, time.GetUtcNow())); store.Add(UserCompany.Create(user.Id, companyId, time.GetUtcNow(), null, user.Id, time.GetUtcNow()).Value); store.Add(UserRole.Create(user.Id, role.Id, companyId, time.GetUtcNow(), null, user.Id, time.GetUtcNow()).Value); Audit("security.bootstrap.completed", "user", user.Id, companyId); return Result.Success(View(user)); }, ct);
    public Task<Result> AuthorizeAsync(string permissionCode, CancellationToken ct) { if (!actor.Actor.IsAuthenticated || !Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Task.FromResult(Result.Failure(Error.Authentication("security.authentication.required"))); if (!company.IsAvailable) return Task.FromResult(Result.Failure(SecurityErrors.AccessDenied)); return transactions.ExecuteAsync(async token => await store.UserHasPermissionAsync(userId, company.RequireCompanyId(), permissionCode, time.GetUtcNow(), token) ? Result.Success() : Result.Failure(SecurityErrors.AccessDenied), ct); }
    private void Audit(string code, string targetType, Guid? target, Guid? companyId = null) => store.Add(SecurityAuditEvent.Create(code, ActorId(), targetType, target, companyId ?? company.CompanyId, time.GetUtcNow(), correlation.CorrelationId, actor.Actor.SourceChannel.ToString()));
    private Guid? ActorId() => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : null;
    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        return actor.Actor.IsAuthenticated && Guid.TryParse(actor.Actor.UserId, out userId);
    }
    private Task<Result<UserView>> ChangeUser(Guid id, long version, Func<ApplicationUser, Result> change, string code, CancellationToken ct) => transactions.ExecuteAsync(async token => { ApplicationUser? user = await store.FindUserAsync(id, token); if (user is null) return Result.Failure<UserView>(SecurityErrors.NotFound); if (user.Version != version) return Result.Failure<UserView>(Error.Concurrency("security.user.concurrency")); Result result = change(user); if (result.IsFailure) return Result.Failure<UserView>(result.Error); Audit(code, "user", id); return Result.Success(View(user)); }, ct);
    private Task<Result<RoleView>> ChangeRole(Guid id, long version, Func<Role, Result> change, string code, CancellationToken ct) => transactions.ExecuteAsync(async token => { Role? role = await store.FindRoleAsync(id, token); if (role is null) return Result.Failure<RoleView>(SecurityErrors.NotFound); if (role.Version != version) return Result.Failure<RoleView>(Error.Concurrency("security.role.concurrency")); Result result = change(role); if (result.IsFailure) return Result.Failure<RoleView>(result.Error); Audit(code, "role", id); return Result.Success(View(role)); }, ct);
    private static Result<TView> Found<T, TView>(T? value, Func<T, TView> map) where T : class => value is null ? Result.Failure<TView>(SecurityErrors.NotFound) : Result.Success(map(value));
    private static UserView View(ApplicationUser x) => new(x.Id, x.Username, x.Email, x.DisplayName, x.AccountState, x.CreatedAtUtc, x.UpdatedAtUtc, x.Version); private static RoleView View(Role x) => new(x.Id, x.Code, x.DisplayTitle, x.IsSystem, x.IsActive, x.Version); private static PermissionView View(Permission x) => new(x.Id, x.Code, x.TechnicalDescription, x.Module); private static UserCompanyView View(UserCompany x) => new(x.Id, x.UserId, x.CompanyId, x.ValidFromUtc, x.ValidToUtc); private static UserRoleView View(UserRole x) => new(x.Id, x.UserId, x.RoleId, x.CompanyId, x.ValidFromUtc, x.ValidToUtc);
}

public interface IRemovalStore { void Remove(RolePermission value); }
