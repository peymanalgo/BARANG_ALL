namespace BARANG.Backoffice.Api;

public interface ICoreApiClient
{
    Task<ApiResult<LoginResult>> LoginAsync(string username, string password, CancellationToken cancellationToken);
    Task<ApiResult<CurrentUserView>> GetMeAsync(string token, Guid? companyId, CancellationToken cancellationToken);
    Task<ApiResult<IReadOnlyList<MyCompanyView>>> GetMyCompaniesAsync(string token, CancellationToken cancellationToken);
    Task<ApiResult<CompanySelectionView>> SelectCompanyAsync(string token, Guid companyId, CancellationToken cancellationToken);
    Task<ApiResult<bool>> LogoutAsync(string token, CancellationToken cancellationToken);
}
