using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Exceptions;
using BARANG.Core.Infrastructure.Context;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class CompanyContextTests
{
    [Fact]
    public void RequiredCompanyReturnsValidatedScopedIdentifier()
    {
        CompanyContext context = new();
        Guid companyId = Guid.NewGuid();

        using (context.BeginScope(companyId))
        {
            Assert.True(context.IsAvailable);
            Assert.Equal(companyId, context.RequireCompanyId());
        }

        Assert.False(context.IsAvailable);
    }

    [Fact]
    public void RequiredCompanyFailsClearlyWhenUnavailable()
    {
        CompanyContext context = new();

        Assert.Throws<CompanyContextRequiredException>(() => context.RequireCompanyId());
    }
}
