using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Promotes <c>trucks</c> to a master-data record: soft delete, the active flag, the merge
/// tombstone pointer, branch scoping and a display name - plus the trigram and exact-match probe
/// indexes the duplicate probe needs. The number column itself is untouched (still <c>number</c>,
/// not renamed to <c>code</c> - see <c>Truck.Number</c>'s doc comment).
/// <para>
/// Three parts are hand-written because the model cannot express them: the backfill of
/// <c>is_active</c> and <c>name</c> for rows that predate both columns, and the trigram/exact-match
/// indexes, whose expressions must match the probe predicate exactly (<c>lower(btrim(col))</c> /
/// <c>upper(btrim(col))</c>) or PostgreSQL cannot use them.
/// </para>
/// </summary>
public partial class AddTrucksMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_trucks_number",
            table: "trucks");

        migrationBuilder.AlterColumn<string>(
            name: "number",
            table: "trucks",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            table: "trucks",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "trucks",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<string>(
            name: "created_by",
            table: "trucks",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "trucks",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deleted_by",
            table: "trucks",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "trucks",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "merged_into_id",
            table: "trucks",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "name",
            table: "trucks",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "trucks",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "updated_by",
            table: "trucks",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_trucks_branch_id",
            table: "trucks",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_deleted_at",
            table: "trucks",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_is_active",
            table: "trucks",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_merged_into_id",
            table: "trucks",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_number",
            table: "trucks",
            column: "number",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_updated_at",
            table: "trucks",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_trucks_vin",
            table: "trucks",
            column: "vin",
            unique: true,
            filter: "deleted_at IS NULL AND vin IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "fk_trucks_branches_branch_id",
            table: "trucks",
            column: "branch_id",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_trucks_trucks_merged_into_id",
            table: "trucks",
            column: "merged_into_id",
            principalTable: "trucks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // Every truck that existed before this migration predates both flags. Taking the column
        // defaults would silently deactivate the entire fleet and leave every truck nameless -
        // the is_active default stays false, and name stays '', only because EF writes the real
        // value explicitly on insert.
        migrationBuilder.Sql("UPDATE trucks SET is_active = true;");
        migrationBuilder.Sql("UPDATE trucks SET name = number WHERE name = '';");

        // Must match DuplicateProbe's predicate character for character, or the index is unused
        // and every probe against a truck degrades to a sequential scan.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_trucks_name_trgm
            ON trucks
            USING gin (lower(btrim(name)) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_trucks_number_probe
            ON trucks (upper(btrim(number)));
            """);

        // Truck's own "second exact rule" (VIN), same shape as Trailer's - without this index
        // the OR has only two of its three arms indexed and the planner gives up on the
        // BitmapOr entirely.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_trucks_vin_probe
            ON trucks (upper(btrim(vin)));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // pg_trgm itself is deliberately left installed: other entities' indexes depend on it,
        // and dropping a shared extension on one entity's rollback would break the others.
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_trucks_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_trucks_number_probe;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_trucks_vin_probe;");

        migrationBuilder.DropForeignKey(
            name: "fk_trucks_branches_branch_id",
            table: "trucks");

        migrationBuilder.DropForeignKey(
            name: "fk_trucks_trucks_merged_into_id",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_branch_id",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_deleted_at",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_is_active",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_merged_into_id",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_number",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_updated_at",
            table: "trucks");

        migrationBuilder.DropIndex(
            name: "ix_trucks_vin",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "branch_id",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "is_active",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "merged_into_id",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "name",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "trucks");

        migrationBuilder.AlterColumn<string>(
            name: "number",
            table: "trucks",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30);

        migrationBuilder.CreateIndex(
            name: "ix_trucks_number",
            table: "trucks",
            column: "number",
            unique: true);
    }
}
