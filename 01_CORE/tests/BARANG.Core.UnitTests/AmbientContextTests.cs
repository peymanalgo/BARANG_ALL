using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Infrastructure.Context;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class AmbientContextTests
{
    [Fact]
    public async Task CorrelationContextFlowsAcrossAsyncWorkAndRestoresScope()
    {
        CorrelationContext context = new();

        using (context.BeginScope("outer-id"))
        {
            await Task.Yield();
            Assert.Equal("outer-id", context.CorrelationId);

            using (context.BeginScope("inner-id"))
            {
                Assert.Equal("inner-id", context.CorrelationId);
            }

            Assert.Equal("outer-id", context.CorrelationId);
        }

        Assert.Null(context.CorrelationId);
    }

    [Fact]
    public void CurrentActorDefaultsToAnonymousAndRestoresScope()
    {
        CurrentActorContext context = new();
        ActorContext actor = new(
            "job-1",
            ActorType.BackgroundJob,
            false,
            SourceChannel.BackgroundJob);

        Assert.Same(ActorContext.Anonymous, context.Actor);

        using (context.BeginScope(actor))
        {
            Assert.Same(actor, context.Actor);
        }

        Assert.Same(ActorContext.Anonymous, context.Actor);
    }
}
