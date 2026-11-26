using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Inventory;

public sealed partial class InventoryAdministration
{
    public Task<Result<PurchaseRequestView>> DecideRequestAsync(Guid id, bool approve, PurchasingDecisionCommand command, CancellationToken cancellationToken) =>
        tx.ExecuteAsync(async t =>
        {
            PurchaseRequest? request = await db.PurchaseRequests.SingleOrDefaultAsync(x => x.Id == id, t);
            if (request is null) return Result.Failure<PurchaseRequestView>(InventoryErrors.NotFound);
            Result eligibility = await EnsureApproverAsync(SecurityPermissions.PurchasingRequestApprove, request.RequestedByUserId, t);
            if (eligibility.IsFailure) return Result.Failure<PurchaseRequestView>(eligibility.Error);
            Result decision = approve ? request.Approve(Actor, command.Version, Now) : request.Reject(command.Version, command.Reason ?? string.Empty, Now);
            if (decision.IsFailure) return Result.Failure<PurchaseRequestView>(decision.Error);
            History("PURCHASE_REQUEST", id, approve ? "Approved" : "Rejected", command.Reason);
            Log(approve ? "purchasing.request.approved" : "purchasing.request.rejected", id);await notifications.PublishBusinessEventInsideTransactionAsync(approve?"purchasing.request.approved":"purchasing.request.rejected",id.ToString("N"),approve?"purchasing.request.approved":"purchasing.request.rejected",new{purchaseRequestId=id,reason=command.Reason},BARANG.Core.Domain.Security.SecurityPermissions.PurchasingManage,t);
            return Result.Success(RequestView(request));
        }, cancellationToken);

    public Task<Result<PurchaseOrderView>> DecideOrderAsync(Guid id, bool approve, PurchasingDecisionCommand command, CancellationToken cancellationToken) =>
        tx.ExecuteAsync(async t =>
        {
            PurchaseOrder? order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, t);
            if (order is null) return Result.Failure<PurchaseOrderView>(InventoryErrors.NotFound);
            Guid creator = await db.PurchasingHistory.Where(x => x.EntityType == "PURCHASE_ORDER" && x.EntityId == id && x.EventType == "Created").OrderBy(x => x.OccurredAtUtc).Select(x => x.ActorUserId).FirstOrDefaultAsync(t);
            Result eligibility = await EnsureApproverAsync(SecurityPermissions.PurchasingOrderApprove, creator, t);
            if (eligibility.IsFailure) return Result.Failure<PurchaseOrderView>(eligibility.Error);
            Result decision = approve ? order.Approve(Actor, command.Version, Now) : order.Reject(command.Version, command.Reason ?? string.Empty, Now);
            if (decision.IsFailure) return Result.Failure<PurchaseOrderView>(decision.Error);
            History("PURCHASE_ORDER", id, approve ? "Approved" : "Rejected", command.Reason);
            Log(approve ? "purchasing.order.approved" : "purchasing.order.rejected", id);await notifications.PublishBusinessEventInsideTransactionAsync(approve?"purchasing.order.approved":"purchasing.order.rejected",id.ToString("N"),approve?"purchasing.order.approved":"purchasing.order.rejected",new{purchaseOrderId=id,reason=command.Reason},BARANG.Core.Domain.Security.SecurityPermissions.PurchasingManage,t);
            return Result.Success(await PurchaseOrderView(order, t));
        }, cancellationToken);

    public Task<Result<PurchaseOrderView>> SendOrderAsync(Guid id, long version, CancellationToken cancellationToken) =>
        tx.ExecuteAsync(async t =>
        {
            PurchaseOrder? order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, t);
            if (order is null) return Result.Failure<PurchaseOrderView>(InventoryErrors.NotFound);
            Result sent = order.Send(version, Now);
            if (sent.IsFailure) return Result.Failure<PurchaseOrderView>(sent.Error);
            History("PURCHASE_ORDER", id, "Sent", null);
            Log("purchasing.order.sent", id);await notifications.PublishBusinessEventInsideTransactionAsync("purchasing.order.sent",id.ToString("N"),"purchasing.order.sent",new{purchaseOrderId=id},BARANG.Core.Domain.Security.SecurityPermissions.PurchasingManage,t);
            return Result.Success(await PurchaseOrderView(order, t));
        }, cancellationToken);

    private async Task<Result> EnsureApproverAsync(string permissionCode, Guid creator, CancellationToken ct)
    {
        Guid permissionId = await db.Permissions.Where(x => x.Code == permissionCode).Select(x => x.Id).SingleOrDefaultAsync(ct);
        if (permissionId == Guid.Empty) return Result.Failure(InventoryErrors.ApproverNotConfigured);
        bool configured = await (from user in db.ApplicationUsers
                                 join membership in db.UserCompanies on user.Id equals membership.UserId
                                 where membership.CompanyId == Company && user.AccountState == UserAccountState.Active && membership.ValidFromUtc <= Now && (membership.ValidToUtc == null || membership.ValidToUtc > Now)
                                 where db.UserPermissions.Any(x => x.UserId == user.Id && x.CompanyId == Company && x.PermissionId == permissionId)
                                    || (from ur in db.UserRoles join rp in db.RolePermissions on ur.RoleId equals rp.RoleId where ur.UserId == user.Id && ur.CompanyId == Company && rp.PermissionId == permissionId && ur.ValidFromUtc <= Now && (ur.ValidToUtc == null || ur.ValidToUtc > Now) select ur).Any()
                                 select user.Id).AnyAsync(ct);
        if (!configured) return Result.Failure(InventoryErrors.ApproverNotConfigured);
        if (creator == Actor) return Result.Failure(InventoryErrors.CreatorCannotApprove);
        bool actorEligible = await (from user in db.ApplicationUsers
                                    join membership in db.UserCompanies on user.Id equals membership.UserId
                                    where user.Id == Actor && user.AccountState == UserAccountState.Active && membership.CompanyId == Company && membership.ValidFromUtc <= Now && (membership.ValidToUtc == null || membership.ValidToUtc > Now)
                                    where db.UserPermissions.Any(x => x.UserId == Actor && x.CompanyId == Company && x.PermissionId == permissionId)
                                       || (from ur in db.UserRoles join rp in db.RolePermissions on ur.RoleId equals rp.RoleId where ur.UserId == Actor && ur.CompanyId == Company && rp.PermissionId == permissionId && ur.ValidFromUtc <= Now && (ur.ValidToUtc == null || ur.ValidToUtc > Now) select ur).Any()
                                    select user.Id).AnyAsync(ct);
        return actorEligible ? Result.Success() : Result.Failure(InventoryErrors.ApprovalUnauthorized);
    }
}
