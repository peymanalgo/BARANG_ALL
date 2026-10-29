using BARANG.Core.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class CustomerWalletTopUpConfiguration : IEntityTypeConfiguration<CustomerWalletTopUp>
{
    public void Configure(EntityTypeBuilder<CustomerWalletTopUp> b)
    {
        b.ToTable("customer_wallet_topups");
        b.HasKey(x => x.Id).HasName("pk_customer_wallet_topups");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Number).HasColumnName("number").HasMaxLength(40).IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 0);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.GatewayCode).HasColumnName("gateway_code").HasMaxLength(30).IsRequired();
        b.Property(x => x.PaymentToken).HasColumnName("payment_token");
        b.Property(x => x.GatewayReference).HasColumnName("gateway_reference").HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.PaidAtUtc).HasColumnName("paid_at_utc");
        b.Property(x => x.Version).HasColumnName("version");
        b.HasIndex(x => x.PaymentToken).IsUnique().HasDatabaseName("uq_customer_wallet_topups_payment_token");
        b.HasIndex(x => x.Number).IsUnique().HasDatabaseName("uq_customer_wallet_topups_number");
        b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_customer_wallet_topups_customer_created");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_wallet_topups_company_id");
    }
}
