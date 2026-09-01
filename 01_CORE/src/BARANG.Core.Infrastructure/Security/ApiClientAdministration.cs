using System.Security.Cryptography;
using System.Text;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Security;
#pragma warning disable CA1862 // PostgreSQL compares the persisted normalized client code.

public sealed class ApiClientAdministration(CoreDbContext db,ITransactionExecutor tx,IAuditWriter audit,IPasswordCredentialService passwords,TimeProvider time):IApiClientAdministration,IApiClientIdentityResolver
{
    public Task<Result<ApiClientCreatedView>>CreateAsync(CreateApiClientCommand command,CancellationToken ct)=>tx.ExecuteAsync(async token=>
    {
        if(command.CompanyIds.Count==0||command.Scopes.Count==0||command.CompanyIds.Distinct().Count()!=command.CompanyIds.Count||command.Scopes.Any(x=>!SecurityPermissions.ClientScopes.Contains(x,StringComparer.Ordinal)))return Result.Failure<ApiClientCreatedView>(SecurityErrors.InvalidIdentity);
        if(await db.ApiClients.AnyAsync(x=>x.Code==command.Code.Trim().ToUpperInvariant(),token)||await db.Companies.CountAsync(x=>command.CompanyIds.Contains(x.Id)&&x.IsActive,token)!=command.CompanyIds.Count)return Result.Failure<ApiClientCreatedView>(SecurityErrors.InvalidIdentity);
        List<Permission>permissions=await db.Permissions.Where(x=>command.Scopes.Contains(x.Code)).ToListAsync(token);if(permissions.Count!=command.Scopes.Count)return Result.Failure<ApiClientCreatedView>(SecurityErrors.InvalidIdentity);
        string secret=GenerateSecret(),hash=Hash(secret);ApiClient client=ApiClient.Create(Guid.NewGuid(),command.Code,command.Name,time.GetUtcNow()).Value;Result rotated=client.RotateCredential(hash,command.CredentialExpiresAtUtc,client.Version,time.GetUtcNow());if(rotated.IsFailure)return Result.Failure<ApiClientCreatedView>(rotated.Error);Result<string>principalCredential=passwords.HashPassword($"Api{GenerateSecret()}a1");if(principalCredential.IsFailure)return Result.Failure<ApiClientCreatedView>(principalCredential.Error);ApplicationUser principal=ApplicationUser.Create(client.Id,$"API_{client.Id:N}",client.Name,null,principalCredential.Value,time.GetUtcNow()).Value;db.Add(principal);db.Add(client);db.AddRange(command.CompanyIds.Select(x=>ApiClientCompany.Create(client.Id,x)));db.AddRange(permissions.Select(x=>ApiClientPermission.Create(client.Id,x.Id)));audit.Write(new("api_client.created","api_client",client.Id.ToString(),ChangeKind:AuditChangeKind.Added));return Result.Success(new ApiClientCreatedView(client.Id,client.Code,client.Name,secret,command.CompanyIds,command.Scopes,client.Version));
    },ct);
    public Task<Result<ApiClientSecretView>>RotateAsync(Guid id,long version,DateTimeOffset?expiresAtUtc,CancellationToken ct)=>tx.ExecuteAsync(async token=>{ApiClient?client=await db.ApiClients.SingleOrDefaultAsync(x=>x.Id==id,token);if(client is null)return Result.Failure<ApiClientSecretView>(SecurityErrors.NotFound);string secret=GenerateSecret();Result r=client.RotateCredential(Hash(secret),expiresAtUtc,version,time.GetUtcNow());if(r.IsFailure)return Result.Failure<ApiClientSecretView>(r.Error);audit.Write(new("api_client.credential.rotated","api_client",id.ToString(),ChangeKind:AuditChangeKind.Modified));return Result.Success(new ApiClientSecretView(id,secret,expiresAtUtc,client.Version));},ct);
    public Task<Result>SetActiveAsync(Guid id,bool active,long version,CancellationToken ct)=>tx.ExecuteAsync(async token=>{ApiClient?client=await db.ApiClients.SingleOrDefaultAsync(x=>x.Id==id,token);if(client is null)return Result.Failure(SecurityErrors.NotFound);Result r=client.SetActive(active,version,time.GetUtcNow());if(r.IsSuccess)audit.Write(new("api_client.activation.changed","api_client",id.ToString(),ChangeKind:AuditChangeKind.Modified));return r;},ct);
    public async Task<ActorContext?>ResolveAsync(string credential,CancellationToken ct)
    {
        int dot=credential.IndexOf('.');if(dot<1||!Guid.TryParse(credential[..dot],out Guid id)||credential.Length-dot-1<32)return null;ApiClient?client=await db.ApiClients.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.IsActive,ct);if(client?.CredentialFingerprint is null||(client.CredentialExpiresAtUtc is DateTimeOffset expires&&expires<=time.GetUtcNow()))return null;byte[]actual=Convert.FromHexString(Hash(credential[(dot+1)..])),expected=Convert.FromHexString(client.CredentialFingerprint);return CryptographicOperations.FixedTimeEquals(actual,expected)?new ActorContext(client.Id.ToString(),ActorType.ApiClient,true,SourceChannel.PartnerApi,userId:client.Id.ToString(),apiClientId:client.Id.ToString(),actorUsername:client.Code):null;
    }
    private static string GenerateSecret()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
#pragma warning restore CA1862
