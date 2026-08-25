using BARANG.Core.Application.Exceptions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class ExceptionPolicyTests
{
    [Fact]
    public void DomainExceptionRejectsTechnicalFailureCategory()
    {
        Error error = Error.Infrastructure("core.storage.failed");

        Assert.Throws<ArgumentException>(() => new DomainException(error));
    }

    [Fact]
    public void InfrastructureExceptionPreservesOriginalFailure()
    {
        InvalidOperationException original = new("Database detail for logs only.");

        InfrastructureException exception = new(
            "core.storage.failed",
            "A persistence operation failed.",
            original);

        Assert.Same(original, exception.InnerException);
        Assert.Equal(ErrorCategory.Infrastructure, exception.Error.Category);
    }

    [Fact]
    public void ConcurrencyExceptionUsesConcurrencyCategory()
    {
        ConcurrencyException exception = new();

        Assert.Equal(ErrorCategory.Concurrency, exception.Error.Category);
    }
}
