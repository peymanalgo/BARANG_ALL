namespace BARANG.Core.Domain.Common;

public sealed record Error
{
    public Error(string code, ErrorCategory category, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!IsValidCode(code))
        {
            throw new ArgumentException(
                "Error codes must use lowercase English letters, digits, dots, or underscores.",
                nameof(code));
        }

        Code = code;
        Category = category;
        Description = description;
    }

    public string Code { get; }

    public ErrorCategory Category { get; }

    public string? Description { get; }

    public static Error Validation(string code, string? description = null) =>
        new(code, ErrorCategory.Validation, description);

    public static Error BusinessRule(string code, string? description = null) =>
        new(code, ErrorCategory.BusinessRule, description);

    public static Error NotFound(string code, string? description = null) =>
        new(code, ErrorCategory.NotFound, description);

    public static Error Conflict(string code, string? description = null) =>
        new(code, ErrorCategory.Conflict, description);

    public static Error Authorization(string code, string? description = null) =>
        new(code, ErrorCategory.Authorization, description);

    public static Error Authentication(string code, string? description = null) =>
        new(code, ErrorCategory.Authentication, description);

    public static Error Concurrency(string code, string? description = null) =>
        new(code, ErrorCategory.Concurrency, description);

    public static Error Infrastructure(string code, string? description = null) =>
        new(code, ErrorCategory.Infrastructure, description);

    public static Error Unexpected(string code, string? description = null) =>
        new(code, ErrorCategory.Unexpected, description);

    private static bool IsValidCode(string code)
    {
        if (code[0] is < 'a' or > 'z')
        {
            return false;
        }

        foreach (char character in code)
        {
            bool isLowercaseLetter = character is >= 'a' and <= 'z';
            bool isDigit = character is >= '0' and <= '9';
            if (!isLowercaseLetter && !isDigit && character is not '.' and not '_')
            {
                return false;
            }
        }

        return true;
    }
}
