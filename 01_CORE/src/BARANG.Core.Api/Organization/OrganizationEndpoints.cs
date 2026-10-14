using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Organization;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Organization;
using Microsoft.AspNetCore.Mvc;
using BARANG.Core.Domain.Security;

namespace BARANG.Core.Api.Organization;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/organization");
        RouteGroupBuilder companies = group.MapGroup("/companies").RequireAuthorization(SecurityPermissions.OrganizationCompanyManage);
        companies.MapPost("/", async (CreateCompanyRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.CreateCompanyAsync(new(r.Code, r.DisplayName), ct), true));
        companies.MapGet("/", async (IOrganizationAdministration s, CancellationToken ct) => Map(await s.ListCompaniesAsync(ct)));
        companies.MapGet("/{companyId:guid}", async (Guid companyId, IOrganizationAdministration s, CancellationToken ct) => Map(await s.GetCompanyAsync(companyId, ct)));
        companies.MapPut("/{companyId:guid}", async (Guid companyId, UpdateNameRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.UpdateCompanyAsync(companyId, r.Name, r.Version, ct)));
        companies.MapPut("/{companyId:guid}/activation", async (Guid companyId, ActivationRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.SetCompanyActiveAsync(companyId, r.IsActive, r.Version, ct)));
        RouteGroupBuilder settings = group.MapGroup("/settings").RequireAuthorization(SecurityPermissions.OrganizationSettingsManage);
        settings.MapPost("/", async (CreateSettingRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.CreateSettingAsync(new(r.Key, r.Value, r.ValueType), ct), true));
        settings.MapGet("/", async (IOrganizationAdministration s, CancellationToken ct) => Map(await s.ListSettingsAsync(ct)));
        settings.MapGet("/{settingId:guid}", async (Guid settingId, IOrganizationAdministration s, CancellationToken ct) => Map(await s.GetSettingAsync(settingId, ct)));
        settings.MapPut("/{settingId:guid}", async (Guid settingId, UpdateSettingRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.UpdateSettingAsync(settingId, r.Value, r.ValueType, r.Version, ct)));
        settings.MapPut("/{settingId:guid}/deactivation", async (Guid settingId, VersionRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.DeactivateSettingAsync(settingId, r.Version, ct)));
        RouteGroupBuilder branches = group.MapGroup("/branches").RequireAuthorization(SecurityPermissions.OrganizationBranchManage);
        branches.MapPost("/", async (CreateBranchRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.CreateBranchAsync(new(r.Code, r.DisplayName), ct), true));
        branches.MapGet("/", async (IOrganizationAdministration s, CancellationToken ct) => Map(await s.ListBranchesAsync(ct)));
        branches.MapGet("/{branchId:guid}", async (Guid branchId, IOrganizationAdministration s, CancellationToken ct) => Map(await s.GetBranchAsync(branchId, ct)));
        branches.MapPut("/{branchId:guid}", async (Guid branchId, UpdateNameRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.UpdateBranchAsync(branchId, r.Name, r.Version, ct)));
        branches.MapPut("/{branchId:guid}/activation", async (Guid branchId, ActivationRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.SetBranchActiveAsync(branchId, r.IsActive, r.Version, ct)));
        RouteGroupBuilder units = group.MapGroup("/organizational-units").RequireAuthorization(SecurityPermissions.OrganizationUnitManage);
        units.MapPost("/", async (CreateUnitRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.CreateUnitAsync(new(r.Code, r.DisplayName, r.BranchId, r.ParentId), ct), true));
        units.MapGet("/", async (IOrganizationAdministration s, CancellationToken ct) => Map(await s.ListUnitsAsync(ct)));
        units.MapGet("/{unitId:guid}", async (Guid unitId, IOrganizationAdministration s, CancellationToken ct) => Map(await s.GetUnitAsync(unitId, ct)));
        units.MapPut("/{unitId:guid}", async (Guid unitId, UpdateUnitRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.UpdateUnitAsync(unitId, r.DisplayName, r.BranchId, r.ParentId, r.Version, ct)));
        units.MapPut("/{unitId:guid}/activation", async (Guid unitId, ActivationRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.SetUnitActiveAsync(unitId, r.IsActive, r.Version, ct)));
        RouteGroupBuilder chart = group.MapGroup("/chart").RequireAuthorization(SecurityPermissions.OrganizationUnitManage);
        chart.MapGet("/", async (Guid? highlightedPersonId, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.GetChartAsync(highlightedPersonId, ct)));
        chart.MapGet("/companies/{companyId:guid}", async (Guid companyId, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.GetCompanyChartAsync(companyId, ct)));
        chart.MapPost("/assignments", async (CreateAssignmentRequest r, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.AssignPersonAsync(new(r.PersonId, r.OrganizationalUnitId, r.ManagerAssignmentId, r.PositionCode, r.PositionTitle, r.IsPrimary, r.StartDate, r.EndDate, r.Notes), ct), true));
        chart.MapPut("/assignments/{id:guid}", async (Guid id, CreateAssignmentRequest r, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.ChangeAssignmentAsync(id, new(r.PersonId, r.OrganizationalUnitId, r.ManagerAssignmentId, r.PositionCode, r.PositionTitle, r.IsPrimary, r.StartDate, r.EndDate, r.Notes), r.Version, ct)));
        chart.MapPut("/assignments/{id:guid}/activation", async (Guid id, ActivationRequest r, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.SetAssignmentActiveAsync(id, r.IsActive, r.Version, ct)));
        chart.MapDelete("/assignments/{id:guid}", async (Guid id, long version, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.RemoveAssignmentAsync(id, version, ct)));
        RouteGroupBuilder persons = group.MapGroup("/persons").RequireAuthorization(SecurityPermissions.OrganizationUnitManage);
        persons.MapGet("/", async (string? search, int? take, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.SearchPersonsAsync(search, take ?? 30, ct)));
        persons.MapGet("/{id:guid}", async (Guid id, IOrganizationalChartAdministration s, CancellationToken ct) => Map(await s.GetPersonAsync(id, ct)));
        RouteGroupBuilder warehouses = group.MapGroup("/warehouses").RequireAuthorization(SecurityPermissions.OrganizationWarehouseManage);
        warehouses.MapPost("/", async (CreateWarehouseRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.CreateWarehouseAsync(new(r.Code, r.Name, r.Type, r.BranchId), ct), true));
        warehouses.MapGet("/", async (IOrganizationAdministration s, CancellationToken ct) => Map(await s.ListWarehousesAsync(ct)));
        warehouses.MapGet("/{warehouseId:guid}", async (Guid warehouseId, IOrganizationAdministration s, CancellationToken ct) => Map(await s.GetWarehouseAsync(warehouseId, ct)));
        warehouses.MapPut("/{warehouseId:guid}", async (Guid warehouseId, UpdateWarehouseRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.UpdateWarehouseAsync(warehouseId, r.Name, r.Type, r.BranchId, r.Version, ct)));
        warehouses.MapPut("/{warehouseId:guid}/activation", async (Guid warehouseId, ActivationRequest r, IOrganizationAdministration s, CancellationToken ct) => Map(await s.SetWarehouseActiveAsync(warehouseId, r.IsActive, r.Version, ct)));
        return endpoints;
    }

    private static OrganizationResult<T> Map<T>(Result<T> result, bool created = false) => new(result, created);
    private static OrganizationVoidResult Map(Result result) => new(result);

    private sealed class OrganizationResult<T>(Result<T> result, bool created) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (result.IsSuccess)
            {
                httpContext.Response.StatusCode = created ? StatusCodes.Status201Created : StatusCodes.Status200OK;
                return httpContext.Response.WriteAsJsonAsync(result.Value, httpContext.RequestAborted);
            }

            ICorrelationContext correlation = httpContext.RequestServices.GetRequiredService<ICorrelationContext>();
            ProblemDetails details = ApiProblemDetailsMapper.Create(result.Error, correlation.CorrelationId ?? httpContext.TraceIdentifier, httpContext.Request.Path);
            httpContext.Response.StatusCode = details.Status!.Value;
            return httpContext.Response.WriteAsJsonAsync(details, httpContext.RequestAborted);
        }
    }

    private sealed class OrganizationVoidResult(Result result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (result.IsSuccess) { httpContext.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; }
            ICorrelationContext correlation = httpContext.RequestServices.GetRequiredService<ICorrelationContext>();
            ProblemDetails details = ApiProblemDetailsMapper.Create(result.Error, correlation.CorrelationId ?? httpContext.TraceIdentifier, httpContext.Request.Path);
            return Results.Problem(details).ExecuteAsync(httpContext);
        }
    }
}

public sealed record CreateCompanyRequest(string Code, string DisplayName);
public sealed record UpdateNameRequest(string Name, long Version);
public sealed record ActivationRequest(bool IsActive, long Version);
public sealed record VersionRequest(long Version);
public sealed record CreateSettingRequest(string Key, string Value, CompanySettingValueType ValueType);
public sealed record UpdateSettingRequest(string Value, CompanySettingValueType ValueType, long Version);
public sealed record CreateBranchRequest(string Code, string DisplayName);
public sealed record CreateUnitRequest(string Code, string DisplayName, Guid? BranchId, Guid? ParentId);
public sealed record UpdateUnitRequest(string DisplayName, Guid? BranchId, Guid? ParentId, long Version);
public sealed record CreateWarehouseRequest(string Code, string Name, WarehouseType Type, Guid? BranchId);
public sealed record UpdateWarehouseRequest(string Name, WarehouseType Type, Guid? BranchId, long Version);
public sealed record CreateAssignmentRequest(Guid PersonId, Guid OrganizationalUnitId, Guid? ManagerAssignmentId, string PositionCode, string PositionTitle, bool IsPrimary, DateOnly? StartDate, DateOnly? EndDate, string? Notes, long Version = 0);
