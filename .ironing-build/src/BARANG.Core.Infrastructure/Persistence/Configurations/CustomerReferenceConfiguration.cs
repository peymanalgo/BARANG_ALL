using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Lookups;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal static class CustomerReferenceConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        model.Entity<Customer>().Property(x=>x.CustomerGroupId).HasColumnName("customer_group_id");
        model.Entity<Customer>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.CustomerGroupId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Customer>().HasIndex(x=>x.CustomerGroupId).HasDatabaseName("ix_customers_customer_group_id");
        model.Entity<Customer>().Property(x=>x.PriceGroupId).HasColumnName("price_group_id");
        model.Entity<Customer>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PriceGroupId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Customer>().HasIndex(x=>x.PriceGroupId).HasDatabaseName("ix_customers_price_group_id");
        model.Entity<LegalCustomerProfile>().Property(x=>x.LegalEntityTypeId).HasColumnName("legal_entity_type_id");
        model.Entity<LegalCustomerProfile>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.LegalEntityTypeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<LegalCustomerProfile>().HasIndex(x=>x.LegalEntityTypeId).HasDatabaseName("ix_legal_customer_profiles_legal_entity_type_id");
        model.Entity<CustomerFinancialProfile>().Property(x=>x.PaymentTermId).HasColumnName("payment_term_id");
        model.Entity<CustomerFinancialProfile>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerFinancialProfile>().HasIndex(x=>x.PaymentTermId).HasDatabaseName("ix_customer_financial_profiles_payment_term_id");
        model.Entity<CustomerFinancialProfile>().Property(x=>x.PaymentConditionId).HasColumnName("payment_condition_id");
        model.Entity<CustomerFinancialProfile>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PaymentConditionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerFinancialProfile>().HasIndex(x=>x.PaymentConditionId).HasDatabaseName("ix_customer_financial_profiles_payment_condition_id");
        model.Entity<CustomerFinancialProfile>().Property(x=>x.PreferredPaymentMethodId).HasColumnName("preferred_payment_method_id");
        model.Entity<CustomerFinancialProfile>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PreferredPaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerFinancialProfile>().HasIndex(x=>x.PreferredPaymentMethodId).HasDatabaseName("ix_customer_financial_profiles_preferred_payment_method_id");
        model.Entity<CustomerFinancialProfile>().Property(x=>x.TaxStatusId).HasColumnName("tax_status_id");
        model.Entity<CustomerFinancialProfile>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.TaxStatusId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerFinancialProfile>().HasIndex(x=>x.TaxStatusId).HasDatabaseName("ix_customer_financial_profiles_tax_status_id");
        model.Entity<CustomerAddress>().Property(x=>x.AddressTypeId).HasColumnName("address_type_id");
        model.Entity<CustomerAddress>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.AddressTypeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerAddress>().HasIndex(x=>x.AddressTypeId).HasDatabaseName("ix_customer_addresses_address_type_id");
        model.Entity<CustomerContact>().Property(x=>x.ContactRoleId).HasColumnName("contact_role_id");
        model.Entity<CustomerContact>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.ContactRoleId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerContact>().HasIndex(x=>x.ContactRoleId).HasDatabaseName("ix_customer_contacts_contact_role_id");
        model.Entity<CustomerBankAccount>().Property(x=>x.BankId).HasColumnName("bank_id");
        model.Entity<CustomerBankAccount>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.BankId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerBankAccount>().HasIndex(x=>x.BankId).HasDatabaseName("ix_customer_bank_accounts_bank_id");
        model.Entity<CustomerBankAccount>().Property(x=>x.AccountTypeId).HasColumnName("account_type_id");
        model.Entity<CustomerBankAccount>().HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.AccountTypeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CustomerBankAccount>().HasIndex(x=>x.AccountTypeId).HasDatabaseName("ix_customer_bank_accounts_account_type_id");
        model.Entity<CustomerAddress>().Property(x=>x.Email).HasColumnName("email").HasMaxLength(254);
        model.Entity<CustomerAddress>().Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(1000);
        model.Entity<CustomerBankAccount>().Property(x=>x.AccountTypeCode).HasColumnName("account_type_code").HasMaxLength(100);
        model.Entity<CustomerBankAccount>().Property(x=>x.AccountName).HasColumnName("account_name").HasMaxLength(200);
        model.Entity<CustomerBankAccount>().Property(x=>x.OwnershipType).HasColumnName("ownership_type").HasConversion<string>().HasMaxLength(20).HasDefaultValue(BankAccountOwnership.Individual);
        model.Entity<CustomerBankAccount>().Property(x=>x.CardNumber).HasColumnName("card_number").HasMaxLength(19);
        model.Entity<CustomerBankAccount>().Property(x=>x.BranchCode).HasColumnName("branch_code").HasMaxLength(50);
    }
}
