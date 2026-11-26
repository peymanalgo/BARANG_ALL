using System.Text.Json.Serialization;

namespace BARANG.Backoffice.Api;

public sealed class CoreApiOptions
{
    public const string Section = "CoreApi";
    public string BaseUrl { get; init; } = "http://localhost:8080";
    public int TimeoutSeconds { get; init; } = 15;
}

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAtUtc, UserView User);
public sealed record UserView(Guid Id, string Username, string? Email, string DisplayName);
public sealed record CurrentUserView(Guid UserId, string Username, string DisplayName, [property: JsonConverter(typeof(AccountStateWireConverter))] string AccountState, Guid? CurrentCompanyId, IReadOnlyList<string> EffectiveRoleCodes, IReadOnlyList<string> EffectivePermissionCodes, bool IsSystemAdministrator);
public sealed record MyCompanyView(Guid CompanyId, string Code, string DisplayName);
public sealed record CompanySelectionView(Guid CompanyId, string Code, string DisplayName, IReadOnlyList<string> EffectiveRoleCodes, IReadOnlyList<string> EffectivePermissionCodes);
public sealed record RecoveryCompanyView(Guid Id,string Code,string DisplayName,bool IsActive,long Version);
public sealed record CoreProblem(string? Type, string? Title, int? Status, string? Detail, string? Code, string? Description, string? BusinessContext);

public enum ApiFailure { None, InvalidCredentials, Unauthorized, Forbidden, Validation, Conflict, NotFound, Unavailable, ContractError, Unexpected }
public sealed record ApiResult<T>(T? Value, ApiFailure Failure, string? ProblemCode = null, string? ProblemDescription = null, int? StatusCode = null)
{
    public bool IsSuccess => Failure == ApiFailure.None;
    public static ApiResult<T> Success(T value) => new(value, ApiFailure.None);
    public static ApiResult<T> Failed(ApiFailure failure, string? code = null, string? description = null, int? statusCode = null) => new(default, failure, code, description, statusCode);
}
