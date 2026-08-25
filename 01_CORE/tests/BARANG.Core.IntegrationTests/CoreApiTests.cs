using System.Net;
using System.Net.Http.Json;
using BARANG.Core.Application;
using Xunit;

namespace BARANG.Core.IntegrationTests;

public sealed class CoreApiTests(CoreApiFactory factory) : IClassFixture<CoreApiFactory>
{
    [Fact]
    public async Task RequestAcceptsSafeCorrelationIdentifier()
    {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", "web-request_123");

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal("web-request_123", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task RequestReplacesUnsafeCorrelationIdentifier()
    {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", "unsafe value with spaces");

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        string correlationId = response.Headers.GetValues("X-Correlation-ID").Single();

        Assert.Equal(32, correlationId.Length);
        Assert.DoesNotContain(' ', correlationId);
    }

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
