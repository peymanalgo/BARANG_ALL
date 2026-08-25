using System.Net;
using System.Net.Http.Json;
using BARANG.Core.Application;
using Xunit;

namespace BARANG.Core.IntegrationTests;

public sealed class CoreApiTests(CoreApiFactory factory) : IClassFixture<CoreApiFactory>
{
    [Fact]
    public async Task HealthReturnsSuccess()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/health",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RootReturnsCoreIdentity()
    {
        using HttpClient client = factory.CreateClient();

        CoreIdentity? identity = await client.GetFromJsonAsync<CoreIdentity>(
            "/",
            TestContext.Current.CancellationToken);

        Assert.NotNull(identity);
        Assert.Equal("BARANG Core", identity.Name);
        Assert.Equal("operational", identity.Status);
    }
}
