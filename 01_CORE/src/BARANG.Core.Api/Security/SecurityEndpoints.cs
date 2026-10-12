using System.Security.Cryptography;
using System.Text;
using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Pagination;
using BARANG.Core.Application.Organization;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Api.Security;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder auth = endpoints.MapGroup("/api/v1/auth");
        auth.MapPost("/login", async (LoginRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.LoginAsync(r.Username, r.Password, ct))).AllowAnonymous().RequireRateLimiting("login");
        auth.MapPost("/logout", async (HttpContext h, ISecurityAdministration s, ISessionTokenService tokens, CancellationToken ct) => { string raw = h.Request.Headers.Authorization.ToString()[7..].Trim(); return Map(await s.LogoutAsync(tokens.Hash(raw), ct)); }).RequireAuthorization();
        auth.MapGet("/me", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.GetCurrentUserAsync(ct))).RequireAuthorization();
        auth.MapGet("/me/companies", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListMyCompaniesAsync(ct))).RequireAuthorization();
        auth.MapPost("/me/company", async (CompanySelectionRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SelectMyCompanyAsync(r.CompanyId, ct))).RequireAuthorization();
        endpoints.MapPost("/api/v1/security/bootstrap", BootstrapAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/security/development-admin/recover", RecoverDevelopmentAdminAsync).AllowAnonymous();

        RouteGroupBuilder users = endpoints.MapGroup("/api/v1/security/users");
        users.MapGet("/", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListUsersAsync(ct))).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapGet("/search", SearchUsersAsync).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapGet("/person-candidates", async (string? search, int? take, ISecurityAdministration s, CancellationToken ct) => Map(await s.SearchPersonCandidatesAsync(search, take ?? 30, ct))).RequireAuthorization(SecurityPermissions.SecurityUserCreate);
        users.MapPost("/linked", async (CreateLinkedUserRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.CreateLinkedUserAsync(new(r.PersonId, r.Username, r.Password, r.Enabled, r.RoleIds), ct))).RequireAuthorization(SecurityPermissions.SecurityUserCreate);
        users.MapGet("/by-person/{personId:guid}", async (Guid personId, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetPersonAccessAsync(personId, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapPost("/for-person/{personId:guid}", async (Guid personId, CreatePersonUserRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.CreateUserForPersonAsync(personId, new(r.Username, r.DisplayName ?? r.Username, r.Email, r.Password), r.Enabled, ct))).RequireAuthorization(SecurityPermissions.SecurityUserCreate);
        users.MapGet("/{id:guid}", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetUserDetailAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapPut("/{id:guid}/account", async (Guid id, UpdateUserAccountRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.UpdateUserAccountAsync(id, r.Username, r.Version, ct))).RequireAuthorization(SecurityPermissions.SecurityUserEdit);
        users.MapPut("/{id:guid}/activation", async (Guid id, SecurityActivationRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetUserActiveAsync(id, r.IsActive, r.Version, ct))).RequireAuthorization(SecurityPermissions.SecurityUserStatusManage);
        users.MapPut("/{id:guid}/unlock", async (Guid id, VersionRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.UnlockUserAsync(id, r.Version, ct))).RequireAuthorization(SecurityPermissions.SecurityUserStatusManage);
        users.MapPut("/{id:guid}/password", async (Guid id, PasswordRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetPasswordAsync(id, r.Password, ct))).RequireAuthorization(SecurityPermissions.SecurityUserPasswordReset);
        users.MapGet("/{id:guid}/companies", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListCompaniesAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapPost("/{id:guid}/companies", async (Guid id, CompanyAccessRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.GrantCompanyAsync(id, r.CompanyId, r.ValidToUtc, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRoleManage);
        users.MapDelete("/{id:guid}/companies/{companyId:guid}", async (Guid id, Guid companyId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokeCompanyAsync(id, companyId, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRoleManage);
        users.MapGet("/{id:guid}/roles", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListUserRolesAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRead);
        users.MapPost("/{id:guid}/roles", async (Guid id, RoleAssignmentRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.AssignRoleAsync(id, r.RoleId, r.CompanyId, r.ValidToUtc, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRoleManage);
        users.MapDelete("/{id:guid}/roles/{roleId:guid}/companies/{companyId:guid}", async (Guid id, Guid roleId, Guid companyId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokeRoleAsync(id, roleId, companyId, ct))).RequireAuthorization(SecurityPermissions.SecurityUserRoleManage);
        users.MapGet("/{id:guid}/permission-target", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetUserPermissionTargetAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityPermissionRead);
        users.MapPut("/{id:guid}/permissions", async (Guid id, SavePermissionSetRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SaveUserPermissionsAsync(id, new(r.Version, r.PermissionIds), ct))).RequireAuthorization(SecurityPermissions.SecurityUserPermissionManage);

        RouteGroupBuilder roles = endpoints.MapGroup("/api/v1/security/roles");
        roles.MapGet("/", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListRolesAsync(ct))).RequireAuthorization(SecurityPermissions.SecurityRoleRead);
        roles.MapGet("/search", async (string? search, bool? isActive, string? sortBy, bool? descending, int? pageNumber, int? pageSize, ISecurityAdministration s, CancellationToken ct) => Map(await s.SearchRolesAsync(new(search, isActive, sortBy ?? "title", descending ?? false, pageNumber ?? 1, pageSize ?? 20), ct))).RequireAuthorization(SecurityPermissions.SecurityRoleRead);
        roles.MapPost("/", async (CreateRoleRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.CreateRoleAsync(r.Code, r.DisplayTitle, r.Description, ct))).RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        roles.MapGet("/{id:guid}", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetRoleAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityRoleRead);
        roles.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.UpdateRoleAsync(id, r.DisplayTitle, r.Description, r.Version, ct))).RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        roles.MapPut("/{id:guid}/activation", async (Guid id, SecurityActivationRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetRoleActiveAsync(id, r.IsActive, r.Version, ct))).RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        roles.MapDelete("/{id:guid}", async (Guid id, long version, ISecurityAdministration s, CancellationToken ct) => Map(await s.DeleteRoleAsync(id, version, ct))).RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        roles.MapGet("/{id:guid}/permissions", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListRolePermissionsAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityPermissionRead);
        roles.MapGet("/{id:guid}/permission-target", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetRolePermissionTargetAsync(id, ct))).RequireAuthorization(SecurityPermissions.SecurityPermissionRead);
        roles.MapPut("/{id:guid}/permissions", async (Guid id, SavePermissionSetRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SaveRolePermissionsAsync(id, new(r.Version, r.PermissionIds), ct))).RequireAuthorization(SecurityPermissions.SecurityRolePermissionManage);
        roles.MapPost("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISecurityAdministration s, CancellationToken ct) => Map(await s.GrantPermissionAsync(id, permissionId, ct))).RequireAuthorization(SecurityPermissions.SecurityRolePermissionManage);
        roles.MapDelete("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokePermissionAsync(id, permissionId, ct))).RequireAuthorization(SecurityPermissions.SecurityRolePermissionManage);
        endpoints.MapGet("/api/v1/security/permissions", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListPermissionsAsync(ct))).RequireAuthorization(SecurityPermissions.SecurityPermissionRead);
        RouteGroupBuilder clients=endpoints.MapGroup("/api/v1/security/api-clients").RequireAuthorization(SecurityPermissions.SecurityUserManage);clients.MapPost("/",async(CreateApiClientCommand c,IApiClientAdministration s,CancellationToken ct)=>Map(await s.CreateAsync(c,ct)));clients.MapPost("/{id:guid}/rotate",async(Guid id,RotateApiClientRequest r,IApiClientAdministration s,CancellationToken ct)=>Map(await s.RotateAsync(id,r.Version,r.ExpiresAtUtc,ct)));clients.MapPut("/{id:guid}/activation",async(Guid id,SecurityActivationRequest r,IApiClientAdministration s,CancellationToken ct)=>Map(await s.SetActiveAsync(id,r.IsActive,r.Version,ct)));
        return endpoints;
    }

    private static async Task<IResult> SearchUsersAsync(string? search, string? accountState, string? sortBy, bool? descending, int? pageNumber, int? pageSize, ISecurityAdministration security, CancellationToken ct)
    {
        UserAccountState? state = null;
        if (!string.IsNullOrWhiteSpace(accountState))
        {
            if (!Enum.TryParse(accountState, true, out UserAccountState parsed))
                return Map(Result.Failure<PagedResult<SecurityUserListItemView>>(Error.Validation("security.user.account_state_invalid")));
            state = parsed;
        }
        return Map(await security.SearchUsersAsync(new(search, state, sortBy ?? "person", descending ?? false, pageNumber ?? 1, pageSize ?? 20), ct));
    }

    private static async Task<IResult> BootstrapAsync(BootstrapRequest request, HttpContext http, IConfiguration config, CompanyContext companies, CoreDbContext db, ISecurityAdministration security, CancellationToken ct)
    {
        string configured = config["SecurityBootstrap:Secret"] ?? string.Empty;
        string supplied = http.Request.Headers["X-Bootstrap-Secret"].ToString();
        if (configured.Length < 24 || supplied.Length != configured.Length || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured))) return Results.Unauthorized();
        // Bootstrap is the pre-context boundary: the caller supplies the company id
        // and the context is established only after this existence check. Applying
        // the normal company query filter here would make every fresh isolated
        // database appear to have no companies.
        bool companyIsActive = await db.Companies.IgnoreQueryFilters().AsNoTracking().AnyAsync(company => company.Id == request.CompanyId && company.IsActive, ct);
        if (!companyIsActive) return Map(Result.Failure(BARANG.Core.Domain.Organization.OrganizationErrors.NotFound));
        using IDisposable scope = companies.BeginScope(request.CompanyId);
        return Map(await security.BootstrapAsync(request.CompanyId, new(request.Username, request.DisplayName, request.Email, request.Password), ct));
    }
    private static async Task<IResult> RecoverDevelopmentAdminAsync(BootstrapRequest request, HttpContext http, IWebHostEnvironment environment, IConfiguration config, CompanyContext companies, CoreDbContext db, IIdentityAccountService identity, IOrganizationalChartStore chart, ITransactionExecutor transactions, IAuditWriter audit, TimeProvider time, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return Results.NotFound(); string configured = config["SecurityBootstrap:Secret"] ?? string.Empty; string supplied = http.Request.Headers["X-Bootstrap-Secret"].ToString(); if (configured.Length < 24 || supplied.Length != configured.Length || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured))) return Results.Unauthorized();
        if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) return Map(Result.Failure(BARANG.Core.Domain.Organization.OrganizationErrors.NotFound)); using IDisposable scope = companies.BeginScope(request.CompanyId);
        return Map(await transactions.ExecuteAsync(async token =>
        {
            DateTimeOffset now = time.GetUtcNow();
            string username = request.Username.Trim().ToUpperInvariant();
            ApplicationUser? user = await db.ApplicationUsers.SingleOrDefaultAsync(x => x.Username == username, token);
            if (user is null)
            {
                string[] names = request.DisplayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string personnelCode = await chart.AllocateNextPersonnelCodeAsync(request.CompanyId, token);
                Result<Person> person = Person.Create(Guid.NewGuid(), request.CompanyId, personnelCode, names.ElementAtOrDefault(0) ?? request.Username, names.ElementAtOrDefault(1) ?? "Administrator", null, null, request.Email, null, null, "Development recovery identity", now);
                if (person.IsFailure) return Result.Failure(person.Error);
                db.Add(person.Value);
                Result<ApplicationUser> created = ApplicationUser.CreateIdentity(Guid.NewGuid(), person.Value.Id, username, person.Value.DisplayName, person.Value.Email, now);
                if (created.IsFailure) return Result.Failure(created.Error);
                Result identityCreated = await identity.CreateAsync(created.Value, request.Password, token);
                if (identityCreated.IsFailure) return identityCreated;
                user = created.Value;
            }
            else
            {
                if (user.PersonId is null)
                {
                    string[] names = request.DisplayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    string personnelCode = await chart.AllocateNextPersonnelCodeAsync(request.CompanyId, token);
                    Result<Person> person = Person.Create(Guid.NewGuid(), request.CompanyId, personnelCode, names.ElementAtOrDefault(0) ?? request.Username, names.ElementAtOrDefault(1) ?? "Administrator", null, null, request.Email, null, null, "Development recovery identity", now);
                    if (person.IsFailure) return Result.Failure(person.Error);
                    db.Add(person.Value);
                    Result linked = user.LinkPerson(person.Value.Id, now);
                    if (linked.IsFailure) return linked;
                }
                user.SetActive(true, now);
                Result reset = await identity.ResetPasswordAsync(user, request.Password, token);
                if (reset.IsFailure) return reset;
                foreach (AuthenticationSession session in await db.AuthenticationSessions.Where(x => x.UserId == user.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > now).ToListAsync(token)) session.Revoke(now);
            }
            Role? role = await db.Roles.SingleOrDefaultAsync(x => x.Code == "system.administrator", token);
            if (role is null) { role = Role.Create(Guid.NewGuid(), "system.administrator", "System administrator", "Built-in recovery-capable administrator", RoleScope.System, true, now).Value; db.Add(role); } else role.SetActive(true, now);
            foreach (Permission permission in await db.Permissions.ToListAsync(token)) if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, token)) db.Add(RolePermission.Create(role.Id, permission.Id, user.Id, now));
            if (!await db.UserCompanies.IgnoreQueryFilters().AnyAsync(x => x.UserId == user.Id && x.CompanyId == request.CompanyId && x.ValidToUtc == null, token)) db.Add(UserCompany.Create(user.Id, request.CompanyId, now, null, user.Id, now).Value);
            if (!await db.UserRoles.IgnoreQueryFilters().AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id && x.CompanyId == request.CompanyId && x.ValidToUtc == null, token)) db.Add(UserRole.Create(user.Id, role.Id, request.CompanyId, now, null, user.Id, now).Value);
            audit.Write(new(AuditActions.BootstrapCompleted, "user", user.Id.ToString(), CompanyId: request.CompanyId, ActorUsername: user.Username, ChangeKind: AuditChangeKind.Modified));
            return Result.Success();
        }, ct));
    }
    private static SecurityResult Map(Result result) => new(result);
    private static SecurityResult<T> Map<T>(Result<T> result) => new(result);

    private sealed class SecurityResult(Result result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (result.IsSuccess)
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }

            return WriteFailureAsync(httpContext, result.Error);
        }
    }

    private sealed class SecurityResult<T>(Result<T> result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (result.IsSuccess)
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                return httpContext.Response.WriteAsJsonAsync(result.Value, httpContext.RequestAborted);
            }

            return WriteFailureAsync(httpContext, result.Error);
        }
    }

    private static Task WriteFailureAsync(HttpContext httpContext, Error error)
    {
        ICorrelationContext correlation = httpContext.RequestServices.GetRequiredService<ICorrelationContext>();
        ProblemDetails details = ApiProblemDetailsMapper.Create(error, correlation.CorrelationId ?? httpContext.TraceIdentifier, httpContext.Request.Path);
        httpContext.Response.StatusCode = details.Status!.Value;
        return httpContext.Response.WriteAsJsonAsync(details, httpContext.RequestAborted);
    }
}
public sealed record LoginRequest(string Username, string Password);
public sealed record CompanySelectionRequest(Guid CompanyId);
public sealed record BootstrapRequest(Guid CompanyId, string Username, string DisplayName, string? Email, string Password);
public sealed record CreateUserRequest(string Username, string DisplayName, string? Email, string Password);
public sealed record UpdateUserRequest(string DisplayName, string? Email, long Version);
public sealed record SecurityActivationRequest(bool IsActive, long Version);
public sealed record PasswordRequest(string Password);
public sealed record CreatePersonUserRequest(string Username,string Password,string? DisplayName,string? Email,bool Enabled=true);
public sealed record CompanyAccessRequest(Guid CompanyId, DateTimeOffset? ValidToUtc);
public sealed record RoleAssignmentRequest(Guid RoleId, Guid CompanyId, DateTimeOffset? ValidToUtc);
public sealed record CreateLinkedUserRequest(Guid PersonId, string Username, string Password, bool Enabled = true, IReadOnlyList<Guid>? RoleIds = null);
public sealed record UpdateUserAccountRequest(string Username, long Version);
public sealed record VersionRequest(long Version);
public sealed record SavePermissionSetRequest(long Version, IReadOnlyList<Guid> PermissionIds);
public sealed record CreateRoleRequest(string Code, string DisplayTitle, string? Description = null);
public sealed record UpdateRoleRequest(string DisplayTitle, long Version, string? Description = null);
public sealed record RotateApiClientRequest(long Version,DateTimeOffset?ExpiresAtUtc);
