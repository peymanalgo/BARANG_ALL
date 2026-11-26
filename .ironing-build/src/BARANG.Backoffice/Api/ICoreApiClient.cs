namespace BARANG.Backoffice.Api;

public interface ICoreApiClient
{
    Task<ApiResult<LoginResult>> LoginAsync(string username, string password, CancellationToken cancellationToken);
    Task<ApiResult<CurrentUserView>> GetMeAsync(string token, Guid? companyId, CancellationToken cancellationToken);
    Task<ApiResult<IReadOnlyList<MyCompanyView>>> GetMyCompaniesAsync(string token, CancellationToken cancellationToken);
    Task<ApiResult<CompanySelectionView>> SelectCompanyAsync(string token, Guid companyId, CancellationToken cancellationToken);
    Task<ApiResult<IReadOnlyList<RecoveryCompanyView>>> GetRecoveryCompaniesAsync(string token,CancellationToken cancellationToken);
    Task<ApiResult<RecoveryCompanyView>> ActivateRecoveryCompanyAsync(string token,Guid companyId,long version,CancellationToken cancellationToken);
    Task<ApiResult<bool>> LogoutAsync(string token, CancellationToken cancellationToken);
    Task<ApiResult<System.Text.Json.JsonElement>> GetAsync(string path, string token, Guid companyId, CancellationToken cancellationToken);
    Task<ApiResult<System.Text.Json.JsonElement>> SendAsync(HttpMethod method, string path, object? body, string token, Guid companyId, CancellationToken cancellationToken);
    Task<ApiResult<System.Text.Json.JsonElement>> UploadAsync(string path, string fileName, string contentType, Stream content, long length, string token, Guid companyId, CancellationToken cancellationToken);
    Task<ApiResult<FileDownload>> DownloadAsync(string path, string token, Guid companyId, CancellationToken cancellationToken);
}

public readonly record struct FileDownload(byte[] Content,string ContentType,string FileName);
