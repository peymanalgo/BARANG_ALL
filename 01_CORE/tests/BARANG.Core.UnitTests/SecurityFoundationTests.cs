using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Security;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class SecurityFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
    [Fact] public void UsernameIsNormalizedAndImmutable() { ApplicationUser user = ApplicationUser.Create(Guid.NewGuid(), " Admin.User ", "Admin", null, "hash", Now).Value; Assert.Equal("ADMIN.USER", user.Username); }
    [Fact] public void RoleCodeIsNormalized() { Role role = Role.Create(Guid.NewGuid(), " Security.Admin ", "Admin", false, Now).Value; Assert.Equal("security.admin", role.Code); }
    [Theory] [InlineData("security.user.manage", true)] [InlineData("Security.User", false)] [InlineData("page button", false)] public void PermissionCodeValidationIsTechnical(string value, bool expected) => Assert.Equal(expected, PermissionCode.IsValid(value));
    [Fact] public void ExpiredAssignmentsAreNotEffective() { UserRole role = UserRole.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-2), Now.AddDays(-1), null, Now.AddDays(-2)).Value; Assert.False(role.IsEffective(Now)); }
    [Fact] public void CompanyAccessRejectsInvalidPeriod() => Assert.True(UserCompany.Create(Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddMinutes(-1), null, Now).IsFailure);
    [Fact] public void PasswordHashIsSaltedAndVerifiable() { PasswordCredentialService service = new(); string first = service.HashPassword("StrongPassword123").Value; string second = service.HashPassword("StrongPassword123").Value; Assert.NotEqual(first, second); Assert.True(service.Verify("StrongPassword123", first)); Assert.False(service.Verify("WrongPassword123", first)); Assert.False(service.Verify("StrongPassword123", null)); }
    [Fact] public void WeakPasswordIsRejected() => Assert.True(new PasswordCredentialService().HashPassword("weak").IsFailure);
    [Fact] public void SessionTokensAreRandomAndOnlyHashIsStable() { SessionTokenService service = new(); var first = service.Create(); var second = service.Create(); Assert.NotEqual(first.Token, second.Token); Assert.Equal(first.Hash, service.Hash(first.Token)); Assert.DoesNotContain(first.Token, first.Hash, StringComparison.Ordinal); }
}
