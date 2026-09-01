using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Security;

public sealed record CreateApiClientCommand(string Code,string Name,IReadOnlyList<Guid>CompanyIds,IReadOnlyList<string>Scopes,DateTimeOffset?CredentialExpiresAtUtc);
public sealed record ApiClientCreatedView(Guid Id,string Code,string Name,string Secret,IReadOnlyList<Guid>CompanyIds,IReadOnlyList<string>Scopes,long Version);
public sealed record ApiClientSecretView(Guid Id,string Secret,DateTimeOffset?ExpiresAtUtc,long Version);
public interface IApiClientAdministration
{
    Task<Result<ApiClientCreatedView>>CreateAsync(CreateApiClientCommand command,CancellationToken ct);
    Task<Result<ApiClientSecretView>>RotateAsync(Guid id,long version,DateTimeOffset?expiresAtUtc,CancellationToken ct);
    Task<Result>SetActiveAsync(Guid id,bool active,long version,CancellationToken ct);
}
public interface IApiClientIdentityResolver{Task<ActorContext?>ResolveAsync(string credential,CancellationToken ct);}
