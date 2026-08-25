using BARANG.Core.Domain.Common;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class ErrorTests
{
    [Fact]
    public void ErrorPreservesStableTechnicalData()
    {
        Error error = Error.NotFound("core.resource.not_found", "Technical diagnostic context.");

        Assert.Equal("core.resource.not_found", error.Code);
        Assert.Equal(ErrorCategory.NotFound, error.Category);
        Assert.Equal("Technical diagnostic context.", error.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ORDER.NOT_FOUND")]
    [InlineData("contains space")]
    [InlineData("fa.خطا")]
    public void ErrorRejectsInvalidTechnicalCodes(string code)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code, ErrorCategory.Validation));
    }
}
