using System.Security.Claims;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Infrastructure.Context;

namespace BARANG.Core.Api.Security;

public sealed class CurrentActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CurrentActorContext currentActor)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(userId, out Guid id))
        {
            using IDisposable scope = currentActor.BeginScope(new ActorContext(id.ToString(), ActorType.InternalUser, true, SourceChannel.Backoffice, id.ToString(), actorUsername: context.User.Identity?.Name));
            await next(context);
            return;
        }
        await next(context);
    }
}
