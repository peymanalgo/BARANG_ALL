using System.Net;
using System.Text;
using BARANG.Backoffice.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace BARANG.Backoffice.Tests;

public sealed class CoreApiClientTests
{
    [Fact]
    public async Task LoginSuccessReturnsAuthenticationState()
    {
        Guid id = Guid.NewGuid();
        string body = "{\"accessToken\":\"opaque\",\"expiresAtUtc\":\"2030-01-01T00:00:00Z\",\"user\":{\"id\":\"" + id + "\",\"username\":\"user\",\"email\":null,\"displayName\":\"User\"}}";
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, body));
        var result = await Client(handler).LoginAsync("user", "secret", TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess); Assert.Equal("opaque", result.Value!.AccessToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ApiFailure.InvalidCredentials)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ApiFailure.Validation)]
    [InlineData(HttpStatusCode.InternalServerError, ApiFailure.Unexpected)]
    public async Task LoginMapsFailures(HttpStatusCode status, ApiFailure expected)
    {
        var result = await Client(new StubHandler(_ => Json(status, "{}"))).LoginAsync("u", "p", TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Failure);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ApiFailure.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ApiFailure.Forbidden)]
    public async Task AuthenticatedRequestsDistinguish401And403(HttpStatusCode status, ApiFailure expected)
    {
        var result = await Client(new StubHandler(_ => Json(status, "{}"))).GetMeAsync("token", null, TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Failure);
    }

    [Fact]
    public async Task CompanyHeaderIsAddedCentrallyOnlyWhenSelected()
    {
        List<HttpRequestMessage> requests = [];
        var handler = new StubHandler(request => { requests.Add(Clone(request)); return Json(HttpStatusCode.OK, "{\"userId\":\"00000000-0000-0000-0000-000000000001\",\"username\":\"u\",\"displayName\":\"U\",\"accountState\":\"Active\",\"currentCompanyId\":null,\"effectiveRoleCodes\":[]}"); });
        Guid companyId = Guid.NewGuid(); var client = Client(handler);
        await client.GetMeAsync("token", null, TestContext.Current.CancellationToken);
        await client.GetMeAsync("token", companyId, TestContext.Current.CancellationToken);
        Assert.False(requests[0].Headers.Contains("X-Company-ID")); Assert.Equal(companyId.ToString(), requests[1].Headers.GetValues("X-Company-ID").Single());
        Assert.Equal("Bearer", requests[1].Headers.Authorization!.Scheme);
    }

    [Theory]
    [InlineData("\"Active\"", "Active")]
    [InlineData("0", "Active")]
    [InlineData("\"Inactive\"", "Inactive")]
    [InlineData("1", "Inactive")]
    [InlineData("\"Locked\"", "Locked")]
    [InlineData("2", "Locked")]
    public async Task CurrentUserAcceptsCanonicalAndLegacyAccountStateWireValues(string wireValue, string expected)
    {
        string body = $"{{\"userId\":\"00000000-0000-0000-0000-000000000001\",\"username\":\"u\",\"displayName\":\"U\",\"accountState\":{wireValue},\"currentCompanyId\":null,\"effectiveRoleCodes\":[],\"effectivePermissionCodes\":[],\"isSystemAdministrator\":false}}";
        var result = await Client(new StubHandler(_ => Json(HttpStatusCode.OK, body))).GetMeAsync("token", null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.AccountState);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("\"Suspended\"")]
    [InlineData("true")]
    public async Task CurrentUserRejectsUnknownAccountStateAsContractError(string wireValue)
    {
        string body = $"{{\"userId\":\"00000000-0000-0000-0000-000000000001\",\"username\":\"u\",\"displayName\":\"U\",\"accountState\":{wireValue},\"currentCompanyId\":null,\"effectiveRoleCodes\":[],\"effectivePermissionCodes\":[],\"isSystemAdministrator\":false}}";
        var result = await Client(new StubHandler(_ => Json(HttpStatusCode.OK, body))).GetMeAsync("token", null, TestContext.Current.CancellationToken);

        Assert.Equal(ApiFailure.ContractError, result.Failure);
        Assert.Equal("backoffice.core.invalid_response_contract", result.ProblemCode);
    }

    [Fact]
    public async Task LogoutCallsOfficialEndpoint()
    {
        HttpRequestMessage? captured = null;
        var result = await Client(new StubHandler(request => { captured = Clone(request); return new HttpResponseMessage(HttpStatusCode.NoContent); })).LogoutAsync("token", TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess); Assert.Equal("/api/v1/auth/logout", captured!.RequestUri!.AbsolutePath); Assert.Equal(HttpMethod.Post, captured.Method);
    }

    [Fact]
    public async Task RecoveryCallsNeverSendAFakeCompanyContext()
    {
        List<HttpRequestMessage>requests=[];Guid id=Guid.NewGuid();var client=Client(new StubHandler(request=>{requests.Add(Clone(request));return request.Method==HttpMethod.Get?Json(HttpStatusCode.OK,"[]"):Json(HttpStatusCode.OK,$"{{\"id\":\"{id}\",\"code\":\"C\",\"displayName\":\"Company\",\"isActive\":true,\"version\":2}}");}));
        await client.GetRecoveryCompaniesAsync("token",TestContext.Current.CancellationToken);await client.ActivateRecoveryCompanyAsync("token",id,1,TestContext.Current.CancellationToken);
        Assert.All(requests,x=>Assert.False(x.Headers.Contains("X-Company-ID")));Assert.All(requests,x=>Assert.Equal("Bearer",x.Headers.Authorization!.Scheme));Assert.Equal("/api/v1/organization/recovery/companies",requests[0].RequestUri!.AbsolutePath);Assert.EndsWith($"/{id}/activation",requests[1].RequestUri!.AbsolutePath,StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict,ApiFailure.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity,ApiFailure.Validation)]
    [InlineData(HttpStatusCode.BadRequest,ApiFailure.Validation)]
    public async Task MutationFailuresMapConcurrencyAndValidation(HttpStatusCode status,ApiFailure expected)
    {
        var result=await Client(new StubHandler(_=>Json(status,"{\"code\":\"test.failure\"}"))).SendAsync(HttpMethod.Put,"api/v1/test",new{version=1},"token",Guid.NewGuid(),TestContext.Current.CancellationToken);
        Assert.Equal(expected,result.Failure);Assert.Equal("test.failure",result.ProblemCode);Assert.Equal((int)status,result.StatusCode);
    }

    [Fact]
    public async Task SecureUploadCarriesCompanyBearerAndMultipartWithoutStorageKey()
    {
        HttpRequestMessage? captured=null;string? content=null;Guid company=Guid.NewGuid();
        var client=Client(new StubHandler(request=>{captured=Clone(request);content=request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();return Json(HttpStatusCode.Created,"{}");}));
        await using MemoryStream stream=new([1,2,3]);await client.UploadAsync("api/v1/files/upload","safe.png","image/png",stream,3,"opaque",company,TestContext.Current.CancellationToken);
        Assert.Equal("Bearer",captured!.Headers.Authorization!.Scheme);Assert.Equal(company.ToString(),captured.Headers.GetValues("X-Company-ID").Single());Assert.Contains("safe.png",content);Assert.DoesNotContain("storageKey",content,StringComparison.OrdinalIgnoreCase);
    }

    private static CoreApiClient Client(HttpMessageHandler handler) => new(new HttpClient(handler) { BaseAddress = new Uri("http://core/") }, NullLogger<CoreApiClient>.Instance);
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpRequestMessage Clone(HttpRequestMessage source) { var clone = new HttpRequestMessage(source.Method, source.RequestUri); foreach (var header in source.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value); return clone; }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request)); }
}
