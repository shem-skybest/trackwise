using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddMasterDataFoundations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "branches",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                short_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                merged_into_id = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                barangay = table.Column<string>(type: "text", nullable: true),
                city_or_municipality = table.Column<string>(type: "text", nullable: true),
                country = table.Column<string>(type: "text", nullable: true),
                province = table.Column<string>(type: "text", nullable: true),
                region = table.Column<string>(type: "text", nullable: true),
                street_address = table.Column<string>(type: "text", nullable: true),
                subdivision = table.Column<string>(type: "text", nullable: true),
                zipcode = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_branches", x => x.id);
                table.ForeignKey(
                    name: "fk_branches_branches_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "master_data_change_logs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                change_type = table.Column<string>(type: "text", nullable: false),
                field = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                old_value = table.Column<string>(type: "text", nullable: true),
                new_value = table.Column<string>(type: "text", nullable: true),
                changed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                source_system = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_master_data_change_logs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "master_data_merge_histories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                winner_id = table.Column<Guid>(type: "uuid", nullable: false),
                loser_id = table.Column<Guid>(type: "uuid", nullable: false),
                loser_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                loser_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                field_choices = table.Column<string>(type: "jsonb", nullable: true),
                xrefs_repointed = table.Column<int>(type: "integer", nullable: false),
                reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                merged_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_master_data_merge_histories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "master_data_xrefs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                source_system = table.Column<string>(type: "text", nullable: false),
                source_table = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                source_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                match_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_master_data_xrefs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "number_series",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                series = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                separator = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                pad_width = table.Column<int>(type: "integer", nullable: false),
                last_allocated = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_number_series", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_branches_code",
            table: "branches",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_branches_deleted_at",
            table: "branches",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_branches_is_active",
            table: "branches",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_branches_merged_into_id",
            table: "branches",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_branches_updated_at",
            table: "branches",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_master_data_change_logs_entity_type_entity_id_changed_at",
            table: "master_data_change_logs",
            columns: new[] { "entity_type", "entity_id", "changed_at" },
            descending: new[] { false, false, true });

        migrationBuilder.CreateIndex(
            name: "ix_master_data_merge_histories_entity_type_winner_id",
            table: "master_data_merge_histories",
            columns: new[] { "entity_type", "winner_id" });

        migrationBuilder.CreateIndex(
            name: "ix_master_data_merge_histories_loser_id",
            table: "master_data_merge_histories",
            column: "loser_id");

        migrationBuilder.CreateIndex(
            name: "ix_master_data_xrefs_entity_type_entity_id",
            table: "master_data_xrefs",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "ix_master_data_xrefs_source_system_source_table_source_key",
            table: "master_data_xrefs",
            columns: new[] { "source_system", "source_table", "source_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_number_series_series",
            table: "number_series",
            column: "series",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "branches");

        migrationBuilder.DropTable(
            name: "master_data_change_logs");

        migrationBuilder.DropTable(
            name: "master_data_merge_histories");

        migrationBuilder.DropTable(
            name: "master_data_xrefs");

        migrationBuilder.DropTable(
            name: "number_series");
    }
}
