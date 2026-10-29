using BARANG.Core.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class CustomerWalletTransactionConfiguration : IEntityTypeConfiguration<CustomerWalletTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerWalletTransaction> b)
    {
        b.ToTable("customer_wallet_transactions");
        b.HasKey(x => x.Id).HasName("pk_customer_wallet_transactions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Direction).HasColumnName("direction");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 0);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        b.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(80).IsRequired();
        b.Property(x => x.ReferenceNote).HasColumnName("reference_note").HasMaxLength(500);
        b.Property(x => x.OrderId).HasColumnName("order_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_customer_wallet_transactions_customer_created");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_wallet_transactions_company_id");
    }
}

internal sealed class CustomerFeedbackConfiguration : IEntityTypeConfiguration<CustomerFeedback>
{
    public void Configure(EntityTypeBuilder<CustomerFeedback> b)
    {
        b.ToTable("customer_feedbacks");
        b.HasKey(x => x.Id).HasName("pk_customer_feedbacks");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.FeedbackTypeCode).HasColumnName("feedback_type_code").HasMaxLength(80).IsRequired();
        b.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(120).IsRequired();
        b.Property(x => x.Body).HasColumnName("body").HasMaxLength(2000).IsRequired();
        b.Property(x => x.StatusCode).HasColumnName("status_code").HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_customer_feedbacks_customer_created");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_feedbacks_company_id");
    }
}

internal sealed class CustomerCustomDesignConfiguration : IEntityTypeConfiguration<CustomerCustomDesign>
{
    public void Configure(EntityTypeBuilder<CustomerCustomDesign> b)
    {
        b.ToTable("customer_custom_designs");
        b.HasKey(x => x.Id).HasName("pk_customer_custom_designs");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
        b.Property(x => x.FileVersionId).HasColumnName("file_version_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_customer_custom_designs_customer_created");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_custom_designs_company_id");
    }
}
