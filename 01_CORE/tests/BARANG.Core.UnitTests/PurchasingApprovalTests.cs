using BARANG.Core.Domain.Inventory;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class PurchasingApprovalTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RequestRequiresSubmissionAndRecordsRevisionBoundDecision()
    {
        Guid company = Guid.NewGuid(), warehouse = Guid.NewGuid(), creator = Guid.NewGuid(), approver = Guid.NewGuid();
        PurchaseRequest request = PurchaseRequest.Create(company, warehouse, creator, "restock", null, Now).Value;
        Assert.Equal(PurchaseRequestStatus.Draft, request.Status);
        Assert.True(request.Approve(approver, request.Version, Now).IsFailure);
        Assert.True(request.Submit(request.Version, Now).IsSuccess);
        long submittedVersion = request.Version;
        Assert.True(request.Approve(approver, submittedVersion, Now).IsSuccess);
        Assert.Equal(PurchaseRequestStatus.Approved, request.Status);
        Assert.Equal(approver, request.ApprovedByUserId);
        Assert.Equal(submittedVersion, request.ApprovedVersion);
        Assert.True(request.Submit(request.Version, Now).IsFailure);
    }

    [Fact]
    public void OrderCannotSendWithoutApprovalAndRejectionCanBeResubmitted()
    {
        PurchaseOrder order = PurchaseOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PO-1", "USD", null, Now);
        Assert.True(order.Submit(order.Version, Now).IsSuccess);
        Assert.True(order.Send(order.Version, Now).IsFailure);
        Assert.True(order.Reject(order.Version, "supplier terms changed", Now).IsSuccess);
        Assert.True(order.Submit(order.Version, Now).IsSuccess);
        long submittedVersion = order.Version;
        Assert.True(order.Approve(Guid.NewGuid(), submittedVersion, Now).IsSuccess);
        Assert.True(order.Send(order.Version, Now).IsSuccess);
        Assert.Equal(PurchaseOrderStatus.Sent, order.Status);
    }
}
