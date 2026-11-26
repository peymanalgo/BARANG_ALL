using System.Security.Claims;
using System.Text.Encodings.Web;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BARANG.Core.Api.Security;

public sealed class BearerAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IRequestIdentityResolver resolver,IApiClientIdentityResolver clients) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "BARANG-Bearer";
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string header = Request.Headers.Authorization.ToString();
        ActorContext?actor;if(header.StartsWith("ApiKey ",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(header[7..]))actor=await clients.ResolveAsync(header[7..].Trim(),Context.RequestAborted);else if(header.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(header[7..]))actor=await resolver.ResolveAsync(header[7..].Trim(),Context.RequestAborted);else return AuthenticateResult.NoResult();
        if (actor is null) return AuthenticateResult.Fail("Invalid credential.");
        ClaimsIdentity identity = new([new Claim(ClaimTypes.NameIdentifier, actor.ActorId!), new Claim(ClaimTypes.Name, actor.ActorUsername ?? actor.ActorId!),new Claim("barang.actor_type",actor.ActorType.ToString())], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
