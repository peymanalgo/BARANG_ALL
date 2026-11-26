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
            bool apiClient=string.Equals(context.User.FindFirstValue("barang.actor_type"),ActorType.ApiClient.ToString(),StringComparison.Ordinal);ActorContext actor=apiClient?new ActorContext(id.ToString(),ActorType.ApiClient,true,SourceChannel.PartnerApi,userId:id.ToString(),apiClientId:id.ToString(),actorUsername:context.User.Identity?.Name):new ActorContext(id.ToString(), ActorType.InternalUser, true, SourceChannel.Backoffice, id.ToString(), actorUsername: context.User.Identity?.Name);using IDisposable scope = currentActor.BeginScope(actor);
            await next(context);
            return;
        }
        await next(context);
    }
}
