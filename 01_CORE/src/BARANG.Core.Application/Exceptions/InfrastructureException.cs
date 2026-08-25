using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;

namespace BARANG.Core.Application.Exceptions;

public sealed class InfrastructureException : CoreException
{
    public InfrastructureException(string code, string? description, Exception innerException)
        : base(Error.Infrastructure(code, description), innerException)
    {
    }
}
