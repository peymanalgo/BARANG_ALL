using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReproductionChainConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_reproduction_request_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "reproduction_request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_root_production_job_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "root_production_job_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_source_production_job_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "source_production_job_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_production_jobs_production_jobs_company_id_root_production_~",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "root_production_job_id" },
                principalSchema: "barang",
                principalTable: "production_jobs",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_production_jobs_production_jobs_company_id_source_productio~",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "source_production_job_id" },
                principalSchema: "barang",
                principalTable: "production_jobs",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_production_jobs_reproduction_requests_company_id_reproducti~",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "reproduction_request_id" },
                principalSchema: "barang",
                principalTable: "reproduction_requests",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_production_jobs_production_jobs_company_id_root_production_~",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_production_jobs_production_jobs_company_id_source_productio~",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_production_jobs_reproduction_requests_company_id_reproducti~",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropIndex(
                name: "IX_production_jobs_company_id_reproduction_request_id",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropIndex(
                name: "IX_production_jobs_company_id_root_production_job_id",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropIndex(
                name: "IX_production_jobs_company_id_source_production_job_id",
                schema: "barang",
                table: "production_jobs");
        }
    }
}
