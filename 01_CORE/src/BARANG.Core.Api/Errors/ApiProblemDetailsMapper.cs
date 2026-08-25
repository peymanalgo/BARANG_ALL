using BARANG.Core.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Errors;

public static class ApiProblemDetailsMapper
{
    public static ProblemDetails Create(Error error, string correlationId, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        int status = GetStatusCode(error.Category);
        ProblemDetails problemDetails = new()
        {
            Status = status,
            Title = GetTitle(error.Category),
            Type = $"https://httpstatuses.com/{status}",
            Instance = instance
        };

        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["category"] = error.Category.ToString();
        problemDetails.Extensions["correlationId"] = correlationId;
        return problemDetails;
    }

    public static int GetStatusCode(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => StatusCodes.Status400BadRequest,
        ErrorCategory.Authentication => StatusCodes.Status401Unauthorized,
        ErrorCategory.Authorization => StatusCodes.Status403Forbidden,
        ErrorCategory.NotFound => StatusCodes.Status404NotFound,
        ErrorCategory.Conflict or ErrorCategory.Concurrency => StatusCodes.Status409Conflict,
        ErrorCategory.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorCategory.Infrastructure or ErrorCategory.Unexpected =>
            StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => "Validation failed",
        ErrorCategory.Authentication => "Authentication required",
        ErrorCategory.Authorization => "Access denied",
        ErrorCategory.NotFound => "Resource not found",
        ErrorCategory.Conflict => "Request conflict",
        ErrorCategory.Concurrency => "Concurrency conflict",
        ErrorCategory.BusinessRule => "Business rule failed",
        _ => "Request failed"
    };
}
