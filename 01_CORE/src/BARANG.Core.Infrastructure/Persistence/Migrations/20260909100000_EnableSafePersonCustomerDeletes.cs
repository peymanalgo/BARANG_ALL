using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260909100000_EnableSafePersonCustomerDeletes")]
public sealed class EnableSafePersonCustomerDeletes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $grant$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT DELETE ON barang.persons, barang.customers,
                        barang.individual_customer_profiles, barang.legal_customer_profiles TO barang_app;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    GRANT DELETE ON barang.persons, barang.customers,
                        barang.individual_customer_profiles, barang.legal_customer_profiles TO barang_test_app;
                END IF;
            END $grant$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $revoke$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    REVOKE DELETE ON barang.persons, barang.customers,
                        barang.individual_customer_profiles, barang.legal_customer_profiles FROM barang_app;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    REVOKE DELETE ON barang.persons, barang.customers,
                        barang.individual_customer_profiles, barang.legal_customer_profiles FROM barang_test_app;
                END IF;
            END $revoke$;
            """);
    }
}
