using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Organization;

public sealed class CompanyContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Company-ID";

    public async Task InvokeAsync(HttpContext httpContext, CompanyContext companyContext, CoreDbContext dbContext, ICorrelationContext correlationContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out Microsoft.Extensions.Primitives.StringValues raw)) { await next(httpContext); return; }
        IHostEnvironment environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        IConfiguration configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        bool allowed = (environment.IsDevelopment() || environment.IsEnvironment("Testing")) && configuration.GetValue<bool>("CompanyResolution:AllowDevelopmentHeader");
        if (!allowed) { await WriteFailureAsync(httpContext, correlationContext, Error.Authorization("core.company_context.header_forbidden")); return; }
        if (raw.Count != 1 || !Guid.TryParse(raw[0], out Guid companyId) || companyId == Guid.Empty) { await WriteFailureAsync(httpContext, correlationContext, Error.Validation("core.company_context.invalid")); return; }
        bool exists = await dbContext.Companies.AsNoTracking().AnyAsync(company => company.Id == companyId && company.IsActive, httpContext.RequestAborted);
        if (!exists) { await WriteFailureAsync(httpContext, correlationContext, Error.NotFound("core.company_context.not_found")); return; }
        using (companyContext.BeginScope(companyId)) { await next(httpContext); }
    }

    private static async Task WriteFailureAsync(HttpContext context, ICorrelationContext correlation, Error error)
    {
        ProblemDetails details = ApiProblemDetailsMapper.Create(error, correlation.CorrelationId ?? context.TraceIdentifier, context.Request.Path);
        context.Response.StatusCode = details.Status!.Value;
        await context.Response.WriteAsJsonAsync(details, context.RequestAborted);
    }
}
