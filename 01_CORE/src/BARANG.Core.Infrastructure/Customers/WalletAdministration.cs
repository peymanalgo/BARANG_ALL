using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Customers;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Domain.Pricing;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BARANG.Core.Infrastructure.Customers;

// Customer wallet module. Business flow:
//   top-up:  customer creates a Pending top-up -> payment gateway (mock for now)
//            -> CompleteByTokenAsync marks it Paid and credits the ledger
//   payment: PayOrderFromWalletAsync debits the ledger, registers an order
//            Payment (method 'wallet') and auto-approves the financial state
//   refund:  cancelling an order credits back its unpaid-reversed wallet payments
// Balance-affecting writes take a per-customer advisory lock so concurrent
// charges/payments cannot race the balance check.
public sealed class WalletAdministration(CoreDbContext db, ITransactionExecutor tx, ICompanyContext company, ICurrentActor actor, TimeProvider time) : IWalletAdministration
{
    private Guid Company => company.RequireCompanyId();
    private Guid Actor => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : throw new InvalidOperationException("Authenticated user required.");
    private DateTimeOffset Now => time.GetUtcNow();

    public Task<Result<WalletTopUpView>> CreateTopUpAsync(Guid customerId, CreateTopUpCommand c, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t)) return Result.Failure<WalletTopUpView>(WalletErrors.CustomerNotFound);
        if (c.Amount <= 0) return Result.Failure<WalletTopUpView>(WalletErrors.InvalidAmount);
        if (decimal.Round(c.Amount, 0) < WalletPolicy.MinimumTopUpAmount) return Result.Failure<WalletTopUpView>(WalletErrors.BelowMinimum);
        string currency = string.IsNullOrWhiteSpace(c.CurrencyCode) ? WalletPolicy.CurrencyCode : c.CurrencyCode.Trim().ToUpperInvariant();
        Guid token = Guid.NewGuid();
        await using System.Data.Common.DbCommand command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT nextval('barang.wallet_topup_seq')";
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        long seq = Convert.ToInt64(await command.ExecuteScalarAsync(t), System.Globalization.CultureInfo.InvariantCulture);
        string number = $"WTU-{Now.Year}-{seq:0000000000}";
        Result<CustomerWalletTopUp> made = CustomerWalletTopUp.Create(Guid.NewGuid(), Company, customerId, number, c.Amount, currency, token, Now);
        if (made.IsFailure) return Result.Failure<WalletTopUpView>(made.Error);
        db.Add(made.Value);
        return Result.Success(View(made.Value));
    }, ct);

    public Task<Result<IReadOnlyList<WalletTopUpView>>> ListTopUpsAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t)) return Result.Failure<IReadOnlyList<WalletTopUpView>>(WalletErrors.CustomerNotFound);
        var query = db.CustomerWalletTopUps.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (Enum.TryParse<CustomerWalletTopUpStatus>(status, true, out CustomerWalletTopUpStatus parsed)) query = query.Where(x => x.Status == parsed);
        List<CustomerWalletTopUp> rows = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(t);
        return Result.Success<IReadOnlyList<WalletTopUpView>>(rows.Select(View).ToList());
    }, ct);

    public Task<Result<WalletTopUpView>> CancelTopUpAsync(Guid customerId, Guid topUpId, long version, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        CustomerWalletTopUp? topUp = await db.CustomerWalletTopUps.SingleOrDefaultAsync(x => x.Id == topUpId && x.CustomerId == customerId, t);
        if (topUp is null) return Result.Failure<WalletTopUpView>(WalletErrors.NotFound);
        if (topUp.Version != version) return Result.Failure<WalletTopUpView>(WalletErrors.Concurrency);
        Result cancelled = topUp.Cancel(Now);
        if (cancelled.IsFailure) return Result.Failure<WalletTopUpView>(cancelled.Error);
        return Result.Success(View(topUp));
    }, ct);

    public Task<Result<WalletTopUpView>> GetByTokenAsync(Guid token, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        CustomerWalletTopUp? topUp = await db.CustomerWalletTopUps.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentToken == token, t);
        return topUp is null ? Result.Failure<WalletTopUpView>(WalletErrors.NotFound) : Result.Success(View(topUp));
    }, ct);

    public Task<Result<WalletTopUpView>> CompleteByTokenAsync(Guid token, bool success, string? gatewayReference, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        CustomerWalletTopUp? topUp = await db.CustomerWalletTopUps.SingleOrDefaultAsync(x => x.PaymentToken == token, t);
        if (topUp is null) return Result.Failure<WalletTopUpView>(WalletErrors.NotFound);
        if (topUp.Status == CustomerWalletTopUpStatus.Paid) return Result.Success(View(topUp)); // idempotent callback
        if (success)
        {
            await LockCustomerAsync(db, topUp.CustomerId, t);
            Result paid = topUp.MarkPaid(gatewayReference, Now);
            if (paid.IsFailure) return Result.Failure<WalletTopUpView>(paid.Error);
            Result<CustomerWalletTransaction> credit = CustomerWalletTransaction.Create(Guid.NewGuid(), topUp.CompanyId, topUp.CustomerId, CustomerWalletDirection.Credit, topUp.Amount, topUp.CurrencyCode, WalletPolicy.ReasonTopUp, $"topup:{topUp.Number}", null, Now);
            if (credit.IsFailure) return Result.Failure<WalletTopUpView>(credit.Error);
            db.Add(credit.Value);
        }
        else
        {
            Result failed = topUp.MarkFailed(gatewayReference, Now);
            if (failed.IsFailure) return Result.Failure<WalletTopUpView>(failed.Error);
        }
        return Result.Success(View(topUp));
    }, ct);

    public Task<Result<WalletPayResultView>> PayOrderFromWalletAsync(Guid orderId, long version, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        Order? order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId, t);
        if (order is null) return Result.Failure<WalletPayResultView>(WalletErrors.NotFound);
        if (order.Version != version) return Result.Failure<WalletPayResultView>(WalletErrors.Concurrency);
        if (order.Status != OrderStatus.Submitted) return Result.Failure<WalletPayResultView>(WalletErrors.OrderNotEligible);
        OrderPriceSnapshot? price = await db.OrderPriceSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId && x.IsCurrent && x.IsFinalized, t);
        if (price is null) return Result.Failure<WalletPayResultView>(WalletErrors.PricingRequired);
        if (!string.Equals(price.CurrencyCode, WalletPolicy.CurrencyCode, StringComparison.OrdinalIgnoreCase)) return Result.Failure<WalletPayResultView>(WalletErrors.CurrencyMismatch);

        await LockCustomerAsync(db, order.CustomerId, t);

        decimal paid = await db.Payments.AsNoTracking().Where(x => x.OrderId == orderId && x.RecordType == PaymentRecordType.Payment).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        decimal reversed = await db.Payments.AsNoTracking().Where(x => x.OrderId == orderId && x.RecordType == PaymentRecordType.Reversal).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        decimal remaining = price.FinalTotal - (paid - reversed);
        if (remaining <= 0) return Result.Failure<WalletPayResultView>(WalletErrors.AlreadyPaid);

        decimal balance = await BalanceAsync(db, order.CustomerId, t);
        if (balance < remaining) return Result.Failure<WalletPayResultView>(WalletErrors.InsufficientBalance);

        Result<CustomerWalletTransaction> debit = CustomerWalletTransaction.Create(Guid.NewGuid(), Company, order.CustomerId, CustomerWalletDirection.Debit, remaining, WalletPolicy.CurrencyCode, WalletPolicy.ReasonPurchase, $"order:{order.OrderNumber}", orderId, Now);
        if (debit.IsFailure) return Result.Failure<WalletPayResultView>(debit.Error);
        db.Add(debit.Value);

        Result<Payment> payment = Payment.Register(Company, orderId, WalletPolicy.PaymentMethodWallet, remaining, price.CurrencyCode, $"wallet:{Guid.NewGuid():N}", Now, Actor, Now);
        if (payment.IsFailure) return Result.Failure<WalletPayResultView>(payment.Error);
        db.Add(payment.Value);

        OrderFinancialState? state = await db.OrderFinancialStates.SingleOrDefaultAsync(x => x.OrderId == orderId, t);
        if (state is null) { state = OrderFinancialState.Create(Company, orderId, Now); db.Add(state); }
        Result approved = state.Transition(FinancialStatus.Approved, $"wallet payment {remaining:N0} {price.CurrencyCode}", Actor, state.Version, Now, price.Id);
        if (approved.IsFailure) return Result.Failure<WalletPayResultView>(approved.IsFailure ? approved.Error : WalletErrors.OrderNotEligible);

        decimal balanceAfter = balance - remaining;
        await BARANG.Core.Infrastructure.Customers.ReferralAdministration.RewardFirstPaymentInsideTransactionAsync(db, order.CustomerId, Now);
        return Result.Success(new WalletPayResultView(orderId, order.OrderNumber, remaining, price.CurrencyCode, balanceAfter, payment.Value.Id));
    }, ct);

    public Task<Result<WalletAdminTransactionView>> AdminAdjustAsync(Guid customerId, WalletAdjustCommand c, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t)) return Result.Failure<WalletAdminTransactionView>(WalletErrors.CustomerNotFound);
        if (c.Amount <= 0) return Result.Failure<WalletAdminTransactionView>(WalletErrors.InvalidAmount);
        bool credit = string.Equals(c.Direction, "credit", StringComparison.OrdinalIgnoreCase);
        if (!credit && !string.Equals(c.Direction, "debit", StringComparison.OrdinalIgnoreCase)) return Result.Failure<WalletAdminTransactionView>(WalletErrors.InvalidAmount);
        await LockCustomerAsync(db, customerId, t);
        Result<CustomerWalletTransaction> made = CustomerWalletTransaction.Create(Guid.NewGuid(), Company, customerId, credit ? CustomerWalletDirection.Credit : CustomerWalletDirection.Debit, c.Amount, WalletPolicy.CurrencyCode, WalletPolicy.ReasonManual, string.IsNullOrWhiteSpace(c.ReferenceNote) ? "manual adjustment" : c.ReferenceNote.Trim(), null, Now);
        if (made.IsFailure) return Result.Failure<WalletAdminTransactionView>(made.Error);
        db.Add(made.Value);
        return Result.Success(new WalletAdminTransactionView(made.Value.Id, customerId, null, null, credit ? "credit" : "debit", made.Value.Amount, made.Value.CurrencyCode, made.Value.ReasonCode, made.Value.ReferenceNote, null, made.Value.CreatedAtUtc));
    }, ct);

    public Task<Result<IReadOnlyList<WalletAdminTransactionView>>> AdminListTransactionsAsync(Guid? customerId, string? reason, int page, int pageSize, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        var rows = await (from w in db.CustomerWalletTransactions.AsNoTracking()
                          join c in db.Customers.AsNoTracking() on w.CustomerId equals c.Id
                          where (customerId == null || w.CustomerId == customerId)
                              && (string.IsNullOrWhiteSpace(reason) || w.ReasonCode == reason!.Trim())
                          orderby w.CreatedAtUtc descending
                          select new { w.Id, w.CustomerId, c.CustomerCode, c.DisplayName, w.Direction, w.Amount, w.CurrencyCode, w.ReasonCode, w.ReferenceNote, w.OrderId, w.CreatedAtUtc })
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(t);
        IReadOnlyList<WalletAdminTransactionView> items = rows.Select(x => new WalletAdminTransactionView(x.Id, x.CustomerId, x.CustomerCode, x.DisplayName, x.Direction == CustomerWalletDirection.Credit ? "credit" : "debit", x.Amount, x.CurrencyCode, x.ReasonCode, x.ReferenceNote, x.OrderId, x.CreatedAtUtc)).ToList();
        return Result.Success(items);
    }, ct);

    public Task<Result<IReadOnlyList<WalletTopUpView>>> AdminListTopUpsAsync(string? status, Guid? customerId, int page, int pageSize, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        var query = db.CustomerWalletTopUps.AsNoTracking().AsQueryable();
        if (Enum.TryParse<CustomerWalletTopUpStatus>(status, true, out CustomerWalletTopUpStatus parsed)) query = query.Where(x => x.Status == parsed);
        if (customerId is Guid cid) query = query.Where(x => x.CustomerId == cid);
        List<CustomerWalletTopUp> rows = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(t);
        return Result.Success<IReadOnlyList<WalletTopUpView>>(rows.Select(View).ToList());
    }, ct);

    private static WalletTopUpView View(CustomerWalletTopUp x) => new(x.Id, x.Number, x.CustomerId, x.Amount, x.CurrencyCode, x.Status.ToString(), x.GatewayCode, x.PaymentToken, x.GatewayReference, x.CreatedAtUtc, x.PaidAtUtc, x.Version);

    internal static async Task LockCustomerAsync(CoreDbContext db, Guid customerId, CancellationToken ct) =>
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtextextended({0}, 0))", new object[] { customerId.ToString() }, ct);

    internal static async Task<decimal> BalanceAsync(CoreDbContext db, Guid customerId, CancellationToken ct)
    {
        decimal credit = await db.CustomerWalletTransactions.AsNoTracking().Where(x => x.CustomerId == customerId && x.Direction == CustomerWalletDirection.Credit).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        decimal debit = await db.CustomerWalletTransactions.AsNoTracking().Where(x => x.CustomerId == customerId && x.Direction == CustomerWalletDirection.Debit).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        return credit - debit;
    }

    // Called from OrderAdministration.CancelAsync inside its transaction: credits
    // back wallet payments of a cancelled order (once per order).
    internal static async Task<Result<decimal>> RefundOrderWalletPaymentsInsideTransactionAsync(CoreDbContext db, Guid orderId, Guid customerId, DateTimeOffset now)
    {
        bool alreadyRefunded = await db.CustomerWalletTransactions.AsNoTracking().AnyAsync(x => x.OrderId == orderId && x.ReasonCode == WalletPolicy.ReasonRefund);
        if (alreadyRefunded) return Result.Success(0m);
        List<Payment> walletPayments = await db.Payments.AsNoTracking()
            .Where(x => x.OrderId == orderId && x.PaymentMethodCode == WalletPolicy.PaymentMethodWallet && x.RecordType == PaymentRecordType.Payment)
            .ToListAsync();
        if (walletPayments.Count == 0) return Result.Success(0m);
        decimal paid = walletPayments.Sum(x => x.Amount);
        decimal reversed = await db.Payments.AsNoTracking().Where(x => x.OriginalPaymentId != null && walletPayments.Select(p => p.Id).Contains(x.OriginalPaymentId.Value) && x.RecordType == PaymentRecordType.Reversal).SumAsync(x => (decimal?)x.Amount) ?? 0m;
        decimal refundable = paid - reversed;
        if (refundable <= 0) return Result.Success(0m);
        Result<CustomerWalletTransaction> credit = CustomerWalletTransaction.Create(Guid.NewGuid(), walletPayments[0].CompanyId, customerId, CustomerWalletDirection.Credit, refundable, WalletPolicy.CurrencyCode, WalletPolicy.ReasonRefund, $"refund:order {orderId.ToString()[..8]}", orderId, now);
        if (credit.IsFailure) return Result.Failure<decimal>(credit.Error);
        db.Add(credit.Value);
        return Result.Success(refundable);
    }
}
