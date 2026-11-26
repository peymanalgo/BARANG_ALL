using BARANG.Core.Domain.Common;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void SuccessExposesNoError()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void FailureExposesItsError()
    {
        Error error = Error.BusinessRule("core.rule.failed");

        Result result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void GenericSuccessExposesValueOnly()
    {
        Result<string> result = Result.Success("value");

        Assert.Equal("value", result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void GenericFailureExposesErrorOnly()
    {
        Error error = Error.Validation("core.value.invalid");
        Result<string> result = Result.Failure<string>(error);

        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericSuccessRejectsNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }
}
