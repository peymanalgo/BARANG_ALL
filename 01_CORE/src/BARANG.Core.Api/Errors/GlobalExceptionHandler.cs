using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ICorrelationContext correlationContext,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception?> ControlledFailure =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(ControlledFailure)),
            "Controlled request failure {ErrorCode} with correlation {CorrelationId}.");

    private static readonly Action<ILogger, string, Exception?> UnexpectedFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(UnexpectedFailure)),
            "Unexpected request failure with correlation {CorrelationId}.");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string correlationId = correlationContext.CorrelationId ?? httpContext.TraceIdentifier;
        Error error;

        if (exception is CoreException coreException)
        {
            error = coreException.Error;
            if (error.Category is ErrorCategory.Infrastructure or ErrorCategory.Unexpected)
            {
                UnexpectedFailure(logger, correlationId, exception);
            }
            else
            {
                ControlledFailure(logger, error.Code, correlationId, null);
            }
        }
        else
        {
            error = Error.Unexpected("core.unexpected");
            UnexpectedFailure(logger, correlationId, exception);
        }

        ProblemDetails details = ApiProblemDetailsMapper.Create(
            error,
            correlationId,
            httpContext.Request.Path);
        httpContext.Response.StatusCode = details.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception
        });
    }
}
