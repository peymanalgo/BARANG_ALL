using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using BARANG.Core.Application.Security;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Transactions;

namespace BARANG.Core.Api.Organization;

public sealed class CompanyContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Company-ID";

    public async Task InvokeAsync(HttpContext httpContext, CompanyContext companyContext, CoreDbContext dbContext, ICorrelationContext correlationContext, ICurrentActor actor, ISecurityStore security, ITransactionExecutor transactions, TimeProvider time)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out Microsoft.Extensions.Primitives.StringValues raw)) { await next(httpContext); return; }
        IHostEnvironment environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        IConfiguration configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (raw.Count != 1 || !Guid.TryParse(raw[0], out Guid companyId) || companyId == Guid.Empty) { await WriteFailureAsync(httpContext, correlationContext, Error.Validation("core.company_context.invalid")); return; }
        bool exists = await dbContext.Companies.IgnoreQueryFilters().AsNoTracking().AnyAsync(company => company.Id == companyId && company.IsActive, httpContext.RequestAborted);
        if (!exists)
        {
            Error error = actor.Actor.IsAuthenticated
                ? Error.Authorization("security.company_access.denied")
                : Error.NotFound("core.company_context.not_found");
            await WriteFailureAsync(httpContext, correlationContext, error);
            return;
        }
        using (companyContext.BeginScope(companyId))
        {
            if(actor.Actor.ActorType==ActorType.ApiClient&&Guid.TryParse(actor.Actor.ApiClientId,out Guid clientId))
            {Result access=await transactions.ExecuteAsync(async ct=>await security.ApiClientHasCompanyAsync(clientId,companyId,ct)?Result.Success():Result.Failure(Error.Authorization("security.company_access.denied")),httpContext.RequestAborted);if(access.IsFailure){await WriteFailureAsync(httpContext,correlationContext,access.Error);return;}}
            else if (actor.Actor.IsAuthenticated && Guid.TryParse(actor.Actor.UserId, out Guid userId))
            {
                Result access = await transactions.ExecuteAsync(async ct => !await security.HasEffectiveCompanyAccessAsync(userId, companyId, time.GetUtcNow(), ct) ? Result.Failure(Error.Authorization("security.company_access.denied")) : Result.Success(), httpContext.RequestAborted);
                if (access.IsFailure) { await WriteFailureAsync(httpContext, correlationContext, access.Error); return; }
            }
            else
            {
                bool allowed = (environment.IsDevelopment() || environment.IsEnvironment("Testing")) && configuration.GetValue<bool>("CompanyResolution:AllowDevelopmentHeader");
                if (!allowed) { await WriteFailureAsync(httpContext, correlationContext, Error.Authentication("security.authentication.required")); return; }
            }
            await next(httpContext);
        }
    }

    private static async Task WriteFailureAsync(HttpContext context, ICorrelationContext correlation, Error error)
    {
        ProblemDetails details = ApiProblemDetailsMapper.Create(error, correlation.CorrelationId ?? context.TraceIdentifier, context.Request.Path);
        context.Response.StatusCode = details.Status!.Value;
        await context.Response.WriteAsJsonAsync(details, context.RequestAborted);
    }
}
