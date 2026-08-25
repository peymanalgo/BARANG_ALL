using BARANG.Core.Infrastructure.Context;
using Microsoft.Extensions.Primitives;

namespace BARANG.Core.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const int MaximumLength = 64;

    public async Task InvokeAsync(HttpContext httpContext, CorrelationContext correlationContext)
    {
        string correlationId = ResolveCorrelationId(httpContext.Request.Headers[HeaderName]);

        using IDisposable scope = correlationContext.BeginScope(correlationId);
        httpContext.TraceIdentifier = correlationId;
        httpContext.Response.Headers[HeaderName] = correlationId;

        await next(httpContext);
    }

    private static string ResolveCorrelationId(StringValues values)
    {
        if (values.Count == 1 && values[0] is { } candidate && IsValid(candidate))
        {
            return candidate;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(string value)
    {
        if (value.Length is < 1 or > MaximumLength)
        {
            return false;
        }

        return value.All(static character =>
            character is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9'
            or '-' or '_' or '.');
    }
}
