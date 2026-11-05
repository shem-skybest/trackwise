using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddGensetMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "genset_billing_type",
            table: "workfiles",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "genset_flat_amount",
            table: "workfiles",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "genset_hourly_rate",
            table: "workfiles",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "genset_hours_used",
            table: "workfiles",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "genset_id",
            table: "workfiles",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "genset_notes",
            table: "workfiles",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "gensets",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                serial_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                merged_into_id = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_gensets", x => x.id);
                table.ForeignKey(
                    name: "fk_gensets_branches_branch_id",
                    column: x => x.branch_id,
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_gensets_gensets_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "gensets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_workfiles_genset_id",
            table: "workfiles",
            column: "genset_id");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_branch_id",
            table: "gensets",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_code",
            table: "gensets",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_deleted_at",
            table: "gensets",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_is_active",
            table: "gensets",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_merged_into_id",
            table: "gensets",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_serial_number",
            table: "gensets",
            column: "serial_number",
            unique: true,
            filter: "deleted_at IS NULL AND serial_number IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_status",
            table: "gensets",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_gensets_updated_at",
            table: "gensets",
            column: "updated_at");

        migrationBuilder.AddForeignKey(
            name: "fk_workfiles_genset_genset_id",
            table: "workfiles",
            column: "genset_id",
            principalTable: "gensets",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_workfiles_genset_genset_id",
            table: "workfiles");

        migrationBuilder.DropTable(
            name: "gensets");

        migrationBuilder.DropIndex(
            name: "ix_workfiles_genset_id",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_billing_type",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_flat_amount",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_hourly_rate",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_hours_used",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_id",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "genset_notes",
            table: "workfiles");
    }
}
