using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Orders;

namespace BARANG.Core.Application.Customers;

public sealed record WalletTopUpView(Guid Id, string Number, Guid CustomerId, decimal Amount, string CurrencyCode, string Status, string GatewayCode, Guid PaymentToken, string? GatewayReference, DateTimeOffset CreatedAtUtc, DateTimeOffset? PaidAtUtc, long Version);
public sealed record CreateTopUpCommand(decimal Amount, string CurrencyCode);
public sealed record WalletPayResultView(Guid OrderId, string? OrderNumber, decimal AmountPaid, string CurrencyCode, decimal BalanceAfter, Guid PaymentId);
public sealed record WalletAdjustCommand(string Direction, decimal Amount, string? ReferenceNote);
public sealed record WalletAdminTransactionView(Guid Id, Guid CustomerId, string? CustomerCode, string? CustomerDisplayName, string Direction, decimal Amount, string CurrencyCode, string ReasonCode, string? ReferenceNote, Guid? OrderId, DateTimeOffset CreatedAtUtc);

public interface IWalletAdministration
{
    Task<Result<WalletTopUpView>> CreateTopUpAsync(Guid customerId, CreateTopUpCommand c, CancellationToken ct);
    Task<Result<IReadOnlyList<WalletTopUpView>>> ListTopUpsAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken ct);
    Task<Result<WalletTopUpView>> CancelTopUpAsync(Guid customerId, Guid topUpId, long version, CancellationToken ct);
    Task<Result<WalletTopUpView>> GetByTokenAsync(Guid token, CancellationToken ct);
    Task<Result<WalletTopUpView>> CompleteByTokenAsync(Guid token, bool success, string? gatewayReference, CancellationToken ct);
    Task<Result<WalletPayResultView>> PayOrderFromWalletAsync(Guid orderId, long version, CancellationToken ct);
    Task<Result<WalletAdminTransactionView>> AdminAdjustAsync(Guid customerId, WalletAdjustCommand c, CancellationToken ct);
    Task<Result<IReadOnlyList<WalletAdminTransactionView>>> AdminListTransactionsAsync(Guid? customerId, string? reason, int page, int pageSize, CancellationToken ct);
    Task<Result<IReadOnlyList<WalletTopUpView>>> AdminListTopUpsAsync(string? status, Guid? customerId, int page, int pageSize, CancellationToken ct);
}

public static class WalletPolicy
{
    public const string CurrencyCode = "IRR";
    public const decimal MinimumTopUpAmount = 10_000m;
    public const string ReasonTopUp = "topup";
    public const string ReasonPurchase = "purchase";
    public const string ReasonRefund = "order_refund";
    public const string ReasonManual = "manual_adjustment";
    public const string PaymentMethodWallet = "wallet";
}
