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

    [Theory]
    [InlineData("GET", "/api/v1/orders")]
    [InlineData("GET", "/api/v1/orders/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/orders")]
    [InlineData("POST", "/api/v1/orders/00000000-0000-0000-0000-000000000001/submit")]
    public async Task OrderContractsRejectAnonymousAccess(string method,string uri)
    {
        using HttpClient client=factory.CreateClient();using HttpRequestMessage request=new(new HttpMethod(method),uri);if(method=="POST")request.Content=JsonContent.Create(new{});
        using HttpResponseMessage response=await client.SendAsync(request,TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/inventory/shortages")]
    [InlineData("/api/v1/quality/templates")]
    [InlineData("/api/v1/quality/defect-types")]
    public async Task BackofficeReferenceQueriesRejectAnonymousAccess(string uri)
    {
        using HttpClient client=factory.CreateClient();using HttpResponseMessage response=await client.GetAsync(uri,TestContext.Current.CancellationToken);Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentAdministratorRecoveryIsHiddenOutsideDevelopment()
    {
        using HttpClient client=factory.CreateClient();using HttpRequestMessage request=new(HttpMethod.Post,"/api/v1/security/development-admin/recover"){Content=JsonContent.Create(new{companyId=Guid.NewGuid(),username="recovery-probe",displayName="Probe",password="NotUsedPassword123"})};request.Headers.Add("X-Bootstrap-Secret","not-a-configured-bootstrap-secret");using HttpResponseMessage response=await client.SendAsync(request,TestContext.Current.CancellationToken);Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
    }
}
