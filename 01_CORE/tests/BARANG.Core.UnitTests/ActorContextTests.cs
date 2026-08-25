using BARANG.Core.Application.Abstractions.Actors;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class ActorContextTests
{
    [Fact]
    public void ActorCapturesProviderNeutralIdentityAndRoles()
    {
        string[] roles = ["operator", "reviewer", "operator"];

        ActorContext actor = new(
            "actor-42",
            ActorType.InternalUser,
            true,
            SourceChannel.Backoffice,
            userId: "user-42",
            roleCodes: roles);

        roles[0] = "changed";
        Assert.Equal(["operator", "reviewer"], actor.RoleCodes);
        Assert.Equal("user-42", actor.UserId);
        Assert.True(actor.IsAuthenticated);
    }

    [Fact]
    public void AuthenticatedActorRequiresIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new ActorContext(
            null,
            ActorType.MobileUser,
            true,
            SourceChannel.Mobile));
    }

    [Fact]
    public void AnonymousActorIsUnauthenticated()
    {
        Assert.Equal(ActorType.Anonymous, ActorContext.Anonymous.ActorType);
        Assert.False(ActorContext.Anonymous.IsAuthenticated);
        Assert.Empty(ActorContext.Anonymous.RoleCodes);
    }

    [Fact]
    public void RoleCodesRejectLocalizedTechnicalIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new ActorContext(
            "actor-42",
            ActorType.InternalUser,
            true,
            SourceChannel.Backoffice,
            roleCodes: ["مدیر"]));
    }
}
