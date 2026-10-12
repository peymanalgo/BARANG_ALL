using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    public Task<ApiResult<IReadOnlyList<RecoveryCompanyView>>> GetRecoveryCompaniesAsync(string token,CancellationToken ct)=>
        SendAuthenticatedAsync<IReadOnlyList<RecoveryCompanyView>>(HttpMethod.Get,"api/v1/organization/recovery/companies",token,null,null,ct);

    public Task<ApiResult<RecoveryCompanyView>> ActivateRecoveryCompanyAsync(string token,Guid companyId,long version,CancellationToken ct)=>
        SendAuthenticatedAsync<RecoveryCompanyView>(HttpMethod.Put,$"api/v1/organization/recovery/companies/{companyId}/activation",token,null,new{isActive=true,version},ct);

    public async Task<ApiResult<bool>> LogoutAsync(string token, CancellationToken ct)
    {
        ApiResult<object> result = await SendAuthenticatedAsync<object>(HttpMethod.Post, "api/v1/auth/logout", token, null, null, ct);
        return result.IsSuccess ? ApiResult<bool>.Success(true) : ApiResult<bool>.Failed(result.Failure, result.ProblemCode);
    }

    public Task<ApiResult<JsonElement>> GetAsync(string path,string token,Guid companyId,CancellationToken ct)=>SendAuthenticatedAsync<JsonElement>(HttpMethod.Get,path,token,companyId,null,ct);
    public Task<ApiResult<JsonElement>> SendAsync(HttpMethod method,string path,object? body,string token,Guid companyId,CancellationToken ct)=>SendAuthenticatedAsync<JsonElement>(method,path,token,companyId,body,ct);
    public Task<ApiResult<JsonElement>> UploadAsync(string path,string fileName,string contentType,Stream content,long length,string token,Guid companyId,CancellationToken ct)
    {
        MultipartFormDataContent form=new();StreamContent file=new(content);file.Headers.ContentType=new MediaTypeHeaderValue(contentType);file.Headers.ContentLength=length;form.Add(file,"file",fileName);HttpRequestMessage request=new(HttpMethod.Post,path){Content=form};request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);request.Headers.Add("X-Company-ID",companyId.ToString());return SendAsync<JsonElement>(request,ct);
    }
    public async Task<ApiResult<FileDownload>> DownloadAsync(string path,string token,Guid companyId,CancellationToken ct)
    {
        try{using HttpRequestMessage request=new(HttpMethod.Get,path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);request.Headers.Add("X-Company-ID",companyId.ToString());using HttpResponseMessage response=await httpClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);if(!response.IsSuccessStatusCode)return ApiResult<FileDownload>.Failed(response.StatusCode switch{HttpStatusCode.Unauthorized=>ApiFailure.Unauthorized,HttpStatusCode.Forbidden=>ApiFailure.Forbidden,HttpStatusCode.NotFound=>ApiFailure.NotFound,HttpStatusCode.Conflict=>ApiFailure.Conflict,_=>ApiFailure.Unexpected});byte[] bytes=await response.Content.ReadAsByteArrayAsync(ct);string name=response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName?.Trim('"')??"download";return ApiResult<FileDownload>.Success(new(bytes,response.Content.Headers.ContentType?.MediaType??"application/octet-stream",name));}catch(HttpRequestException){return ApiResult<FileDownload>.Failed(ApiFailure.Unavailable);}
    }

    private Task<ApiResult<T>> SendAuthenticatedAsync<T>(HttpMethod method, string path, string token, Guid? companyId, object? body, CancellationToken ct)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (companyId.HasValue) request.Headers.Add("X-Company-ID", companyId.Value.ToString());
        if (body is not null) request.Content = JsonContent.Create(NormalizeWireEnums(body));
        return SendAsync<T>(request, ct);
    }

    private static object NormalizeWireEnums(object body)
    {
        JsonNode node=JsonSerializer.SerializeToNode(body,JsonOptions)!;
        if(node is not JsonObject value)return node;
        Normalize(value,"entityType",new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase){{"Artwork",0},{"Order",1},{"OrderItem",2},{"OrderPrintOperation",3},{"Supplier",4},{"SupplierContract",5},{"SupplierDocument",6},{"Equipment",7},{"WorkflowDecisionAttachment",8}});
        Normalize(value,"purpose",new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase){{"Source",0},{"Attachment",1},{"Reference",2}});
        return value;
    }
    private static void Normalize(JsonObject value,string property,IReadOnlyDictionary<string,int> codes)
    {
        if(value[property] is JsonValue raw&&raw.TryGetValue<string>(out string? text)&&text is not null&&codes.TryGetValue(text,out int code))value[property]=code;
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
                    if (response.StatusCode == HttpStatusCode.NoContent) return new ApiResult<T>(default, ApiFailure.None, null, null, (int)response.StatusCode);
                    T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                    return value is null ? ApiResult<T>.Failed(ApiFailure.ContractError, "backoffice.core.empty_response", statusCode: (int)response.StatusCode) : new ApiResult<T>(value, ApiFailure.None, null, null, (int)response.StatusCode);
                }
                CoreProblem? problem = null;
                try { problem = await response.Content.ReadFromJsonAsync<CoreProblem>(JsonOptions, ct); } catch (JsonException) { }
                ApiFailure failure = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized when login => ApiFailure.InvalidCredentials,
                    HttpStatusCode.Unauthorized => ApiFailure.Unauthorized,
                    HttpStatusCode.Forbidden => ApiFailure.Forbidden,
                    HttpStatusCode.Conflict => ApiFailure.Conflict,
                    HttpStatusCode.NotFound => ApiFailure.NotFound,
                    HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiFailure.Validation,
                    _ when (int)response.StatusCode >= 500 => ApiFailure.Unexpected,
                    _ => ApiFailure.Unexpected
                };
                logger.LogWarning("Core request {Method} {Path} failed with status {Status} and code {Code}", request.Method, request.RequestUri, (int)response.StatusCode, problem?.Code);
                return ApiResult<T>.Failed(failure, problem?.Code, problem?.BusinessContext ?? problem?.Description ?? problem?.Detail, (int)response.StatusCode);
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
        catch (JsonException exception)
        {
            logger.LogError(exception, "Core API returned an invalid response contract for {Method} {Path}", request.Method, request.RequestUri);
            return ApiResult<T>.Failed(ApiFailure.ContractError, "backoffice.core.invalid_response_contract");
        }
    }
}
