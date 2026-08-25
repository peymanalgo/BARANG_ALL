using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;

namespace BARANG.Core.Application.Exceptions;

public sealed class CompanyContextRequiredException : CoreException
{
    public CompanyContextRequiredException()
        : base(Error.Authorization(
            "core.company_context.required",
            "A validated company context is required for this operation."))
    {
    }
}
