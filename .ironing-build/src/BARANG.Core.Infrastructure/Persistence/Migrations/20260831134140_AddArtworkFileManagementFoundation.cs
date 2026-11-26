using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration arrays are one-time arguments.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkFileManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "artwork_tags",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_tags", x => x.id);
                    table.UniqueConstraint("ak_artwork_tags_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_artwork_tags_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artworks",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    designer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lifecycle_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artworks", x => x.id);
                    table.UniqueConstraint("ak_artworks_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_artworks_application_users_designer_user_id",
                        column: x => x.designer_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artworks_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stored_files",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_files", x => x.id);
                    table.UniqueConstraint("ak_stored_files_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_stored_files_application_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stored_files_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artwork_tag_assignments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artwork_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_tag_assignments", x => x.id);
                    table.UniqueConstraint("ak_artwork_tag_assignments_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_artwork_tag_assignments_artwork_tags_company_id_tag_id",
                        columns: x => new { x.company_id, x.tag_id },
                        principalSchema: "barang",
                        principalTable: "artwork_tags",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artwork_tag_assignments_artworks_company_id_artwork_id",
                        columns: x => new { x.company_id, x.artwork_id },
                        principalSchema: "barang",
                        principalTable: "artworks",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artwork_tag_assignments_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_versions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    storage_bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    media_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_versions", x => x.id);
                    table.UniqueConstraint("ak_file_versions_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_file_versions_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_versions_stored_files_company_id_file_id",
                        columns: x => new { x.company_id, x.file_id },
                        principalSchema: "barang",
                        principalTable: "stored_files",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artwork_versions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artwork_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_versions", x => x.id);
                    table.UniqueConstraint("ak_artwork_versions_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_artwork_versions_application_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artwork_versions_artworks_company_id_artwork_id",
                        columns: x => new { x.company_id, x.artwork_id },
                        principalSchema: "barang",
                        principalTable: "artworks",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artwork_versions_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artwork_versions_file_versions_company_id_file_version_id",
                        columns: x => new { x.company_id, x.file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_derivatives",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_file_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    derivative_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    media_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_derivatives", x => x.id);
                    table.UniqueConstraint("ak_file_derivatives_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_file_derivatives_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_derivatives_file_versions_company_id_source_file_versi~",
                        columns: x => new { x.company_id, x.source_file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_links",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_links", x => x.id);
                    table.UniqueConstraint("ak_file_links_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_file_links_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_links_file_versions_company_id_file_version_id",
                        columns: x => new { x.company_id, x.file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_links_stored_files_company_id_file_id",
                        columns: x => new { x.company_id, x.file_id },
                        principalSchema: "barang",
                        principalTable: "stored_files",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_processing_jobs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_processing_jobs", x => x.id);
                    table.UniqueConstraint("ak_file_processing_jobs_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_file_processing_jobs_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_processing_jobs_file_versions_company_id_file_version_~",
                        columns: x => new { x.company_id, x.file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000019"), "artwork.read", "artwork", "artwork.read" },
                    { new Guid("00000000-0000-0000-0000-000000000020"), "artwork.manage", "artwork", "artwork.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), "file.read", "file", "file.read" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), "file.manage", "file", "file.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000023"), "file.upload", "file", "file.upload" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_artwork_tag_assignments_company_id_tag_id",
                schema: "barang",
                table: "artwork_tag_assignments",
                columns: new[] { "company_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "uq_artwork_tag_assignments",
                schema: "barang",
                table: "artwork_tag_assignments",
                columns: new[] { "company_id", "artwork_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_artwork_tags_company_code",
                schema: "barang",
                table: "artwork_tags",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artwork_versions_company_id_file_version_id",
                schema: "barang",
                table: "artwork_versions",
                columns: new[] { "company_id", "file_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_artwork_versions_created_by_user_id",
                schema: "barang",
                table: "artwork_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_artwork_versions_number",
                schema: "barang",
                table: "artwork_versions",
                columns: new[] { "company_id", "artwork_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artworks_designer_user_id",
                schema: "barang",
                table: "artworks",
                column: "designer_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_artworks_options",
                schema: "barang",
                table: "artworks",
                columns: new[] { "company_id", "is_active", "is_archived", "code" });

            migrationBuilder.CreateIndex(
                name: "uq_artworks_company_code",
                schema: "barang",
                table: "artworks",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_file_derivatives_type",
                schema: "barang",
                table: "file_derivatives",
                columns: new[] { "company_id", "source_file_version_id", "derivative_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_links_company_id_file_id",
                schema: "barang",
                table: "file_links",
                columns: new[] { "company_id", "file_id" });

            migrationBuilder.CreateIndex(
                name: "IX_file_links_company_id_file_version_id",
                schema: "barang",
                table: "file_links",
                columns: new[] { "company_id", "file_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_file_links_entity",
                schema: "barang",
                table: "file_links",
                columns: new[] { "company_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_file_links_target",
                schema: "barang",
                table: "file_links",
                columns: new[] { "company_id", "entity_type", "entity_id", "file_id", "purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_processing_jobs_company_id_file_version_id",
                schema: "barang",
                table: "file_processing_jobs",
                columns: new[] { "company_id", "file_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_file_processing_jobs_pending",
                schema: "barang",
                table: "file_processing_jobs",
                columns: new[] { "status", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_file_versions_company_hash",
                schema: "barang",
                table: "file_versions",
                columns: new[] { "company_id", "sha256_hash" });

            migrationBuilder.CreateIndex(
                name: "uq_file_versions_number",
                schema: "barang",
                table: "file_versions",
                columns: new[] { "company_id", "file_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_file_versions_storage_key",
                schema: "barang",
                table: "file_versions",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stored_files_uploaded_by_user_id",
                schema: "barang",
                table: "stored_files",
                column: "uploaded_by_user_id");

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.code IN ('artwork.read', 'artwork.manage', 'file.read', 'file.manage', 'file.upload')
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                ALTER TABLE barang.artworks ENABLE ROW LEVEL SECURITY;
                CREATE POLICY artworks_company_isolation ON barang.artworks USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.artwork_versions ENABLE ROW LEVEL SECURITY;
                CREATE POLICY artwork_versions_company_isolation ON barang.artwork_versions USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.artwork_tags ENABLE ROW LEVEL SECURITY;
                CREATE POLICY artwork_tags_company_isolation ON barang.artwork_tags USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.artwork_tag_assignments ENABLE ROW LEVEL SECURITY;
                CREATE POLICY artwork_tag_assignments_company_isolation ON barang.artwork_tag_assignments USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.stored_files ENABLE ROW LEVEL SECURITY;
                CREATE POLICY stored_files_company_isolation ON barang.stored_files USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.file_versions ENABLE ROW LEVEL SECURITY;
                CREATE POLICY file_versions_company_isolation ON barang.file_versions USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.file_links ENABLE ROW LEVEL SECURITY;
                CREATE POLICY file_links_company_isolation ON barang.file_links USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.file_processing_jobs ENABLE ROW LEVEL SECURITY;
                CREATE POLICY file_processing_jobs_company_isolation ON barang.file_processing_jobs USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.file_derivatives ENABLE ROW LEVEL SECURITY;
                CREATE POLICY file_derivatives_company_isolation ON barang.file_derivatives USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.artworks, barang.artwork_versions, barang.artwork_tags, barang.artwork_tag_assignments, barang.stored_files, barang.file_versions, barang.file_links, barang.file_processing_jobs, barang.file_derivatives TO barang_app;
                    REVOKE DELETE ON barang.artworks, barang.artwork_versions, barang.artwork_tags, barang.artwork_tag_assignments, barang.stored_files, barang.file_versions, barang.file_links, barang.file_processing_jobs, barang.file_derivatives FROM barang_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000019','00000000-0000-0000-0000-000000000020','00000000-0000-0000-0000-000000000021','00000000-0000-0000-0000-000000000022','00000000-0000-0000-0000-000000000023');");
            migrationBuilder.DropTable(
                name: "artwork_tag_assignments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "artwork_versions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "file_derivatives",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "file_links",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "file_processing_jobs",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "artwork_tags",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "artworks",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "file_versions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stored_files",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000023"));
        }
    }
}
