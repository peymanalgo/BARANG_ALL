using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

public partial class WarehouseResponsibilities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("create table if not exists barang.warehouse_responsibilities (id uuid not null, company_id uuid not null, warehouse_id uuid not null, application_user_id uuid not null, is_active boolean not null, created_at_utc timestamptz not null, updated_at_utc timestamptz not null, version bigint not null, constraint pk_warehouse_responsibilities primary key (id), constraint uq_warehouse_responsibilities_warehouse_user unique (company_id, warehouse_id, application_user_id), constraint fk_warehouse_responsibilities_companies_company_id foreign key (company_id) references barang.companies(id), constraint fk_warehouse_responsibilities_warehouses foreign key (company_id, warehouse_id) references barang.warehouses(company_id, id), constraint fk_warehouse_responsibilities_users foreign key (application_user_id) references barang.application_users(id), constraint ck_warehouse_responsibilities_version_positive check (version > 0));");
        migrationBuilder.Sql("create index if not exists ix_warehouse_responsibilities_warehouse_active on barang.warehouse_responsibilities(company_id, warehouse_id, is_active);");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("drop table if exists barang.warehouse_responsibilities;");
}
