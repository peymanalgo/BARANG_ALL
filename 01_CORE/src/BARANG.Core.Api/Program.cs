using BARANG.Core.Application;
using BARANG.Core.Api.Errors;
using BARANG.Core.Api.Middleware;
using BARANG.Core.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using BARANG.Core.Api.Organization;
using BARANG.Core.Api.Security;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using BARANG.Core.Api.Auditing;
using BARANG.Core.Api.Lookups;
using BARANG.Core.Api.Customers;
using BARANG.Core.Application.Auditing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(BearerAuthenticationHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>(BearerAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization(options =>
{
    foreach (string permission in SecurityPermissions.All) options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditRequestContext, HttpAuditRequestContext>();

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseMiddleware<CurrentActorMiddleware>();
app.UseMiddleware<CompanyContextMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(CoreIdentity.Current))
    .WithName("GetCoreIdentity");
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = static registration => registration.Tags.Contains("ready")
});
app.MapOrganizationEndpoints();
app.MapSecurityEndpoints();
app.MapAuditEndpoints();
app.MapLookupEndpoints();
app.MapCustomerEndpoints();

app.Run();

public partial class Program;
