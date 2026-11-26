using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Exceptions;

public abstract class CoreException : Exception
{
    protected CoreException(Error error, Exception? innerException = null)
        : base(error?.Description ?? error?.Code, innerException)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public Error Error { get; }
}
