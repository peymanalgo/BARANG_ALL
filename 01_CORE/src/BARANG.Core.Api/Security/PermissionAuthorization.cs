using BARANG.Core.Application.Security;
using Microsoft.AspNetCore.Authorization;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Api.Security;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;
public sealed class PermissionAuthorizationHandler(IPermissionAuthorizationService authorization, ITransactionExecutor transactions, IAuditWriter audit) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        Result result = await authorization.AuthorizeAsync(requirement.Code, CancellationToken.None);
        if (result.IsSuccess) { context.Succeed(requirement); return; }
        if (context.User.Identity?.IsAuthenticated == true)
            await transactions.ExecuteAsync(_ => { audit.Write(new AuditWriteRequest(
                AuditActions.AuthorizationDenied, "permission", requirement.Code,
                AuditOutcome.Failure, result.Error.Code,
                Metadata: new Dictionary<string, object?> { ["permission"] = requirement.Code }));
                return Task.FromResult(Result.Success()); }, CancellationToken.None);
    }
}

// سیاست چندکُدِ «یا»: کافی است کاربر یکی از کدها را داشته باشد (مثلاً نمای ۳۶۰ سفارش برای کاربر کارتابل با workflow.read)
public sealed record AnyPermissionRequirement(params string[] Codes) : IAuthorizationRequirement;
public sealed class AnyPermissionAuthorizationHandler(IPermissionAuthorizationService authorization) : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPermissionRequirement requirement)
    {
        foreach (string code in requirement.Codes)
        {
            if ((await authorization.AuthorizeAsync(code, CancellationToken.None)).IsSuccess) { context.Succeed(requirement); return; }
        }
    }
}
