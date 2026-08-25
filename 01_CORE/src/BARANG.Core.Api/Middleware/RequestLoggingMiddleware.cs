using System.Diagnostics;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;

namespace BARANG.Core.Api.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly Action<ILogger, string, string, int, double, Exception?> RequestCompleted =
        LoggerMessage.Define<string, string, int, double>(
            LogLevel.Information,
            new EventId(1, nameof(RequestCompleted)),
            "HTTP {RequestMethod} {RequestPath} completed with {StatusCode} in {ElapsedMilliseconds} ms.");

    public async Task InvokeAsync(
        HttpContext httpContext,
        ICorrelationContext correlationContext,
        ICurrentActor currentActor,
        ICompanyContext companyContext)
    {
        ActorContext actor = currentActor.Actor;
        Dictionary<string, object?> properties = new(StringComparer.Ordinal)
        {
            ["CorrelationId"] = correlationContext.CorrelationId,
            ["RequestPath"] = httpContext.Request.Path.Value,
            ["RequestMethod"] = httpContext.Request.Method,
            ["ActorId"] = actor.ActorId,
            ["UserId"] = actor.UserId,
            ["CompanyId"] = companyContext.CompanyId,
            ["SourceChannel"] = actor.SourceChannel.ToString()
        };

        using IDisposable? scope = logger.BeginScope(properties);
        long startedAt = Stopwatch.GetTimestamp();

        await next(httpContext);

        if (!httpContext.Request.Path.StartsWithSegments("/health"))
        {
            double elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            RequestCompleted(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                httpContext.Response.StatusCode,
                elapsedMilliseconds,
                null);
        }
    }
}
