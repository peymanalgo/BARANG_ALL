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
public sealed record CurrentUserView(Guid UserId, string Username, string DisplayName, int AccountState, Guid? CurrentCompanyId, IReadOnlyList<string> EffectiveRoleCodes, IReadOnlyList<string> EffectivePermissionCodes);
public sealed record MyCompanyView(Guid CompanyId, string Code, string DisplayName);
public sealed record CompanySelectionView(Guid CompanyId, string Code, string DisplayName, IReadOnlyList<string> EffectiveRoleCodes, IReadOnlyList<string> EffectivePermissionCodes);
public sealed record CoreProblem(string? Type, string? Title, int? Status, string? Detail, string? Code);

public enum ApiFailure { None, InvalidCredentials, Unauthorized, Forbidden, Validation, Conflict, NotFound, Unavailable, Unexpected }
public sealed record ApiResult<T>(T? Value, ApiFailure Failure, string? ProblemCode = null)
{
    public bool IsSuccess => Failure == ApiFailure.None;
    public static ApiResult<T> Success(T value) => new(value, ApiFailure.None);
    public static ApiResult<T> Failed(ApiFailure failure, string? code = null) => new(default, failure, code);
}
