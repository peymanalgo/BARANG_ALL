using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BARANG.Backoffice.Api;

public sealed class CoreApiClient(HttpClient httpClient, ILogger<CoreApiClient> logger) : ICoreApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<LoginResult>> LoginAsync(string username, string password, CancellationToken ct) =>
        SendAsync<LoginResult>(new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/login") { Content = JsonContent.Create(new LoginRequest(username, password)) }, ct, true);

    public Task<ApiResult<CurrentUserView>> GetMeAsync(string token, Guid? companyId, CancellationToken ct) =>
        SendAuthenticatedAsync<CurrentUserView>(HttpMethod.Get, "api/v1/auth/me", token, companyId, null, ct);

    public Task<ApiResult<IReadOnlyList<MyCompanyView>>> GetMyCompaniesAsync(string token, CancellationToken ct) =>
        SendAuthenticatedAsync<IReadOnlyList<MyCompanyView>>(HttpMethod.Get, "api/v1/auth/me/companies", token, null, null, ct);

    public Task<ApiResult<CompanySelectionView>> SelectCompanyAsync(string token, Guid companyId, CancellationToken ct) =>
        SendAuthenticatedAsync<CompanySelectionView>(HttpMethod.Post, "api/v1/auth/me/company", token, null, new { companyId }, ct);

    public async Task<ApiResult<bool>> LogoutAsync(string token, CancellationToken ct)
    {
        ApiResult<object> result = await SendAuthenticatedAsync<object>(HttpMethod.Post, "api/v1/auth/logout", token, null, null, ct);
        return result.IsSuccess ? ApiResult<bool>.Success(true) : ApiResult<bool>.Failed(result.Failure, result.ProblemCode);
    }

    private Task<ApiResult<T>> SendAuthenticatedAsync<T>(HttpMethod method, string path, string token, Guid? companyId, object? body, CancellationToken ct)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (companyId.HasValue) request.Headers.Add("X-Company-ID", companyId.Value.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return SendAsync<T>(request, ct);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct, bool login = false)
    {
        try
        {
            using (request)
            using (HttpResponseMessage response = await httpClient.SendAsync(request, ct))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NoContent) return ApiResult<T>.Success(default!);
                    T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                    return value is null ? ApiResult<T>.Failed(ApiFailure.Unexpected) : ApiResult<T>.Success(value);
                }
                CoreProblem? problem = null;
                try { problem = await response.Content.ReadFromJsonAsync<CoreProblem>(JsonOptions, ct); } catch (JsonException) { }
                ApiFailure failure = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized when login => ApiFailure.InvalidCredentials,
                    HttpStatusCode.Unauthorized => ApiFailure.Unauthorized,
                    HttpStatusCode.Forbidden => ApiFailure.Forbidden,
                    HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiFailure.Validation,
                    _ when (int)response.StatusCode >= 500 => ApiFailure.Unexpected,
                    _ => ApiFailure.Unexpected
                };
                logger.LogWarning("Core request {Method} {Path} failed with status {Status} and code {Code}", request.Method, request.RequestUri, (int)response.StatusCode, problem?.Code);
                return ApiResult<T>.Failed(failure, problem?.Code);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Core API is unavailable for {Method} {Path}", request.Method, request.RequestUri);
            return ApiResult<T>.Failed(ApiFailure.Unavailable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Core API timed out for {Method} {Path}", request.Method, request.RequestUri);
            return ApiResult<T>.Failed(ApiFailure.Unavailable);
        }
    }
}
