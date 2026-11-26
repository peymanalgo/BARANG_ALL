using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Exceptions;

public sealed class DomainException : CoreException
{
    public DomainException(Error error)
        : base(Validate(error))
    {
    }

    private static Error Validate(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Category is ErrorCategory.Infrastructure or ErrorCategory.Unexpected)
        {
            throw new ArgumentException("A domain exception cannot represent a technical failure.", nameof(error));
        }

        return error;
    }
}
