using BARANG.Core.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace BARANG.Core.Api.Security;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;
public sealed class PermissionAuthorizationHandler(IPermissionAuthorizationService authorization) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if ((await authorization.AuthorizeAsync(requirement.Code, CancellationToken.None)).IsSuccess) context.Succeed(requirement);
    }
}
