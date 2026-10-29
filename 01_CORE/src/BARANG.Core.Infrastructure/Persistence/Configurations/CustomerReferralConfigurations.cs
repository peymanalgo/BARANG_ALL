using BARANG.Core.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class CustomerReferralConfiguration : IEntityTypeConfiguration<CustomerReferral>
{
    public void Configure(EntityTypeBuilder<CustomerReferral> b)
    {
        b.ToTable("customer_referrals");
        b.HasKey(x => x.Id).HasName("pk_customer_referrals");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ReferrerCustomerId).HasColumnName("referrer_customer_id");
        b.Property(x => x.ReferredCustomerId).HasColumnName("referred_customer_id");
        b.Property(x => x.CodeUsed).HasColumnName("code_used").HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.RewardAmount).HasColumnName("reward_amount").HasPrecision(18, 0);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        b.Property(x => x.RewardTransactionId).HasColumnName("reward_transaction_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.RewardedAtUtc).HasColumnName("rewarded_at_utc");
        b.HasIndex(x => x.ReferredCustomerId).IsUnique().HasDatabaseName("uq_customer_referrals_referred");
        b.HasIndex(x => new { x.ReferrerCustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_customer_referrals_referrer_created");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_referrals_company_id");
    }
}

internal sealed class CustomerReferralCodeConfiguration : IEntityTypeConfiguration<CustomerReferralCode>
{
    public void Configure(EntityTypeBuilder<CustomerReferralCode> b)
    {
        b.ToTable("customer_referral_codes");
        b.HasKey(x => x.Id).HasName("pk_customer_referral_codes");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_customer_referral_codes_code");
        b.HasIndex(x => x.CustomerId).IsUnique().HasDatabaseName("uq_customer_referral_codes_customer");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_referral_codes_company_id");
    }
}
