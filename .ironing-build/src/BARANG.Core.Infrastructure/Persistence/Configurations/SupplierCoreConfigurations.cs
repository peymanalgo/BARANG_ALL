using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal static class SupplierCoreConfiguration
{
    internal static void Supplier(EntityTypeBuilder<Supplier> b)
    {
        b.Ignore(x=>x.Status);
        b.Property(x=>x.SupplierNumber).HasColumnName("supplier_number").HasDefaultValue(0L).ValueGeneratedOnAdd().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        b.Property(x=>x.EntityType).HasColumnName("entity_type").HasMaxLength(20).HasDefaultValue("Individual");
        b.Property(x=>x.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false);
        b.Property(x=>x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        b.Property(x=>x.LastName).HasColumnName("last_name").HasMaxLength(100);
        b.Property(x=>x.NationalId).HasColumnName("national_id").HasMaxLength(64);
        b.Property(x=>x.Occupation).HasColumnName("occupation").HasMaxLength(200);
        b.Property(x=>x.TradeName).HasColumnName("trade_name").HasMaxLength(200);
        b.Property(x=>x.LegalEntityTypeId).HasColumnName("legal_entity_type_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.LegalEntityTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.EstablishmentDate).HasColumnName("establishment_date");
        b.Property(x=>x.BusinessActivity).HasColumnName("business_activity").HasMaxLength(500);
        b.Property(x=>x.GradeId).HasColumnName("grade_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.GradeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.IsPreferred).HasColumnName("is_preferred");
        b.Property(x=>x.CooperationStartDate).HasColumnName("cooperation_start_date");
        b.Property(x=>x.HoldReasonId).HasColumnName("hold_reason_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.HoldReasonId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x=>new{x.CompanyId,x.SupplierNumber}).IsUnique().HasFilter("supplier_number > 0").HasDatabaseName("uq_suppliers_company_number");
        b.HasIndex(x=>new{x.CompanyId,x.NationalId}).HasDatabaseName("ix_suppliers_company_national_id");
        b.ToTable("suppliers",t=>{
            t.HasCheckConstraint("ck_suppliers_core_status","NOT (is_blocked AND is_active)");
            t.HasCheckConstraint("ck_suppliers_core_entity_type","entity_type IN ('Individual','LegalEntity')");
            t.HasCheckConstraint("ck_suppliers_hold_reason","NOT is_blocked OR hold_reason_id IS NOT NULL");
        });
    }
    internal static void Owner<T>(EntityTypeBuilder<T> b,string table) where T:SupplierOwnedEntity
    {
        CatalogConfiguration.Base(b,table);
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");
        string selected=typeof(T)==typeof(SupplierContact)?"is_primary":"is_default";
        b.Property(x=>x.IsDefault).HasColumnName(selected);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId}).IsUnique().HasFilter(selected).HasDatabaseName("uq_"+table+"_default");
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.CreatedAtUtc}).HasDatabaseName("ix_"+table+"_directory");
        b.ToTable(table,t=>t.HasCheckConstraint("ck_"+table+"_active_default","NOT "+selected+" OR is_active"));
    }
}

internal sealed class SupplierAddressConfiguration:IEntityTypeConfiguration<SupplierAddress>
{
    public void Configure(EntityTypeBuilder<SupplierAddress> b)
    {
        SupplierCoreConfiguration.Owner(b,"supplier_addresses");
        b.Property(x=>x.AddressName).HasColumnName("address_name").HasMaxLength(100);
        b.Property(x=>x.AddressTypeId).HasColumnName("address_type_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.AddressTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        b.Property(x=>x.ProvinceCode).HasColumnName("province_code").HasMaxLength(80);
        b.Property(x=>x.CityCode).HasColumnName("city_code").HasMaxLength(80);
        b.Property(x=>x.PostalCode).HasColumnName("postal_code").HasMaxLength(32);
        b.Property(x=>x.FullAddress).HasColumnName("full_address").HasMaxLength(1000);
        b.Property(x=>x.Landline).HasColumnName("landline").HasMaxLength(16);
        b.Property(x=>x.Mobile).HasColumnName("mobile").HasMaxLength(16);
        b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(320);
        b.Property(x=>x.RecipientName).HasColumnName("recipient_name").HasMaxLength(200);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);


    }
}

internal sealed class SupplierContactConfiguration:IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> b)
    {
        SupplierCoreConfiguration.Owner(b,"supplier_contacts");
        b.Ignore(x=>x.IsPrimary);
        b.Property(x=>x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        b.Property(x=>x.LastName).HasColumnName("last_name").HasMaxLength(100);
        b.Property(x=>x.ContactRoleId).HasColumnName("contact_role_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.ContactRoleId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.PositionTitle).HasColumnName("position_title").HasMaxLength(150);
        b.Property(x=>x.Landline).HasColumnName("landline").HasMaxLength(16);
        b.Property(x=>x.Mobile).HasColumnName("mobile").HasMaxLength(16);
        b.Property(x=>x.Extension).HasColumnName("extension").HasMaxLength(12);
        b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(320);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);


    }
}

internal sealed class SupplierBankAccountConfiguration:IEntityTypeConfiguration<SupplierBankAccount>
{
    public void Configure(EntityTypeBuilder<SupplierBankAccount> b)
    {
        SupplierCoreConfiguration.Owner(b,"supplier_bank_accounts");
        b.Property(x=>x.BankId).HasColumnName("bank_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.BankId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.AccountName).HasColumnName("account_name").HasMaxLength(150);
        b.Property(x=>x.AccountTypeId).HasColumnName("account_type_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.AccountTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.OwnershipType).HasColumnName("ownership_type").HasMaxLength(20);
        b.Property(x=>x.AccountHolderNames).HasColumnName("account_holder_names").HasMaxLength(400);
        b.Property(x=>x.AccountNumber).HasColumnName("account_number").HasMaxLength(64);
        b.Property(x=>x.Iban).HasColumnName("iban").HasMaxLength(34);
        b.Property(x=>x.CardNumber).HasColumnName("card_number").HasMaxLength(19);
        b.Property(x=>x.BranchCode).HasColumnName("branch_code").HasMaxLength(40);
        b.Property(x=>x.BranchName).HasColumnName("branch_name").HasMaxLength(150);
        b.Property(x=>x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);

        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.AccountNumber}).IsUnique().HasFilter("account_number IS NOT NULL").HasDatabaseName("uq_supplier_bank_account_number");
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.Iban}).IsUnique().HasFilter("iban IS NOT NULL").HasDatabaseName("uq_supplier_bank_iban");
        b.ToTable("supplier_bank_accounts",t=>t.HasCheckConstraint("ck_supplier_bank_ownership","ownership_type IN ('Individual','Joint')"));
    }
}

internal sealed class SupplierFinancialProfileConfiguration:IEntityTypeConfiguration<SupplierFinancialProfile>
{
    public void Configure(EntityTypeBuilder<SupplierFinancialProfile> b)
    {
        CatalogConfiguration.Base(b,"supplier_financial_profiles");
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId}).IsUnique();
        b.Property(x=>x.PaymentTermId).HasColumnName("payment_term_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.PaymentConditionId).HasColumnName("payment_condition_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PaymentConditionId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.PaymentMethodId).HasColumnName("payment_method_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3);
        b.Property(x=>x.MinimumOrderAmount).HasColumnName("minimum_order_amount").HasPrecision(18,2);
        b.Property(x=>x.DefaultLeadTimeDays).HasColumnName("default_lead_time_days");
        b.Property(x=>x.BaseDiscountPercent).HasColumnName("base_discount_percent").HasPrecision(5,2);
        b.Property(x=>x.CreditPurchaseAllowed).HasColumnName("credit_purchase_allowed");
        b.Property(x=>x.PurchaseCreditLimit).HasColumnName("purchase_credit_limit").HasPrecision(18,2);
        b.Property(x=>x.ShippingMethodId).HasColumnName("shipping_method_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.ShippingMethodId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.FreightDeliveryTermId).HasColumnName("freight_delivery_term_id");
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.FreightDeliveryTermId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable("supplier_financial_profiles",t=>{
            t.HasCheckConstraint("ck_supplier_financial_amounts","(minimum_order_amount IS NULL OR minimum_order_amount>=0) AND purchase_credit_limit>=0 AND (default_lead_time_days IS NULL OR default_lead_time_days>=0)");
            t.HasCheckConstraint("ck_supplier_financial_discount","base_discount_percent BETWEEN 0 AND 100");
        });
    }
}
internal sealed class SupplierCategoryAssignmentConfiguration:IEntityTypeConfiguration<SupplierCategoryAssignment>
{
    public void Configure(EntityTypeBuilder<SupplierCategoryAssignment> b)
    {
        CatalogConfiguration.Base(b,"supplier_category_assignments");
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");b.Property(x=>x.CategoryId).HasColumnName("category_id");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.CategoryId}).IsUnique();
    }
}
