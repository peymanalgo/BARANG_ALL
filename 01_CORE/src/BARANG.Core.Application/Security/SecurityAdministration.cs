using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Organization;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Application.Security;

public sealed class SecurityAdministration(
    ISecurityStore store,
    IOrganizationalChartStore chart,
    IOrganizationStore organization,
    IIdentityAccountService identity,
    IPasswordCredentialService passwordCompatibility,
    ISessionTokenService tokens,
    ITransactionExecutor transactions,
    ICurrentActor actor,
    ICompanyContext company,
    IAuditWriter audit,
    TimeProvider time) : ISecurityAdministration, IPermissionAuthorizationService
{
    public Task<Result<UserView>> CreateUserAsync(CreateUserCommand command, CancellationToken ct) => Task.FromResult(Result.Failure<UserView>(Error.Validation("security.user.person_required")));

    public Task<Result<IReadOnlyList<UserView>>> ListUsersAsync(CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!company.IsAvailable) return Result.Failure<IReadOnlyList<UserView>>(SecurityErrors.AccessDenied);
        return Result.Success<IReadOnlyList<UserView>>((await store.ListUsersAsync(token)).Select(View).ToArray());
    }, ct);

    public Task<Result<UserView>> GetUserAsync(Guid id, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        return scoped.IsFailure ? Result.Failure<UserView>(scoped.Error) : Result.Success(View(scoped.Value));
    }, ct);

    // Kept only for contract compatibility. Person profile fields are no longer editable from Security.
    public Task<Result<UserView>> UpdateUserAsync(Guid id, string name, string? email, long version, CancellationToken ct) => UpdateUserAccountAsync(id, name, version, ct);

    public Task<Result<IReadOnlyList<PersonUserCandidateView>>> SearchPersonCandidatesAsync(string? query, int take, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        company.RequireCompanyId();
        if (take is < 1 or > 100) return Result.Failure<IReadOnlyList<PersonUserCandidateView>>(Error.Validation("security.person.invalid_limit"));
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return Result.Success<IReadOnlyList<PersonUserCandidateView>>([]);
        IReadOnlyList<Person> people = await chart.SearchPersonsAsync(query, take, token);
        List<PersonUserCandidateView> result = [];
        foreach (Person person in people)
        {
            ApplicationUser? linked = await store.FindUserByPersonAsync(person.Id, token);
            result.Add(new(await PersonSummary(person, token), linked is not null, linked?.Id, linked?.Username));
        }
        return Result.Success<IReadOnlyList<PersonUserCandidateView>>(result);
    }, ct);

    public Task<Result<PagedResult<SecurityUserListItemView>>> SearchUsersAsync(SecurityUserQuery query, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid companyId = company.RequireCompanyId();
        PagedResult<ApplicationUser> page;
        try { page = await store.SearchUsersAsync(query, companyId, time.GetUtcNow(), token); }
        catch (ArgumentOutOfRangeException) { return Result.Failure<PagedResult<SecurityUserListItemView>>(Error.Validation("security.user.invalid_pagination")); }
        List<SecurityUserListItemView> items = [];
        foreach (ApplicationUser user in page.Items)
        {
            PersonSummaryView person = await PersonSummary(user, token);
            IReadOnlyList<UserRoleDetailView> roles = await store.ListEffectiveUserRoleDetailsAsync(user.Id, companyId, time.GetUtcNow(), token);
            items.Add(new(View(user), person, roles.Select(x => x.RoleTitle).ToArray()));
        }
        return Result.Success(new PagedResult<SecurityUserListItemView>(items, page.PageNumber, page.PageSize, page.TotalCount));
    }, ct);

    public Task<Result<SecurityUserDetailView>> GetUserDetailAsync(Guid id, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        if (scoped.IsFailure) return Result.Failure<SecurityUserDetailView>(scoped.Error);
        Guid companyId = company.RequireCompanyId();
        PersonSummaryView person = await PersonSummary(scoped.Value, token);
        IReadOnlyList<UserRoleDetailView> roles = await store.ListEffectiveUserRoleDetailsAsync(id, companyId, time.GetUtcNow(), token);
        return Result.Success(new SecurityUserDetailView(View(scoped.Value), person, roles));
    }, ct);

    public Task<Result<UserView>> CreateLinkedUserAsync(CreateLinkedUserCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid companyId = company.RequireCompanyId();
        Person? person = await chart.FindPersonAsync(command.PersonId, token);
        if (person is not { IsActive: true } || person.CompanyId != companyId) return Result.Failure<UserView>(SecurityErrors.NotFound);
        if (await store.FindUserByPersonAsync(person.Id, token) is not null) return Result.Failure<UserView>(SecurityErrors.DuplicatePersonAccount);
        if (await store.UsernameExistsAsync(command.Username, null, token)) return Result.Failure<UserView>(SecurityErrors.Conflict);

        Guid[] roleIds = (command.RoleIds ?? []).Distinct().ToArray();
        List<Role> roles = [];
        foreach (Guid roleId in roleIds)
        {
            Role? role = await store.FindRoleAsync(roleId, token);
            if (role is not { IsActive: true }) return Result.Failure<UserView>(SecurityErrors.NotFound);
            Result delegation = await CanAssignRole(role, token);
            if (delegation.IsFailure) return Result.Failure<UserView>(delegation.Error);
            roles.Add(role);
        }

        Result<ApplicationUser> created = ApplicationUser.CreateIdentity(Guid.NewGuid(), person.Id, command.Username, person.DisplayName, person.Email, time.GetUtcNow());
        if (created.IsFailure) return Result.Failure<UserView>(created.Error);
        Result identityResult = await identity.CreateAsync(created.Value, command.Password, token);
        if (identityResult.IsFailure) return Result.Failure<UserView>(identityResult.Error);
        if (!command.Enabled) created.Value.SetActive(false, time.GetUtcNow());

        Result<UserCompany> access = UserCompany.Create(created.Value.Id, companyId, time.GetUtcNow(), null, ActorId(), time.GetUtcNow());
        if (access.IsFailure) return Result.Failure<UserView>(access.Error);
        store.Add(access.Value);
        Audit(AuditActions.UserCompanyGranted, "user", created.Value.Id, companyId, AuditChangeKind.Added);
        foreach (Role role in roles)
        {
            Result<UserRole> assignment = UserRole.Create(created.Value.Id, role.Id, companyId, time.GetUtcNow(), null, ActorId(), time.GetUtcNow());
            if (assignment.IsFailure) return Result.Failure<UserView>(assignment.Error);
            store.Add(assignment.Value);
            Audit(AuditActions.UserRoleAssigned, "user", created.Value.Id, companyId, AuditChangeKind.Added, new Dictionary<string, object?> { ["roleId"] = role.Id, ["roleCode"] = role.Code });
        }
        Audit(AuditActions.UserCreated, "user", created.Value.Id, companyId, AuditChangeKind.Added, new Dictionary<string, object?> { ["personId"] = person.Id, ["username"] = created.Value.Username, ["roleIds"] = roleIds });
        return Result.Success(View(created.Value));
    }, ct);

    public Task<Result<UserView>> CreateUserForPersonAsync(Guid personId, CreateUserCommand command, bool enabled, CancellationToken ct) => CreateLinkedUserAsync(new(personId, command.Username, command.Password, enabled), ct);

    public Task<Result<PersonAccessView>> GetPersonAccessAsync(Guid personId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid companyId = company.RequireCompanyId();
        Person? person = await chart.FindPersonAsync(personId, token);
        if (person is null || person.CompanyId != companyId) return Result.Failure<PersonAccessView>(SecurityErrors.NotFound);
        ApplicationUser? user = await store.FindUserByPersonAsync(personId, token);
        if (user is null) return Result.Success(new PersonAccessView(null, [], []));
        IReadOnlyList<string> roles = await store.ListEffectiveRoleCodesAsync(user.Id, companyId, time.GetUtcNow(), token);
        IReadOnlyList<string> permissions = await store.ListEffectivePermissionCodesAsync(user.Id, companyId, time.GetUtcNow(), token);
        return Result.Success(new PersonAccessView(View(user), roles, permissions));
    }, ct);

    public Task<Result<UserView>> UpdateUserAccountAsync(Guid id, string username, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        if (scoped.IsFailure) return Result.Failure<UserView>(scoped.Error);
        if (scoped.Value.Version != version) return Result.Failure<UserView>(Error.Concurrency("security.user.concurrency"));
        if (await store.UsernameExistsAsync(username, id, token)) return Result.Failure<UserView>(SecurityErrors.Conflict);
        string old = scoped.Value.Username;
        Result changed = await identity.UpdateUsernameAsync(scoped.Value, username, token);
        if (changed.IsFailure) return Result.Failure<UserView>(changed.Error);
        audit.Write(new(AuditActions.UserUpdated, "user", id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: AuditChangeKind.Modified, Changes: new Dictionary<string, AuditValueChange> { ["username"] = new(old, scoped.Value.Username) }));
        return Result.Success(View(scoped.Value));
    }, ct);

    public Task<Result<UserView>> SetUserActiveAsync(Guid id, bool active, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        if (scoped.IsFailure) return Result.Failure<UserView>(scoped.Error);
        ApplicationUser user = scoped.Value;
        if (user.Version != version) return Result.Failure<UserView>(Error.Concurrency("security.user.concurrency"));
        if (!active && await store.IsSystemAdministratorAsync(id, time.GetUtcNow(), token) && await store.CountRecoveryAdministratorsAsync(time.GetUtcNow(), token) <= 1) return Result.Failure<UserView>(SecurityErrors.CriticalAdministrator);
        UserAccountState old = user.AccountState;
        user.SetActive(active, time.GetUtcNow());
        int revoked = !active ? RevokeSessions(await store.ListActiveSessionsAsync(id, time.GetUtcNow(), token)) : 0;
        audit.Write(new(active ? AuditActions.UserActivated : AuditActions.UserDeactivated, "user", id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: AuditChangeKind.Modified, Metadata: new Dictionary<string, object?> { ["revokedSessionCount"] = revoked }, Changes: new Dictionary<string, AuditValueChange> { ["accountState"] = new(old.ToString(), user.AccountState.ToString()) }));
        return Result.Success(View(user));
    }, ct);

    public Task<Result<UserView>> UnlockUserAsync(Guid id, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        if (scoped.IsFailure) return Result.Failure<UserView>(scoped.Error);
        if (scoped.Value.Version != version) return Result.Failure<UserView>(Error.Concurrency("security.user.concurrency"));
        Result unlocked = await identity.UnlockAsync(scoped.Value, token);
        if (unlocked.IsFailure) return Result.Failure<UserView>(unlocked.Error);
        Audit(AuditActions.UserUnlocked, "user", id, company.RequireCompanyId(), AuditChangeKind.Modified);
        return Result.Success(View(scoped.Value));
    }, ct);

    public Task<Result> SetPasswordAsync(Guid id, string password, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(id, token);
        if (scoped.IsFailure) return Result.Failure(scoped.Error);
        Result reset = await identity.ResetPasswordAsync(scoped.Value, password, token);
        if (reset.IsFailure) return reset;
        int revoked = RevokeSessions(await store.ListActiveSessionsAsync(id, time.GetUtcNow(), token));
        Audit(AuditActions.UserPasswordReset, "user", id, company.RequireCompanyId(), AuditChangeKind.Modified, new Dictionary<string, object?> { ["revokedSessionCount"] = revoked });
        return Result.Success();
    }, ct);

    public Task<Result> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure(SecurityErrors.UserContextUnavailable);
        ApplicationUser? user = await store.FindUserAsync(userId, token);
        if (user is null) return Result.Failure(SecurityErrors.UserContextUnavailable);
        Result<PasswordAuthenticationOutcome> checkedPassword = await identity.CheckPasswordAsync(user, currentPassword, token);
        if (checkedPassword.IsFailure || checkedPassword.Value != PasswordAuthenticationOutcome.Success) return Result.Failure(SecurityErrors.InvalidCredentials);
        Result reset = await identity.ResetPasswordAsync(user, newPassword, token);
        if (reset.IsFailure) return reset;
        int revoked = RevokeSessions(await store.ListActiveSessionsAsync(userId, time.GetUtcNow(), token));
        Audit(AuditActions.UserPasswordReset, "user", userId, company.IsAvailable ? company.RequireCompanyId() : null, AuditChangeKind.Modified, new Dictionary<string, object?> { ["self"] = true, ["revokedSessionCount"] = revoked });
        return Result.Success();
    }, ct);

    public Task<Result<RoleView>> CreateRoleAsync(string code, string title, CancellationToken ct) => CreateRoleAsync(code, title, null, ct);
    public Task<Result<RoleView>> CreateRoleAsync(string code, string title, string? description, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<Role> role = Role.Create(Guid.NewGuid(), code, title, description, RoleScope.Company, false, time.GetUtcNow());
        if (role.IsFailure) return Result.Failure<RoleView>(role.Error);
        if (await store.RoleCodeExistsAsync(role.Value.Code, token)) return Result.Failure<RoleView>(SecurityErrors.Conflict);
        store.Add(role.Value);
        Audit(AuditActions.RoleCreated, "role", role.Value.Id, company.RequireCompanyId(), AuditChangeKind.Added, new Dictionary<string, object?> { ["code"] = role.Value.Code });
        return Result.Success(View(role.Value));
    }, ct);

    public async Task<Result<IReadOnlyList<RoleView>>> ListRolesAsync(CancellationToken ct) => Result.Success<IReadOnlyList<RoleView>>((await store.ListRolesAsync(ct)).Select(View).ToArray());

    public Task<Result<PagedResult<RoleView>>> SearchRolesAsync(SecurityRoleQuery query, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        try { return Result.Success(await store.SearchRolesAsync(query, company.RequireCompanyId(), time.GetUtcNow(), token)); }
        catch (ArgumentOutOfRangeException) { return Result.Failure<PagedResult<RoleView>>(Error.Validation("security.role.invalid_pagination")); }
    }, ct);

    public async Task<Result<RoleView>> GetRoleAsync(Guid id, CancellationToken ct)
    {
        Role? role = await store.FindRoleAsync(id, ct);
        return role is null ? Result.Failure<RoleView>(SecurityErrors.NotFound) : Result.Success(View(role));
    }

    public Task<Result<RoleView>> UpdateRoleAsync(Guid id, string title, long version, CancellationToken ct) => UpdateRoleAsync(id, title, null, version, ct);
    public Task<Result<RoleView>> UpdateRoleAsync(Guid id, string title, string? description, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(id, token);
        if (role is null) return Result.Failure<RoleView>(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure<RoleView>(SecurityErrors.SystemRoleProtected);
        if (role.Version != version) return Result.Failure<RoleView>(Error.Concurrency("security.role.concurrency"));
        string oldTitle = role.DisplayTitle; string? oldDescription = role.Description;
        Result changed = role.Update(title, description, time.GetUtcNow());
        if (changed.IsFailure) return Result.Failure<RoleView>(changed.Error);
        audit.Write(new(AuditActions.RoleUpdated, "role", id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: AuditChangeKind.Modified, Changes: new Dictionary<string, AuditValueChange> { ["displayTitle"] = new(oldTitle, role.DisplayTitle), ["description"] = new(oldDescription, role.Description) }));
        return Result.Success(View(role));
    }, ct);

    public Task<Result<RoleView>> SetRoleActiveAsync(Guid id, bool active, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(id, token);
        if (role is null) return Result.Failure<RoleView>(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure<RoleView>(SecurityErrors.SystemRoleProtected);
        if (role.Version != version) return Result.Failure<RoleView>(Error.Concurrency("security.role.concurrency"));
        bool old = role.IsActive; role.SetActive(active, time.GetUtcNow());
        audit.Write(new(active ? AuditActions.RoleActivated : AuditActions.RoleDeactivated, "role", id.ToString(), CompanyId: company.RequireCompanyId(), ChangeKind: AuditChangeKind.Modified, Changes: new Dictionary<string, AuditValueChange> { ["isActive"] = new(old, role.IsActive) }));
        return Result.Success(View(role));
    }, ct);

    public Task<Result> DeleteRoleAsync(Guid id, long version, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(id, token);
        if (role is null) return Result.Failure(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure(SecurityErrors.SystemRoleProtected);
        if (role.Version != version) return Result.Failure(Error.Concurrency("security.role.concurrency"));
        if (await store.RoleHasAssignmentsAsync(id, token)) return Result.Failure(SecurityErrors.Conflict);
        if (store is not IRemovalStore removal) return Result.Failure(SecurityErrors.Conflict);
        foreach (RolePermission permission in await store.ListRolePermissionLinksAsync(id, token)) removal.Remove(permission);
        removal.Remove(role);
        Audit(AuditActions.RoleDeleted, "role", id, company.RequireCompanyId(), AuditChangeKind.Deleted, new Dictionary<string, object?> { ["code"] = role.Code });
        return Result.Success();
    }, ct);

    public async Task<Result<IReadOnlyList<PermissionView>>> ListPermissionsAsync(CancellationToken ct) => Result.Success<IReadOnlyList<PermissionView>>((await store.ListPermissionsAsync(ct)).Select(View).ToArray());
    public async Task<Result<IReadOnlyList<PermissionView>>> ListRolePermissionsAsync(Guid roleId, CancellationToken ct) => Result.Success<IReadOnlyList<PermissionView>>((await store.ListRolePermissionsAsync(roleId, ct)).Select(View).ToArray());

    public Task<Result<PermissionTargetView>> GetRolePermissionTargetAsync(Guid roleId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(roleId, token);
        if (role is null) return Result.Failure<PermissionTargetView>(SecurityErrors.NotFound);
        IReadOnlyList<Permission> all = await store.ListPermissionsAsync(token);
        HashSet<Guid> assigned = (await store.ListRolePermissionsAsync(roleId, token)).Select(x => x.Id).ToHashSet();
        return Result.Success(RolePermissionTarget(role, all, assigned));
    }, ct);

    public Task<Result<PermissionTargetView>> GetUserPermissionTargetAsync(Guid userId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(userId, token);
        if (scoped.IsFailure) return Result.Failure<PermissionTargetView>(scoped.Error);
        return Result.Success(await UserPermissionTarget(scoped.Value, token));
    }, ct);

    public Task<Result<PermissionTargetView>> SaveRolePermissionsAsync(Guid roleId, SavePermissionSetCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(roleId, token);
        if (role is null) return Result.Failure<PermissionTargetView>(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure<PermissionTargetView>(SecurityErrors.SystemRoleProtected);
        if (role.Version != command.Version) return Result.Failure<PermissionTargetView>(Error.Concurrency("security.role.concurrency"));
        IReadOnlyList<Permission> all = await store.ListPermissionsAsync(token);
        HashSet<Guid> desired = command.PermissionIds.Distinct().ToHashSet();
        if (desired.Count != command.PermissionIds.Count || desired.Any(id => all.All(p => p.Id != id))) return Result.Failure<PermissionTargetView>(Error.Validation("security.permission.invalid_set"));
        IReadOnlyList<Permission> currentPermissions = await store.ListRolePermissionsAsync(roleId, token);
        HashSet<Guid> current = currentPermissions.Select(x => x.Id).ToHashSet();
        Guid[] added = desired.Except(current).ToArray(); Guid[] removed = current.Except(desired).ToArray();
        foreach (Guid id in added.Concat(removed)) { Result allowed = await CanDelegate(all.Single(x => x.Id == id).Code, token); if (allowed.IsFailure) return Result.Failure<PermissionTargetView>(allowed.Error); }
        foreach (Guid id in added) store.Add(RolePermission.Create(roleId, id, ActorId(), time.GetUtcNow()));
        foreach (Guid id in removed) { RolePermission? link = await store.FindRolePermissionAsync(roleId, id, token); if (link is not null && store is IRemovalStore removal) removal.Remove(link); }
        if (added.Length > 0 || removed.Length > 0) role.TouchPermissions(time.GetUtcNow());
        Audit(AuditActions.RolePermissionsChanged, "role", roleId, company.RequireCompanyId(), AuditChangeKind.Modified, PermissionChanges(all, added, removed));
        return Result.Success(RolePermissionTarget(role, all, desired));
    }, ct);

    public Task<Result<PermissionTargetView>> SaveUserPermissionsAsync(Guid userId, SavePermissionSetCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Result<ApplicationUser> scoped = await FindScopedUser(userId, token);
        if (scoped.IsFailure) return Result.Failure<PermissionTargetView>(scoped.Error);
        ApplicationUser user = scoped.Value;
        if (user.Version != command.Version) return Result.Failure<PermissionTargetView>(Error.Concurrency("security.user.concurrency"));
        Guid companyId = company.RequireCompanyId();
        IReadOnlyList<Permission> all = await store.ListPermissionsAsync(token);
        HashSet<Guid> desired = command.PermissionIds.Distinct().ToHashSet();
        if (desired.Count != command.PermissionIds.Count || desired.Any(id => all.All(p => p.Id != id))) return Result.Failure<PermissionTargetView>(Error.Validation("security.permission.invalid_set"));
        IReadOnlyList<UserPermission> links = await store.ListUserPermissionLinksAsync(userId, companyId, token);
        HashSet<Guid> current = links.Select(x => x.PermissionId).ToHashSet();
        Guid[] added = desired.Except(current).ToArray(); Guid[] removed = current.Except(desired).ToArray();
        foreach (Guid id in added.Concat(removed)) { Result allowed = await CanDelegate(all.Single(x => x.Id == id).Code, token); if (allowed.IsFailure) return Result.Failure<PermissionTargetView>(allowed.Error); }
        foreach (Guid id in added) { Result<UserPermission> link = UserPermission.Create(userId, id, companyId, ActorId(), time.GetUtcNow()); if (link.IsFailure) return Result.Failure<PermissionTargetView>(link.Error); store.Add(link.Value); }
        foreach (UserPermission link in links.Where(x => removed.Contains(x.PermissionId))) if (store is IRemovalStore removal) removal.Remove(link);
        if (added.Length > 0 || removed.Length > 0) user.TouchSecurity(time.GetUtcNow());
        Audit(AuditActions.UserDirectPermissionsChanged, "user", userId, companyId, AuditChangeKind.Modified, PermissionChanges(all, added, removed));
        HashSet<Guid> inherited = (await store.ListInheritedUserPermissionsAsync(userId, companyId, time.GetUtcNow(), token)).Select(x => x.Id).ToHashSet();
        HashSet<Guid> effective = user.IsActive ? desired.Concat(inherited).ToHashSet() : [];
        return Result.Success(new PermissionTargetView(user.Id, user.Username, user.Version, all.Select(p => new PermissionAssignmentView(p.Id, p.Code, desired.Contains(p.Id), inherited.Contains(p.Id), effective.Contains(p.Id))).ToArray()));
    }, ct);

    public Task<Result> GrantPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(roleId, token); Permission? permission = await store.FindPermissionAsync(permissionId, token);
        if (role is null || permission is null) return Result.Failure(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure(SecurityErrors.SystemRoleProtected);
        Result allowed = await CanDelegate(permission.Code, token); if (allowed.IsFailure) return allowed;
        if (await store.FindRolePermissionAsync(roleId, permissionId, token) is not null) return Result.Failure(SecurityErrors.Conflict);
        store.Add(RolePermission.Create(roleId, permissionId, ActorId(), time.GetUtcNow())); role.TouchPermissions(time.GetUtcNow());
        Audit(AuditActions.PermissionAssigned, "role", roleId, company.RequireCompanyId(), AuditChangeKind.Modified, new Dictionary<string, object?> { ["permissionId"] = permissionId, ["permissionCode"] = permission.Code }); return Result.Success();
    }, ct);

    public Task<Result> RevokePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Role? role = await store.FindRoleAsync(roleId, token); RolePermission? link = await store.FindRolePermissionAsync(roleId, permissionId, token); Permission? permission = await store.FindPermissionAsync(permissionId, token);
        if (role is null || link is null || permission is null) return Result.Failure(SecurityErrors.NotFound);
        if (role.IsSystem) return Result.Failure(SecurityErrors.SystemRoleProtected);
        Result allowed = await CanDelegate(permission.Code, token); if (allowed.IsFailure) return allowed;
        if (store is IRemovalStore removal) removal.Remove(link); role.TouchPermissions(time.GetUtcNow());
        Audit(AuditActions.PermissionRevoked, "role", roleId, company.RequireCompanyId(), AuditChangeKind.Modified, new Dictionary<string, object?> { ["permissionId"] = permissionId, ["permissionCode"] = permission.Code }); return Result.Success();
    }, ct);

    public Task<Result<UserCompanyView>> GrantCompanyAsync(Guid userId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!await IsActorSystemAdministrator(token)) return Result.Failure<UserCompanyView>(SecurityErrors.AccessDenied);
        if (await store.FindUserAsync(userId, token) is null || await store.FindOpenCompanyAccessAsync(userId, companyId, token) is not null) return Result.Failure<UserCompanyView>(SecurityErrors.Conflict);
        Result<UserCompany> access = UserCompany.Create(userId, companyId, time.GetUtcNow(), validTo, ActorId(), time.GetUtcNow()); if (access.IsFailure) return Result.Failure<UserCompanyView>(access.Error);
        store.Add(access.Value); Audit(AuditActions.UserCompanyGranted, "user", userId, companyId, AuditChangeKind.Added); return Result.Success(View(access.Value));
    }, ct);

    public Task<Result> RevokeCompanyAsync(Guid userId, Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!await IsActorSystemAdministrator(token)) return Result.Failure(SecurityErrors.AccessDenied);
        UserCompany? access = await store.FindOpenCompanyAccessAsync(userId, companyId, token); if (access is null) return Result.Failure(SecurityErrors.NotFound);
        if (await store.IsSystemAdministratorAsync(userId, time.GetUtcNow(), token) && await store.CountRecoveryAdministratorsAsync(time.GetUtcNow(), token) <= 1) return Result.Failure(SecurityErrors.CriticalAdministrator);
        access.End(time.GetUtcNow()); Audit(AuditActions.UserCompanyRevoked, "user", userId, companyId, AuditChangeKind.Archived); return Result.Success();
    }, ct);

    public async Task<Result<IReadOnlyList<UserCompanyView>>> ListCompaniesAsync(Guid userId, CancellationToken ct)
    {
        if (!company.IsAvailable) return Result.Failure<IReadOnlyList<UserCompanyView>>(SecurityErrors.AccessDenied);
        bool system = await IsActorSystemAdministrator(ct);
        if (!system && !await store.UserBelongsToCompanyAsync(userId, company.RequireCompanyId(), time.GetUtcNow(), ct)) return Result.Failure<IReadOnlyList<UserCompanyView>>(SecurityErrors.NotFound);
        IReadOnlyList<UserCompany> values = await store.ListCompanyAccessAsync(userId, ct);
        if (!system) values = values.Where(x => x.CompanyId == company.RequireCompanyId()).ToArray();
        return Result.Success<IReadOnlyList<UserCompanyView>>(values.Select(View).ToArray());
    }

    public Task<Result<UserRoleView>> AssignRoleAsync(Guid userId, Guid roleId, Guid companyId, DateTimeOffset? validTo, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid currentCompany = company.RequireCompanyId(); if (companyId != currentCompany) return Result.Failure<UserRoleView>(SecurityErrors.AccessDenied);
        if (!await store.UserBelongsToCompanyAsync(userId, currentCompany, time.GetUtcNow(), token)) return Result.Failure<UserRoleView>(SecurityErrors.NotFound);
        Role? role = await store.FindRoleAsync(roleId, token); if (role is not { IsActive: true }) return Result.Failure<UserRoleView>(SecurityErrors.NotFound);
        Result allowed = await CanAssignRole(role, token); if (allowed.IsFailure) return Result.Failure<UserRoleView>(allowed.Error);
        if (await store.FindOpenUserRoleAsync(userId, roleId, currentCompany, token) is not null) return Result.Failure<UserRoleView>(SecurityErrors.Conflict);
        Result<UserRole> assignment = UserRole.Create(userId, roleId, currentCompany, time.GetUtcNow(), validTo, ActorId(), time.GetUtcNow()); if (assignment.IsFailure) return Result.Failure<UserRoleView>(assignment.Error);
        store.Add(assignment.Value); ApplicationUser? user = await store.FindUserAsync(userId, token); user?.TouchSecurity(time.GetUtcNow());
        Audit(AuditActions.UserRoleAssigned, "user", userId, currentCompany, AuditChangeKind.Added, new Dictionary<string, object?> { ["roleId"] = roleId, ["roleCode"] = role.Code }); return Result.Success(View(assignment.Value));
    }, ct);

    public Task<Result> RevokeRoleAsync(Guid userId, Guid roleId, Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        Guid currentCompany = company.RequireCompanyId(); if (companyId != currentCompany) return Result.Failure(SecurityErrors.AccessDenied);
        UserRole? link = await store.FindOpenUserRoleAsync(userId, roleId, currentCompany, token); Role? role = await store.FindRoleAsync(roleId, token);
        if (link is null || role is null) return Result.Failure(SecurityErrors.NotFound);
        Result allowed = await CanAssignRole(role, token); if (allowed.IsFailure) return allowed;
        if (role.Code == "system.administrator" && await store.CountRecoveryAdministratorsAsync(time.GetUtcNow(), token) <= 1) return Result.Failure(SecurityErrors.CriticalAdministrator);
        link.End(time.GetUtcNow()); ApplicationUser? user = await store.FindUserAsync(userId, token); user?.TouchSecurity(time.GetUtcNow());
        Audit(AuditActions.UserRoleRevoked, "user", userId, currentCompany, AuditChangeKind.Archived, new Dictionary<string, object?> { ["roleId"] = roleId, ["roleCode"] = role.Code }); return Result.Success();
    }, ct);

    public async Task<Result<IReadOnlyList<UserRoleView>>> ListUserRolesAsync(Guid userId, CancellationToken ct)
    {
        if (!company.IsAvailable || !await store.UserBelongsToCompanyAsync(userId, company.RequireCompanyId(), time.GetUtcNow(), ct)) return Result.Failure<IReadOnlyList<UserRoleView>>(SecurityErrors.NotFound);
        return Result.Success<IReadOnlyList<UserRoleView>>((await store.ListUserRolesAsync(userId, ct)).Where(x => x.CompanyId == company.RequireCompanyId()).Select(View).ToArray());
    }

    public async Task<Result<LoginResult>> LoginAsync(string username, string password, CancellationToken ct)
    {
        string normalized = username.Trim().ToUpperInvariant();
        ApplicationUser? user = await store.FindUserByUsernameAsync(normalized, ct);
        if (user is null)
        {
            passwordCompatibility.Verify(password, null);
            await RecordFailedLogin(normalized, ct);
            return Result.Failure<LoginResult>(SecurityErrors.InvalidCredentials);
        }
        Result<LoginAttempt> attempt = await transactions.ExecuteAsync(async token =>
        {
            Result<PasswordAuthenticationOutcome> checkedPassword = await identity.CheckPasswordAsync(user, password, token);
            if (checkedPassword.IsFailure || checkedPassword.Value != PasswordAuthenticationOutcome.Success)
            {
                Audit(AuditActions.LoginFailed, "authentication", user.Id, outcome: AuditOutcome.Failure, failureCode: SecurityErrors.InvalidCredentials.Code, actorUsername: normalized);
                return Result.Success(new LoginAttempt(null));
            }
            user.RecordLogin(time.GetUtcNow());
            (string raw, string hash) = tokens.Create(); DateTimeOffset expires = time.GetUtcNow().AddMinutes(30);
            store.Add(AuthenticationSession.Create(user.Id, hash, time.GetUtcNow(), expires));
            Audit(AuditActions.LoginSucceeded, "user", user.Id, actorUsername: user.Username, actorUserId: user.Id, actorType: ActorType.InternalUser.ToString());
            return Result.Success(new LoginAttempt(new LoginResult(raw, expires, View(user))));
        }, ct);
        return attempt.IsSuccess && attempt.Value.Login is not null ? Result.Success(attempt.Value.Login) : Result.Failure<LoginResult>(SecurityErrors.InvalidCredentials);
    }

    public Task<Result> LogoutAsync(string tokenHash, CancellationToken ct) => transactions.ExecuteAsync(async token => { AuthenticationSession? session = await store.FindSessionAsync(tokenHash, time.GetUtcNow(), token); if (session is null) return Result.Failure(SecurityErrors.NotFound); session.Revoke(time.GetUtcNow()); Audit(AuditActions.Logout, "session", session.Id); Audit(AuditActions.SessionRevoked, "session", session.Id); return Result.Success(); }, ct);

    public Task<Result<CurrentUserView>> GetCurrentUserAsync(CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure<CurrentUserView>(SecurityErrors.UserContextUnavailable);
        ApplicationUser? user = await store.FindUserAsync(userId, token); if (user is not { IsActive: true }) return Result.Failure<CurrentUserView>(SecurityErrors.UserContextUnavailable);
        Guid? companyId = company.CompanyId; IReadOnlyList<string> roles = companyId is { } selected ? await store.ListEffectiveRoleCodesAsync(userId, selected, time.GetUtcNow(), token) : []; IReadOnlyList<string> permissions = companyId is { } permissionCompany ? await store.ListEffectivePermissionCodesAsync(userId, permissionCompany, time.GetUtcNow(), token) : []; bool system = await store.IsSystemAdministratorAsync(userId, time.GetUtcNow(), token);
        string displayName = user.PersonId is { } personId ? await store.FindLinkedPersonDisplayNameAsync(personId, token) ?? user.DisplayName : user.DisplayName;
        return Result.Success(new CurrentUserView(user.Id, user.Username, displayName, user.AccountState, companyId, roles, permissions, system));
    }, ct);
    public Task<Result<IReadOnlyList<MyCompanyView>>> ListMyCompaniesAsync(CancellationToken ct) => transactions.ExecuteAsync(async token => !TryGetCurrentUserId(out Guid userId) ? Result.Failure<IReadOnlyList<MyCompanyView>>(SecurityErrors.UserContextUnavailable) : Result.Success(await store.ListEffectiveCompaniesAsync(userId, time.GetUtcNow(), token)), ct);
    public Task<Result<CompanySelectionView>> SelectMyCompanyAsync(Guid companyId, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (!TryGetCurrentUserId(out Guid userId)) return Result.Failure<CompanySelectionView>(SecurityErrors.UserContextUnavailable); DateTimeOffset now = time.GetUtcNow();
        if (!await store.HasEffectiveCompanyAccessAsync(userId, companyId, now, token)) return Result.Failure<CompanySelectionView>(SecurityErrors.CompanyNotAuthorized); if (!await store.CompanyIsActiveAsync(companyId, token)) return Result.Failure<CompanySelectionView>(SecurityErrors.CompanyInactive);
        MyCompanyView selected = (await store.FindEffectiveCompanyAsync(userId, companyId, now, token))!; IReadOnlyList<string> roles = await store.ListEffectiveRoleCodesAsync(userId, companyId, now, token); IReadOnlyList<string> permissions = await store.ListEffectivePermissionCodesAsync(userId, companyId, now, token); Audit(AuditActions.CompanySelected, "company", companyId, companyId);
        return Result.Success(new CompanySelectionView(selected.CompanyId, selected.Code, selected.DisplayName, roles, permissions));
    }, ct);

    public Task<Result<UserView>> BootstrapAsync(Guid companyId, CreateUserCommand command, CancellationToken ct) => transactions.ExecuteAsync(async token =>
    {
        if (await store.AnyUsersAsync(token)) return Result.Failure<UserView>(SecurityErrors.Conflict);
        string[] names = command.DisplayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries); string first = names.Length > 0 ? names[0] : command.Username; string last = names.Length > 1 ? names[1] : "Administrator";
        string personnelCode = await chart.AllocateNextPersonnelCodeAsync(companyId, token);
        Result<Person> person = Person.Create(Guid.NewGuid(), companyId, personnelCode, first, last, null, null, command.Email, null, null, "Bootstrap identity", time.GetUtcNow()); if (person.IsFailure) return Result.Failure<UserView>(person.Error); chart.Add(person.Value);
        Result<ApplicationUser> made = ApplicationUser.CreateIdentity(Guid.NewGuid(), person.Value.Id, command.Username, person.Value.DisplayName, person.Value.Email, time.GetUtcNow()); if (made.IsFailure) return Result.Failure<UserView>(made.Error);
        Result identityResult = await identity.CreateAsync(made.Value, command.Password, token); if (identityResult.IsFailure) return Result.Failure<UserView>(identityResult.Error);
        Role role = Role.Create(Guid.NewGuid(), "system.administrator", "System administrator", "Built-in recovery-capable administrator", RoleScope.System, true, time.GetUtcNow()).Value; store.Add(role);
        foreach (Permission permission in await store.ListPermissionsAsync(token)) store.Add(RolePermission.Create(role.Id, permission.Id, made.Value.Id, time.GetUtcNow()));
        store.Add(UserCompany.Create(made.Value.Id, companyId, time.GetUtcNow(), null, made.Value.Id, time.GetUtcNow()).Value); store.Add(UserRole.Create(made.Value.Id, role.Id, companyId, time.GetUtcNow(), null, made.Value.Id, time.GetUtcNow()).Value);
        Audit(AuditActions.BootstrapCompleted, "user", made.Value.Id, companyId, AuditChangeKind.Added, actorUsername: made.Value.Username); return Result.Success(View(made.Value));
    }, ct);

    public Task<Result> AuthorizeAsync(string permissionCode, CancellationToken ct)
    {
        if (!actor.Actor.IsAuthenticated) return Task.FromResult(Result.Failure(Error.Authentication("security.authentication.required"))); if (!company.IsAvailable) return Task.FromResult(Result.Failure(SecurityErrors.AccessDenied));
        if (actor.Actor.ActorType == ActorType.ApiClient && Guid.TryParse(actor.Actor.ApiClientId, out Guid clientId)) return transactions.ExecuteAsync(async token => await store.ApiClientHasCompanyAsync(clientId, company.RequireCompanyId(), token) && await store.ApiClientHasPermissionAsync(clientId, permissionCode, token) ? Result.Success() : Result.Failure(SecurityErrors.AccessDenied), ct);
        if (!Guid.TryParse(actor.Actor.UserId, out Guid userId)) return Task.FromResult(Result.Failure(Error.Authentication("security.authentication.required")));
        return transactions.ExecuteAsync(async token => await store.UserHasPermissionAsync(userId, company.RequireCompanyId(), permissionCode, time.GetUtcNow(), token) ? Result.Success() : Result.Failure(SecurityErrors.AccessDenied), ct);
    }

    private async Task<Result<ApplicationUser>> FindScopedUser(Guid id, CancellationToken ct)
    {
        Guid companyId = company.RequireCompanyId();
        if (!await store.UserBelongsToCompanyAsync(id, companyId, time.GetUtcNow(), ct)) return Result.Failure<ApplicationUser>(SecurityErrors.NotFound);
        ApplicationUser? user = await store.FindUserAsync(id, ct); return user is null ? Result.Failure<ApplicationUser>(SecurityErrors.NotFound) : Result.Success(user);
    }

    private async Task<PersonSummaryView> PersonSummary(ApplicationUser user, CancellationToken ct)
    {
        Person? person = user.PersonId is { } id ? await chart.FindPersonAsync(id, ct) : null;
        return person is null ? new PersonSummaryView(user.PersonId ?? user.Id, user.Username, user.DisplayName, null, user.Email, true, null, null, null) : await PersonSummary(person, ct);
    }

    private async Task<PersonSummaryView> PersonSummary(Person person, CancellationToken ct)
    {
        OrganizationalAssignment? placement = (await chart.ListAssignmentsAsync(person.Id, ct)).Where(x => x.IsActive).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.PositionTitle).FirstOrDefault();
        OrganizationalUnit? unit = placement is null ? null : await organization.FindUnitAsync(placement.OrganizationalUnitId, ct);
        Branch? branch = unit?.BranchId is { } branchId ? await organization.FindBranchAsync(branchId, ct) : null;
        return new PersonSummaryView(person.Id, person.PersonnelCode, person.DisplayName, person.Phone, person.Email, person.IsActive, placement?.PositionTitle, unit?.DisplayName, branch?.DisplayName);
    }

    private async Task<PermissionTargetView> UserPermissionTarget(ApplicationUser user, CancellationToken ct)
    {
        Guid companyId = company.RequireCompanyId(); IReadOnlyList<Permission> all = await store.ListPermissionsAsync(ct); HashSet<Guid> direct = (await store.ListDirectUserPermissionsAsync(user.Id, companyId, ct)).Select(x => x.Id).ToHashSet(); HashSet<Guid> inherited = (await store.ListInheritedUserPermissionsAsync(user.Id, companyId, time.GetUtcNow(), ct)).Select(x => x.Id).ToHashSet(); HashSet<string> effective = (await store.ListEffectivePermissionCodesAsync(user.Id, companyId, time.GetUtcNow(), ct)).ToHashSet(StringComparer.Ordinal);
        return new PermissionTargetView(user.Id, user.Username, user.Version, all.Select(p => new PermissionAssignmentView(p.Id, p.Code, direct.Contains(p.Id), inherited.Contains(p.Id), effective.Contains(p.Code))).ToArray());
    }
    private static PermissionTargetView RolePermissionTarget(Role role, IReadOnlyList<Permission> all, HashSet<Guid> assigned) => new(role.Id, role.DisplayTitle, role.Version, all.Select(p => new PermissionAssignmentView(p.Id, p.Code, assigned.Contains(p.Id), false, assigned.Contains(p.Id))).ToArray());
    private static Dictionary<string, object?> PermissionChanges(IReadOnlyList<Permission> all, Guid[] added, Guid[] removed) => new() { ["added"] = all.Where(x => added.Contains(x.Id)).Select(x => x.Code).ToArray(), ["removed"] = all.Where(x => removed.Contains(x.Id)).Select(x => x.Code).ToArray() };
    private async Task<Result> CanAssignRole(Role role, CancellationToken ct)
    {
        if (await IsActorSystemAdministrator(ct)) return Result.Success(); if (role.IsSystem) return Result.Failure(SecurityErrors.PrivilegeEscalation);
        foreach (Permission permission in await store.ListRolePermissionsAsync(role.Id, ct)) { Result result = await CanDelegate(permission.Code, ct); if (result.IsFailure) return result; }
        return Result.Success();
    }
    private async Task<Result> CanDelegate(string permissionCode, CancellationToken ct)
    {
        if (await IsActorSystemAdministrator(ct)) return Result.Success();
        return Guid.TryParse(actor.Actor.UserId, out Guid actorId) && await store.UserHasPermissionAsync(actorId, company.RequireCompanyId(), permissionCode, time.GetUtcNow(), ct) ? Result.Success() : Result.Failure(SecurityErrors.PrivilegeEscalation);
    }
    private Task<bool> IsActorSystemAdministrator(CancellationToken ct) => Guid.TryParse(actor.Actor.UserId, out Guid id) ? store.IsSystemAdministratorAsync(id, time.GetUtcNow(), ct) : Task.FromResult(false);
    private int RevokeSessions(IReadOnlyList<AuthenticationSession> sessions) { DateTimeOffset now = time.GetUtcNow(); foreach (AuthenticationSession session in sessions) session.Revoke(now); return sessions.Count; }
    private async Task RecordFailedLogin(string normalized, CancellationToken ct) => _ = await transactions.ExecuteAsync(_ => { Audit(AuditActions.LoginFailed, "authentication", null, outcome: AuditOutcome.Failure, failureCode: SecurityErrors.InvalidCredentials.Code, actorUsername: normalized); return Task.FromResult(Result.Success()); }, ct);
    private void Audit(string action, string entityType, Guid? entityId, Guid? companyId = null, AuditChangeKind? changeKind = null, IReadOnlyDictionary<string, object?>? metadata = null, AuditOutcome outcome = AuditOutcome.Success, string? failureCode = null, string? actorUsername = null, Guid? actorUserId = null, string? actorType = null) => audit.Write(new AuditWriteRequest(action, entityType, entityId?.ToString(), outcome, failureCode, companyId, actorUsername, changeKind, metadata, ActorUserId: actorUserId, ActorType: actorType));
    private Guid? ActorId() => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : null;
    private bool TryGetCurrentUserId(out Guid userId) { userId = Guid.Empty; return actor.Actor.IsAuthenticated && Guid.TryParse(actor.Actor.UserId, out userId); }
    private static UserView View(ApplicationUser x) => new(x.Id, x.Username, x.Email, x.DisplayName, x.AccountState, x.CreatedAtUtc, x.UpdatedAtUtc, x.Version, x.PersonId, x.LockoutEndUtc, x.AccessFailedCount, x.LastLoginAtUtc);
    private static RoleView View(Role x) => new(x.Id, x.Code, x.DisplayTitle, x.IsSystem, x.IsActive, x.Version, x.Description, x.Scope);
    private static PermissionView View(Permission x) { SecurityPermissionDefinition definition = SecurityPermissions.Catalog.FirstOrDefault(d => d.Code == x.Code) ?? SecurityPermissionDefinition.FromCode(x.Code); return new(x.Id, x.Code, x.TechnicalDescription, x.Module, definition.FeaturePath, definition.Action, definition.Capability); }
    private static UserCompanyView View(UserCompany x) => new(x.Id, x.UserId, x.CompanyId, x.ValidFromUtc, x.ValidToUtc);
    private static UserRoleView View(UserRole x) => new(x.Id, x.UserId, x.RoleId, x.CompanyId, x.ValidFromUtc, x.ValidToUtc);
    private sealed record LoginAttempt(LoginResult? Login);
}
