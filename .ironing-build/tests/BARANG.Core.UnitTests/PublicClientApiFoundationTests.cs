using BARANG.Core.Domain.Security;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class PublicClientApiFoundationTests
{
    [Fact]public void ClientScopesAreStableUniqueTechnicalCodes(){Assert.Equal(8,SecurityPermissions.ClientScopes.Count);Assert.Equal(SecurityPermissions.ClientScopes.Count,SecurityPermissions.ClientScopes.Distinct(StringComparer.Ordinal).Count());Assert.All(SecurityPermissions.ClientScopes,x=>Assert.Matches("^[a-z][a-z0-9.]+$",x));}
    [Fact]public void ApiClientCredentialRotationIsConcurrentAndNeverExposesSecret(){DateTimeOffset now=DateTimeOffset.UtcNow;ApiClient client=ApiClient.Create(Guid.NewGuid(),"partner_1","Partner",now).Value;Assert.True(client.RotateCredential(new string('a',64),now.AddDays(1),1,now).IsSuccess);Assert.Equal(new string('a',64),client.CredentialFingerprint);Assert.True(client.RotateCredential(new string('b',64),null,1,now).IsFailure);Assert.Equal(2,client.Version);}
    [Fact]public void ApiClientActivationUsesOptimisticConcurrency(){ApiClient client=ApiClient.Create(Guid.NewGuid(),"mobile_1","Mobile",DateTimeOffset.UtcNow).Value;Assert.True(client.SetActive(false,1,DateTimeOffset.UtcNow).IsSuccess);Assert.False(client.IsActive);Assert.True(client.SetActive(true,1,DateTimeOffset.UtcNow).IsFailure);}
}
