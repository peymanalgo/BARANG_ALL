using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1725
namespace BARANG.Core.Infrastructure.Security;

public sealed class SecurityStore(CoreDbContext db, ICompanyContext company, TimeProvider time) : ISecurityStore, IRemovalStore
{
    public void Add(ApplicationUser x) => db.ApplicationUsers.Add(x);
    public void Add(Role x) => db.Roles.Add(x);
    public void Add(RolePermission x) => db.RolePermissions.Add(x);
    public void Add(UserPermission x) => db.UserPermissions.Add(x);
    public void Add(UserCompany x) => db.UserCompanies.Add(x);
    public void Add(UserRole x) => db.UserRoles.Add(x);
    public void Add(AuthenticationSession x) => db.AuthenticationSessions.Add(x);
    public void Remove(Role x) => db.Roles.Remove(x);
    public void Remove(RolePermission x) => db.RolePermissions.Remove(x);
    public void Remove(UserPermission x) => db.UserPermissions.Remove(x);

    public Task<ApplicationUser?> FindUserAsync(Guid id, CancellationToken ct) => db.ApplicationUsers.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<ApplicationUser?> FindUserByPersonAsync(Guid personId, CancellationToken ct) => db.ApplicationUsers.SingleOrDefaultAsync(x => x.PersonId == personId, ct);
    public Task<string?> FindLinkedPersonDisplayNameAsync(Guid personId, CancellationToken ct) => db.Persons.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == personId).Select(x => x.DisplayName).SingleOrDefaultAsync(ct);
    public Task<ApplicationUser?> FindUserByUsernameAsync(string username, CancellationToken ct)
    {
        string normalized = username.Trim().ToUpperInvariant();
        return db.ApplicationUsers.SingleOrDefaultAsync(x => x.Username == normalized, ct);
    }
    public async Task<IReadOnlyList<ApplicationUser>> ListUsersAsync(CancellationToken ct)
    {
        if (!company.IsAvailable) return [];
        PagedResult<ApplicationUser> page = await SearchUsersAsync(new SecurityUserQuery(PageSize: 100), company.RequireCompanyId(), time.GetUtcNow(), ct);
        return page.Items.ToArray();
    }
    public async Task<PagedResult<ApplicationUser>> SearchUsersAsync(SecurityUserQuery query, Guid companyId, DateTimeOffset now, CancellationToken ct)
    {
        ValidatePage(query.PageNumber, query.PageSize);
        IQueryable<ApplicationUser> users =
            (from user in db.ApplicationUsers.AsNoTracking()
             join access in db.UserCompanies.IgnoreQueryFilters().AsNoTracking() on user.Id equals access.UserId
             where access.CompanyId == companyId && access.ValidFromUtc <= now && (access.ValidToUtc == null || access.ValidToUtc > now)
             select user).Distinct();

        if (query.AccountState is { } state) users = users.Where(x => x.AccountState == state);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            IQueryable<Guid> people = db.Persons.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.CompanyId == companyId && (EF.Functions.ILike(x.DisplayName, $"%{term}%") || EF.Functions.ILike(x.PersonnelCode, $"%{term}%") || x.Phone != null && EF.Functions.ILike(x.Phone, $"%{term}%")))
                .Select(x => x.Id);
            users = users.Where(x => EF.Functions.ILike(x.Username, $"%{term}%") || x.PersonId != null && people.Contains(x.PersonId.Value));
        }

        long total = await users.LongCountAsync(ct);
        users = query.SortBy.ToLowerInvariant() switch
        {
            "username" => query.Descending ? users.OrderByDescending(x => x.Username).ThenByDescending(x => x.Id) : users.OrderBy(x => x.Username).ThenBy(x => x.Id),
            "status" => query.Descending ? users.OrderByDescending(x => x.AccountState).ThenByDescending(x => x.Username) : users.OrderBy(x => x.AccountState).ThenBy(x => x.Username),
            "lastlogin" => query.Descending ? users.OrderByDescending(x => x.LastLoginAtUtc).ThenByDescending(x => x.Username) : users.OrderBy(x => x.LastLoginAtUtc).ThenBy(x => x.Username),
            _ => query.Descending
                ? users.OrderByDescending(x => db.Persons.IgnoreQueryFilters().Where(p => p.Id == x.PersonId && p.CompanyId == companyId).Select(p => p.DisplayName).FirstOrDefault()).ThenByDescending(x => x.Username)
                : users.OrderBy(x => db.Persons.IgnoreQueryFilters().Where(p => p.Id == x.PersonId && p.CompanyId == companyId).Select(p => p.DisplayName).FirstOrDefault()).ThenBy(x => x.Username)
        };
        ApplicationUser[] items = await users.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArrayAsync(ct);
        return new PagedResult<ApplicationUser>(items, query.PageNumber, query.PageSize, total);
    }
    public Task<bool> UserBelongsToCompanyAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => db.UserCompanies.IgnoreQueryFilters().AsNoTracking().AnyAsync(x => x.UserId == userId && x.CompanyId == companyId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct);
    public Task<bool> UsernameExistsAsync(string username, Guid? excludingUserId, CancellationToken ct) { string normalized = username.Trim().ToUpperInvariant(); return db.ApplicationUsers.AnyAsync(x => x.Username == normalized && (excludingUserId == null || x.Id != excludingUserId), ct); }

    public Task<Role?> FindRoleAsync(Guid id, CancellationToken ct) => db.Roles.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken ct) => await db.Roles.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public async Task<PagedResult<RoleView>> SearchRolesAsync(SecurityRoleQuery query, Guid companyId, DateTimeOffset now, CancellationToken ct)
    {
        ValidatePage(query.PageNumber, query.PageSize);
        IQueryable<Role> roles = db.Roles.AsNoTracking();
        if (query.IsActive is { } active) roles = roles.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(query.Search)) { string term = query.Search.Trim(); roles = roles.Where(x => EF.Functions.ILike(x.Code, $"%{term}%") || EF.Functions.ILike(x.DisplayTitle, $"%{term}%") || x.Description != null && EF.Functions.ILike(x.Description, $"%{term}%")); }
        long total = await roles.LongCountAsync(ct);
        roles = query.SortBy.ToLowerInvariant() switch
        {
            "code" => query.Descending ? roles.OrderByDescending(x => x.Code) : roles.OrderBy(x => x.Code),
            "status" => query.Descending ? roles.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.DisplayTitle) : roles.OrderBy(x => x.IsActive).ThenBy(x => x.DisplayTitle),
            "users" => query.Descending
                ? roles.OrderByDescending(r => db.UserRoles.IgnoreQueryFilters().Count(ur => ur.RoleId == r.Id && ur.CompanyId == companyId && ur.ValidFromUtc <= now && (ur.ValidToUtc == null || ur.ValidToUtc > now)))
                : roles.OrderBy(r => db.UserRoles.IgnoreQueryFilters().Count(ur => ur.RoleId == r.Id && ur.CompanyId == companyId && ur.ValidFromUtc <= now && (ur.ValidToUtc == null || ur.ValidToUtc > now))),
            _ => query.Descending ? roles.OrderByDescending(x => x.DisplayTitle).ThenByDescending(x => x.Code) : roles.OrderBy(x => x.DisplayTitle).ThenBy(x => x.Code)
        };
        Role[] entities = await roles.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArrayAsync(ct);
        List<RoleView> items = [];
        foreach (Role role in entities)
        {
            int count = await db.UserRoles.IgnoreQueryFilters().AsNoTracking().CountAsync(x => x.RoleId == role.Id && x.CompanyId == companyId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct);
            items.Add(new RoleView(role.Id, role.Code, role.DisplayTitle, role.IsSystem, role.IsActive, role.Version, role.Description, role.Scope, count));
        }
        return new PagedResult<RoleView>(items, query.PageNumber, query.PageSize, total);
    }
    public Task<bool> RoleCodeExistsAsync(string code, CancellationToken ct)
    {
        string normalized = code.Trim().ToLowerInvariant();
        return db.Roles.AnyAsync(x => x.Code == normalized, ct);
    }
    public Task<bool> RoleHasAssignmentsAsync(Guid roleId, CancellationToken ct) => db.UserRoles.IgnoreQueryFilters().AnyAsync(x => x.RoleId == roleId, ct);

    public Task<Permission?> FindPermissionAsync(Guid id, CancellationToken ct) => db.Permissions.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken ct) => await db.Permissions.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
    public async Task<IReadOnlyList<Permission>> ListRolePermissionsAsync(Guid roleId, CancellationToken ct) => await (from link in db.RolePermissions.AsNoTracking() join permission in db.Permissions.AsNoTracking() on link.PermissionId equals permission.Id where link.RoleId == roleId orderby permission.Code select permission).ToListAsync(ct);
    public async Task<IReadOnlyList<RolePermission>> ListRolePermissionLinksAsync(Guid roleId, CancellationToken ct) => await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
    public async Task<IReadOnlyList<Permission>> ListDirectUserPermissionsAsync(Guid userId, Guid companyId, CancellationToken ct) => await (from link in db.UserPermissions.IgnoreQueryFilters().AsNoTracking() join permission in db.Permissions.AsNoTracking() on link.PermissionId equals permission.Id where link.UserId == userId && link.CompanyId == companyId orderby permission.Code select permission).ToListAsync(ct);
    public async Task<IReadOnlyList<Permission>> ListInheritedUserPermissionsAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => await InheritedPermissions(userId, companyId, now).OrderBy(x => x.Code).ToListAsync(ct);
    public async Task<IReadOnlyList<UserPermission>> ListUserPermissionLinksAsync(Guid userId, Guid companyId, CancellationToken ct) => await db.UserPermissions.IgnoreQueryFilters().Where(x => x.UserId == userId && x.CompanyId == companyId).ToListAsync(ct);

    public Task<UserCompany?> FindEffectiveCompanyAccessAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => db.UserCompanies.SingleOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct);
    public async Task<IReadOnlyList<UserCompany>> ListCompanyAccessAsync(Guid userId, CancellationToken ct) => await db.UserCompanies.IgnoreQueryFilters().AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct);
    public Task<UserCompany?> FindOpenCompanyAccessAsync(Guid userId, Guid companyId, CancellationToken ct) => db.UserCompanies.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId && x.ValidToUtc == null, ct);
    public Task<UserRole?> FindOpenUserRoleAsync(Guid userId, Guid roleId, Guid companyId, CancellationToken ct) => db.UserRoles.SingleOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId && x.CompanyId == companyId && x.ValidToUtc == null, ct);
    public async Task<IReadOnlyList<UserRole>> ListUserRolesAsync(Guid userId, CancellationToken ct) => await db.UserRoles.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct);
    public async Task<IReadOnlyList<UserRoleDetailView>> ListEffectiveUserRoleDetailsAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => await (from link in db.UserRoles.IgnoreQueryFilters().AsNoTracking() join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id where link.UserId == userId && link.CompanyId == companyId && link.ValidFromUtc <= now && (link.ValidToUtc == null || link.ValidToUtc > now) orderby role.DisplayTitle select new UserRoleDetailView(link.Id, role.Id, role.Code, role.DisplayTitle, role.Scope, link.ValidFromUtc, link.ValidToUtc)).ToListAsync(ct);
    public Task<RolePermission?> FindRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct) => db.RolePermissions.SingleOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, ct);
    public Task<bool> UserHasPermissionAsync(Guid userId, Guid companyId, string permission, DateTimeOffset now, CancellationToken ct) => EffectivePermissionCodes(userId, companyId, now).AnyAsync(x => x == permission, ct);

    public Task<AuthenticationSession?> FindSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken ct) => db.AuthenticationSessions.SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > now, ct);
    public async Task<IReadOnlyList<AuthenticationSession>> ListActiveSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken ct) => await db.AuthenticationSessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now).ToListAsync(ct);
    public Task<bool> AnyUsersAsync(CancellationToken ct) => db.ApplicationUsers.AnyAsync(ct);
    public async Task<IReadOnlyList<MyCompanyView>> ListEffectiveCompaniesAsync(Guid userId, DateTimeOffset now, CancellationToken ct) =>
        await EligibleCompanies(userId, now).OrderBy(x => x.Code).Select(x => new MyCompanyView(x.Id, x.Code, x.DisplayName)).ToListAsync(ct);
    public Task<MyCompanyView?> FindEffectiveCompanyAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) =>
        EligibleCompanies(userId, now).Where(x => x.Id == companyId).Select(x => new MyCompanyView(x.Id, x.Code, x.DisplayName)).SingleOrDefaultAsync(ct);
    public Task<bool> HasEffectiveCompanyAccessAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) =>
        EligibleCompanies(userId, now, activeOnly: false).AnyAsync(x => x.Id == companyId, ct);
    public Task<bool> CompanyIsActiveAsync(Guid companyId, CancellationToken ct) => db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive, ct);
    public async Task<IReadOnlyList<string>> ListEffectiveRoleCodesAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => await (from assignment in db.UserRoles.IgnoreQueryFilters().AsNoTracking() join role in db.Roles.AsNoTracking() on assignment.RoleId equals role.Id join user in db.ApplicationUsers.AsNoTracking() on assignment.UserId equals user.Id join access in db.UserCompanies.IgnoreQueryFilters().AsNoTracking() on new { assignment.UserId, assignment.CompanyId } equals new { access.UserId, access.CompanyId } where assignment.UserId == userId && assignment.CompanyId == companyId && user.AccountState == UserAccountState.Active && assignment.ValidFromUtc <= now && (assignment.ValidToUtc == null || assignment.ValidToUtc > now) && access.ValidFromUtc <= now && (access.ValidToUtc == null || access.ValidToUtc > now) && role.IsActive orderby role.Code select role.Code).Union(db.Roles.AsNoTracking().Where(r => r.Code == "system.administrator" && r.IsActive && RecoveryAdministrators(now).Contains(userId)).Select(r => r.Code)).Distinct().ToListAsync(ct);
    public async Task<IReadOnlyList<string>> ListEffectivePermissionCodesAsync(Guid userId, Guid companyId, DateTimeOffset now, CancellationToken ct) => await EffectivePermissionCodes(userId, companyId, now).OrderBy(x => x).ToListAsync(ct);
    public Task<bool> IsSystemAdministratorAsync(Guid userId, DateTimeOffset now, CancellationToken ct) => RecoveryAdministrators(now).AnyAsync(x => x == userId, ct);
    public Task<int> CountRecoveryAdministratorsAsync(DateTimeOffset now, CancellationToken ct) => RecoveryAdministrators(now).Distinct().CountAsync(ct);
    public Task<bool> ApiClientHasCompanyAsync(Guid clientId, Guid companyId, CancellationToken ct) => db.ApiClientCompanies.AsNoTracking().AnyAsync(x => x.ApiClientId == clientId && x.CompanyId == companyId, ct);
    public Task<bool> ApiClientHasPermissionAsync(Guid clientId, string permission, CancellationToken ct) => (from link in db.ApiClientPermissions.AsNoTracking() join p in db.Permissions.AsNoTracking() on link.PermissionId equals p.Id where link.ApiClientId == clientId && p.Code == permission select link).AnyAsync(ct);

    private IQueryable<Permission> InheritedPermissions(Guid userId, Guid companyId, DateTimeOffset now) =>
        (from assignment in db.UserRoles.IgnoreQueryFilters().AsNoTracking()
         join role in db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
         join link in db.RolePermissions.AsNoTracking() on role.Id equals link.RoleId
         join permission in db.Permissions.AsNoTracking() on link.PermissionId equals permission.Id
         where assignment.UserId == userId && assignment.CompanyId == companyId && assignment.ValidFromUtc <= now && (assignment.ValidToUtc == null || assignment.ValidToUtc > now) && role.IsActive
         select permission).Distinct();

    private IQueryable<string> EffectivePermissionCodes(Guid userId, Guid companyId, DateTimeOffset now)
    {
        IQueryable<Guid> eligibleUsers =
            from user in db.ApplicationUsers.AsNoTracking()
            join access in db.UserCompanies.IgnoreQueryFilters().AsNoTracking() on user.Id equals access.UserId
            where user.Id == userId && user.AccountState == UserAccountState.Active && access.CompanyId == companyId && access.ValidFromUtc <= now && (access.ValidToUtc == null || access.ValidToUtc > now)
            select user.Id;
        IQueryable<string> inherited = from permission in InheritedPermissions(userId, companyId, now) where eligibleUsers.Contains(userId) select permission.Code;
        IQueryable<string> direct = from link in db.UserPermissions.IgnoreQueryFilters().AsNoTracking() join permission in db.Permissions.AsNoTracking() on link.PermissionId equals permission.Id where link.UserId == userId && link.CompanyId == companyId && eligibleUsers.Contains(userId) select permission.Code;
        IQueryable<Guid> administrators = RecoveryAdministrators(now);
        IQueryable<string> system = from role in db.Roles.AsNoTracking()
            join grant in db.RolePermissions.AsNoTracking() on role.Id equals grant.RoleId
            join permission in db.Permissions.AsNoTracking() on grant.PermissionId equals permission.Id
            where role.Code == "system.administrator" && role.IsActive
                && administrators.Contains(userId) && db.Companies.Any(x => x.Id == companyId && x.IsActive)
            select permission.Code;
        return inherited.Union(direct).Union(system).Distinct();
    }

    private IQueryable<BARANG.Core.Domain.Organization.Company> EligibleCompanies(Guid userId, DateTimeOffset now, bool activeOnly = true)
    {
        IQueryable<Guid> activeUsers = db.ApplicationUsers.AsNoTracking().Where(x => x.Id == userId && x.AccountState == UserAccountState.Active).Select(x => x.Id);
        IQueryable<Guid> memberships = db.UserCompanies.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.UserId == userId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now)).Select(x => x.CompanyId);
        IQueryable<Guid> administrators = RecoveryAdministrators(now);
        return db.Companies.AsNoTracking().Where(x => (!activeOnly || x.IsActive) && activeUsers.Contains(userId) && (memberships.Contains(x.Id) || administrators.Contains(userId)));
    }

    private IQueryable<Guid> RecoveryAdministrators(DateTimeOffset now) =>
        from user in db.ApplicationUsers.AsNoTracking()
        join assignment in db.UserRoles.IgnoreQueryFilters().AsNoTracking() on user.Id equals assignment.UserId
        join access in db.UserCompanies.IgnoreQueryFilters().AsNoTracking() on new { assignment.UserId, assignment.CompanyId } equals new { access.UserId, access.CompanyId }
        join role in db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
        where user.AccountState == UserAccountState.Active && assignment.ValidFromUtc <= now && (assignment.ValidToUtc == null || assignment.ValidToUtc > now) && access.ValidFromUtc <= now && (access.ValidToUtc == null || access.ValidToUtc > now) && role.IsActive && role.Code == "system.administrator"
        select user.Id;

    private static void ValidatePage(int page, int size) { _ = new PagedRequest(page, size); }
}

public sealed class RequestIdentityResolver(ISecurityStore store, ISessionTokenService tokens, TimeProvider time) : IRequestIdentityResolver
{
    public async Task<ActorContext?> ResolveAsync(string accessToken, CancellationToken ct)
    {
        AuthenticationSession? session = await store.FindSessionAsync(tokens.Hash(accessToken), time.GetUtcNow(), ct);
        if (session is null) return null;
        ApplicationUser? user = await store.FindUserAsync(session.UserId, ct);
        return user is { IsActive: true } && (user.LockoutEndUtc is null || user.LockoutEndUtc <= time.GetUtcNow())
            ? new ActorContext(user.Id.ToString(), ActorType.InternalUser, true, SourceChannel.Backoffice, user.Id.ToString(), actorUsername: user.Username)
            : null;
    }
}
#pragma warning restore CA1725
