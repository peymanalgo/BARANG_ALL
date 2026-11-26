using BARANG.Core.Application.Exceptions;

namespace BARANG.Core.Application.Abstractions.Companies;

public static class CompanyContextExtensions
{
    public static Guid RequireCompanyId(this ICompanyContext companyContext)
    {
        ArgumentNullException.ThrowIfNull(companyContext);

        return companyContext.CompanyId is { } companyId && companyId != Guid.Empty
            ? companyId
            : throw new CompanyContextRequiredException();
    }
}
