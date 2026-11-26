using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBirthCertificateNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_certificate_number",
                schema: "barang",
                table: "individual_customer_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "birth_certificate_number",
                schema: "barang",
                table: "individual_customer_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
