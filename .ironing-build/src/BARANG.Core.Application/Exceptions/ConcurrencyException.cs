using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;

namespace BARANG.Core.Application.Exceptions;

public sealed class ConcurrencyException : CoreException
{
    public ConcurrencyException(
        string code = "core.concurrency.conflict",
        string? description = null,
        Exception? innerException = null)
        : base(Error.Concurrency(code, description), innerException)
    {
    }
}
