using System.Security.Cryptography;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Customers;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Finance;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BARANG.Core.Infrastructure.Customers;

// Referral (دعوت از دوستان) module: every customer gets a stable invite code on
// first use; when a referred customer pays their FIRST order the referrer earns
// a gift credit in their wallet and the referral is marked Rewarded.
public sealed class ReferralAdministration(CoreDbContext db, ITransactionExecutor tx, ICompanyContext company, IConfiguration configuration) : IReferralAdministration
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous 0/O/1/I

    private Guid Company => company.RequireCompanyId();
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    public decimal RewardAmount => configuration.GetValue<decimal?>("Wallet:ReferralRewardAmount") ?? 200_000m;

    public Task<Result<ReferralInfoView>> GetInfoAsync(Guid customerId, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t)) return Result.Failure<ReferralInfoView>(WalletErrors.CustomerNotFound);
        string code = await EnsureCodeAsync(customerId, t);
        List<ReferralEntryView> referrals = await ListEntriesAsync(customerId, t);
        decimal earned = referrals.Where(x => x.Status == "Rewarded").Sum(x => x.RewardAmount);
        return Result.Success(new ReferralInfoView(code, referrals.Count, earned, WalletPolicy.CurrencyCode, referrals));
    }, ct);

    public Task<Result<ReferralInfoView>> AttachAsync(Guid customerId, string code, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t)) return Result.Failure<ReferralInfoView>(WalletErrors.CustomerNotFound);
        string normalized = (code ?? "").Trim().ToUpperInvariant();
        CustomerReferralCode? referralCode = await db.CustomerReferralCodes.AsNoTracking().SingleOrDefaultAsync(x => x.Code == normalized, t);
        if (referralCode is null) return Result.Failure<ReferralInfoView>(ReferralErrors.CodeNotFound);
        if (referralCode.CustomerId == customerId) return Result.Failure<ReferralInfoView>(ReferralErrors.SelfReferral);
        if (await db.CustomerReferrals.AsNoTracking().AnyAsync(x => x.ReferredCustomerId == customerId, t)) return Result.Failure<ReferralInfoView>(ReferralErrors.AlreadyReferred);
        // eligibility: only brand-new customers (no submitted orders yet)
        bool hasOrders = await db.Orders.AsNoTracking().AnyAsync(x => x.CustomerId == customerId && x.Status != OrderStatus.Draft, t);
        if (hasOrders) return Result.Failure<ReferralInfoView>(ReferralErrors.NotEligible);
        Result<CustomerReferral> made = CustomerReferral.Create(Guid.NewGuid(), Company, referralCode.CustomerId, customerId, normalized, RewardAmount, WalletPolicy.CurrencyCode, Now);
        if (made.IsFailure) return Result.Failure<ReferralInfoView>(made.Error);
        db.Add(made.Value);
        List<ReferralEntryView> referrals = await ListEntriesAsync(customerId, t);
        decimal earned = referrals.Where(x => x.Status == "Rewarded").Sum(x => x.RewardAmount);
        return Result.Success(new ReferralInfoView(normalized, referrals.Count, earned, WalletPolicy.CurrencyCode, referrals));
    }, ct);

    public Task<Result<IReadOnlyList<AdminReferralView>>> AdminListAsync(string? status, int page, int pageSize, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        var rows = await (from r in db.CustomerReferrals.AsNoTracking()
                          join referrer in db.Customers.AsNoTracking() on r.ReferrerCustomerId equals referrer.Id
                          join referred in db.Customers.AsNoTracking() on r.ReferredCustomerId equals referred.Id
                          join rc in db.CustomerReferralCodes.AsNoTracking() on r.ReferrerCustomerId equals rc.CustomerId into codes
                          from rc in codes.DefaultIfEmpty()
                          where string.IsNullOrWhiteSpace(status) || r.Status.ToString() == status!.Trim()
                          orderby r.CreatedAtUtc descending
                          select new { r.Id, ReferrerCode = rc != null ? rc.Code : "", ReferrerName = referrer.DisplayName, ReferredName = referred.DisplayName, ReferredCustomerCode = referred.CustomerCode, Status2 = r.Status.ToString(), r.RewardAmount, r.CurrencyCode, r.CreatedAtUtc, r.RewardedAtUtc })
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync(t);
        IReadOnlyList<AdminReferralView> items = rows.Select(x => new AdminReferralView(x.Id, x.ReferrerCode, x.ReferrerName, x.ReferredName, x.ReferredCustomerCode, x.Status2, x.RewardAmount, x.CurrencyCode, x.CreatedAtUtc, x.RewardedAtUtc)).ToList();
        return Result.Success(items);
    }, ct);

    private async Task<List<ReferralEntryView>> ListEntriesAsync(Guid customerId, CancellationToken ct)
    {
        return await (from r in db.CustomerReferrals.AsNoTracking()
                      join c in db.Customers.AsNoTracking() on r.ReferredCustomerId equals c.Id
                      where r.ReferrerCustomerId == customerId
                      orderby r.CreatedAtUtc descending
                      select new ReferralEntryView(r.Id, c.DisplayName, r.Status.ToString(), r.RewardAmount, r.CurrencyCode, r.CreatedAtUtc, r.RewardedAtUtc))
            .ToListAsync(ct);
    }

    private async Task<string> EnsureCodeAsync(Guid customerId, CancellationToken ct)
    {
        CustomerReferralCode? existing = await db.CustomerReferralCodes.AsNoTracking().SingleOrDefaultAsync(x => x.CustomerId == customerId, ct);
        if (existing is not null) return existing.Code;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            CustomerReferralCode candidate = CustomerReferralCode.Create(Guid.NewGuid(), Company, customerId, NewCode(), Now);
            db.Add(candidate);
            try
            {
                await db.SaveChangesAsync(ct);
                return candidate.Code;
            }
            catch (DbUpdateException)
            {
                db.Entry(candidate).State = EntityState.Detached;
            }
        }
        throw new InvalidOperationException("Could not generate a unique referral code.");
    }

    private static string NewCode()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(8);
        return new string(bytes.Select(b => CodeAlphabet[b % CodeAlphabet.Length]).ToArray());
    }

    // Called from WalletAdministration.PayOrderFromWalletAsync inside its transaction:
    // if this was the referred customer's first paid order, credit the referrer's wallet.
    internal static async Task RewardFirstPaymentInsideTransactionAsync(CoreDbContext db, Guid customerId, DateTimeOffset now)
    {
        CustomerReferral? referral = await db.CustomerReferrals.SingleOrDefaultAsync(x => x.ReferredCustomerId == customerId && x.Status == CustomerReferralStatus.Pending);
        if (referral is null) return;
        // the purchase debit for this payment was just added in the same transaction:
        // exactly one means this was the first paid order
        int purchaseCount = await db.CustomerWalletTransactions
            .Where(x => x.CustomerId == customerId && x.ReasonCode == WalletPolicy.ReasonPurchase && x.Direction == CustomerWalletDirection.Debit)
            .CountAsync();
        if (purchaseCount != 1) return;
        Result<CustomerWalletTransaction> gift = CustomerWalletTransaction.Create(Guid.NewGuid(), referral.CompanyId, referral.ReferrerCustomerId, CustomerWalletDirection.Credit, referral.RewardAmount, referral.CurrencyCode, WalletPolicy.ReasonGift, $"referral:{referral.CodeUsed}", null, now);
        if (gift.IsFailure) return;
        db.Add(gift.Value);
        await db.SaveChangesAsync();
        referral.Reward(gift.Value.Id, now);
    }
}
