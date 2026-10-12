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

        RouteGroupBuilder users = endpoints.MapGroup("/api/v1/security/users").RequireAuthorization(SecurityPermissions.SecurityUserManage);
        users.MapPost("/", async (CreateUserRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.CreateUserAsync(new(r.Username, r.DisplayName, r.Email, r.Password), ct)));
        users.MapGet("/", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListUsersAsync(ct)));
        users.MapGet("/search", async (string? search, ISecurityAdministration s, CancellationToken ct) =>
        {
            Result<IReadOnlyList<UserView>> result = await s.ListUsersAsync(ct);
            if (result.IsFailure) return Map(result);
            string query = search?.Trim() ?? string.Empty;
            UserView[] items = result.Value.Where(x => query.Length == 0 || x.Username.Contains(query, StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Results.Json(new { items, totalCount = items.Length, pageNumber = 1, pageSize = items.Length });
        });
        users.MapGet("/{id:guid}", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetUserAsync(id, ct)));
        users.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.UpdateUserAsync(id, r.DisplayName, r.Email, r.Version, ct)));
        users.MapPut("/{id:guid}/activation", async (Guid id, SecurityActivationRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetUserActiveAsync(id, r.IsActive, r.Version, ct)));
        users.MapPut("/{id:guid}/password", async (Guid id, PasswordRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetPasswordAsync(id, r.Password, ct)));
        users.MapGet("/{id:guid}/companies", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListCompaniesAsync(id, ct)));
        users.MapPost("/{id:guid}/companies", async (Guid id, CompanyAccessRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.GrantCompanyAsync(id, r.CompanyId, r.ValidToUtc, ct)));
        users.MapDelete("/{id:guid}/companies/{companyId:guid}", async (Guid id, Guid companyId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokeCompanyAsync(id, companyId, ct)));
        users.MapGet("/{id:guid}/roles", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListUserRolesAsync(id, ct)));
        users.MapPost("/{id:guid}/roles", async (Guid id, RoleAssignmentRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.AssignRoleAsync(id, r.RoleId, r.CompanyId, r.ValidToUtc, ct)));
        users.MapDelete("/{id:guid}/roles/{roleId:guid}/companies/{companyId:guid}", async (Guid id, Guid roleId, Guid companyId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokeRoleAsync(id, roleId, companyId, ct)));

        RouteGroupBuilder roles = endpoints.MapGroup("/api/v1/security/roles").RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        roles.MapPost("/", async (CreateRoleRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.CreateRoleAsync(r.Code, r.DisplayTitle, ct)));
        roles.MapGet("/", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListRolesAsync(ct)));
        roles.MapGet("/search", async (string? search, ISecurityAdministration s, CancellationToken ct) =>
        {
            Result<IReadOnlyList<RoleView>> result = await s.ListRolesAsync(ct);
            if (result.IsFailure) return Map(result);
            string query = search?.Trim() ?? string.Empty;
            RoleView[] items = result.Value.Where(x => query.Length == 0 || x.Code.Contains(query, StringComparison.OrdinalIgnoreCase) || x.DisplayTitle.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Results.Json(new { items, totalCount = items.Length, pageNumber = 1, pageSize = items.Length });
        });
        roles.MapGet("/{id:guid}", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.GetRoleAsync(id, ct)));
        roles.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.UpdateRoleAsync(id, r.DisplayTitle, r.Version, ct)));
        roles.MapPut("/{id:guid}/activation", async (Guid id, SecurityActivationRequest r, ISecurityAdministration s, CancellationToken ct) => Map(await s.SetRoleActiveAsync(id, r.IsActive, r.Version, ct)));
        roles.MapGet("/{id:guid}/permissions", async (Guid id, ISecurityAdministration s, CancellationToken ct) => Map(await s.ListRolePermissionsAsync(id, ct)));
        roles.MapPost("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISecurityAdministration s, CancellationToken ct) => Map(await s.GrantPermissionAsync(id, permissionId, ct)));
        roles.MapDelete("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISecurityAdministration s, CancellationToken ct) => Map(await s.RevokePermissionAsync(id, permissionId, ct)));
        endpoints.MapGet("/api/v1/security/permissions", async (ISecurityAdministration s, CancellationToken ct) => Map(await s.ListPermissionsAsync(ct))).RequireAuthorization(SecurityPermissions.SecurityRoleManage);
        RouteGroupBuilder clients=endpoints.MapGroup("/api/v1/security/api-clients").RequireAuthorization(SecurityPermissions.SecurityUserManage);clients.MapPost("/",async(CreateApiClientCommand c,IApiClientAdministration s,CancellationToken ct)=>Map(await s.CreateAsync(c,ct)));clients.MapPost("/{id:guid}/rotate",async(Guid id,RotateApiClientRequest r,IApiClientAdministration s,CancellationToken ct)=>Map(await s.RotateAsync(id,r.Version,r.ExpiresAtUtc,ct)));clients.MapPut("/{id:guid}/activation",async(Guid id,SecurityActivationRequest r,IApiClientAdministration s,CancellationToken ct)=>Map(await s.SetActiveAsync(id,r.IsActive,r.Version,ct)));
        return endpoints;
    }

    private static async Task<IResult> BootstrapAsync(BootstrapRequest request, HttpContext http, IConfiguration config, CompanyContext companies, CoreDbContext db, ISecurityAdministration security, CancellationToken ct)
    {
        string configured = config["SecurityBootstrap:Secret"] ?? string.Empty;
        string supplied = http.Request.Headers["X-Bootstrap-Secret"].ToString();
        if (configured.Length < 24 || supplied.Length != configured.Length || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured))) return Results.Unauthorized();
        bool companyIsActive = await db.Companies.AsNoTracking().AnyAsync(company => company.Id == request.CompanyId && company.IsActive, ct);
        if (!companyIsActive) return Map(Result.Failure(BARANG.Core.Domain.Organization.OrganizationErrors.NotFound));
        using IDisposable scope = companies.BeginScope(request.CompanyId);
        return Map(await security.BootstrapAsync(request.CompanyId, new(request.Username, request.DisplayName, request.Email, request.Password), ct));
    }
    private static async Task<IResult> RecoverDevelopmentAdminAsync(BootstrapRequest request, HttpContext http, IWebHostEnvironment environment, IConfiguration config, CompanyContext companies, CoreDbContext db, IPasswordCredentialService passwords, ITransactionExecutor transactions, IAuditWriter audit, TimeProvider time, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return Results.NotFound(); string configured = config["SecurityBootstrap:Secret"] ?? string.Empty; string supplied = http.Request.Headers["X-Bootstrap-Secret"].ToString(); if (configured.Length < 24 || supplied.Length != configured.Length || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured))) return Results.Unauthorized();
        if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) return Map(Result.Failure(BARANG.Core.Domain.Organization.OrganizationErrors.NotFound)); using IDisposable scope = companies.BeginScope(request.CompanyId);
        return Map(await transactions.ExecuteAsync(async token =>
        {
            string username = request.Username.Trim().ToUpperInvariant(); ApplicationUser? user = await db.ApplicationUsers.SingleOrDefaultAsync(x => x.Username == username, token); if (user is null) { Result<string> hash = passwords.HashPassword(request.Password); if (hash.IsFailure) return Result.Failure(hash.Error); Result<ApplicationUser> created = ApplicationUser.Create(Guid.NewGuid(), username, request.DisplayName, request.Email, hash.Value, time.GetUtcNow()); if (created.IsFailure) return Result.Failure(created.Error); user = created.Value; db.Add(user); }
            Role? role = await db.Roles.SingleOrDefaultAsync(x => x.Code == "system.administrator", token); if (role is null) { role = Role.Create(Guid.NewGuid(), "system.administrator", "System administrator", true, time.GetUtcNow()).Value; db.Add(role); } else role.SetActive(true, time.GetUtcNow()); foreach (Permission permission in await db.Permissions.ToListAsync(token)) if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, token)) db.Add(RolePermission.Create(role.Id, permission.Id, user.Id, time.GetUtcNow())); if (!await db.UserCompanies.IgnoreQueryFilters().AnyAsync(x => x.UserId == user.Id && x.CompanyId == request.CompanyId && x.ValidToUtc == null, token)) db.Add(UserCompany.Create(user.Id, request.CompanyId, time.GetUtcNow(), null, user.Id, time.GetUtcNow()).Value); if (!await db.UserRoles.IgnoreQueryFilters().AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id && x.CompanyId == request.CompanyId && x.ValidToUtc == null, token)) db.Add(UserRole.Create(user.Id, role.Id, request.CompanyId, time.GetUtcNow(), null, user.Id, time.GetUtcNow()).Value); audit.Write(new(AuditActions.BootstrapCompleted, "user", user.Id.ToString(), CompanyId: request.CompanyId, ActorUsername: user.Username, ChangeKind: AuditChangeKind.Modified)); return Result.Success();
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
public sealed record CompanyAccessRequest(Guid CompanyId, DateTimeOffset? ValidToUtc);
public sealed record RoleAssignmentRequest(Guid RoleId, Guid CompanyId, DateTimeOffset? ValidToUtc);
public sealed record CreateRoleRequest(string Code, string DisplayTitle);
public sealed record UpdateRoleRequest(string DisplayTitle, long Version);
public sealed record RotateApiClientRequest(long Version,DateTimeOffset?ExpiresAtUtc);
