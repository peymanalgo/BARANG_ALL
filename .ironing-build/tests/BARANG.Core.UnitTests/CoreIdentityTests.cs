using BARANG.Core.Application;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class CoreIdentityTests
{
    [Fact]
    public void CurrentIdentifiesOperationalCore()
    {
        CoreIdentity identity = CoreIdentity.Current;

        Assert.Equal("BARANG Core", identity.Name);
        Assert.Equal("operational", identity.Status);
        Assert.False(string.IsNullOrWhiteSpace(identity.Version));
    }
}
