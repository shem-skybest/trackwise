using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Promotes <c>terminals</c> to a master-data record: soft delete, the active flag, the merge
/// tombstone pointer and branch scoping, plus the trigram index the duplicate probe needs.
/// <para>
/// Two parts are hand-written because the model cannot express them - the backfill of
/// <c>is_active</c>, and the trigram index, whose expression must match the probe predicate
/// exactly (<c>lower(btrim(col))</c>) or PostgreSQL cannot use it.
/// </para>
/// </summary>
public partial class AddPortsMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_terminals_code",
            table: "terminals");

        migrationBuilder.AlterColumn<string>(
            name: "code",
            table: "terminals",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(5)",
            oldMaxLength: 5);

        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            table: "terminals",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "terminals",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deleted_by",
            table: "terminals",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "terminals",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "merged_into_id",
            table: "terminals",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "references_repointed",
            table: "master_data_merge_histories",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "ix_terminals_branch_id",
            table: "terminals",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_terminals_code",
            table: "terminals",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_terminals_deleted_at",
            table: "terminals",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_terminals_is_active",
            table: "terminals",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_terminals_merged_into_id",
            table: "terminals",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_terminals_updated_at",
            table: "terminals",
            column: "updated_at");

        migrationBuilder.AddForeignKey(
            name: "fk_terminals_branches_branch_id",
            table: "terminals",
            column: "branch_id",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_terminals_terminals_merged_into_id",
            table: "terminals",
            column: "merged_into_id",
            principalTable: "terminals",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // Every terminal that existed before this migration predates the flag and is in use.
        // Taking the column default would silently deactivate the entire port list - the
        // column default stays false only because EF writes the value explicitly on insert.
        migrationBuilder.Sql("UPDATE terminals SET is_active = true;");

        // Must match DuplicateProbe's predicate character for character, or the index is unused
        // and every probe against a port name degrades to a sequential scan.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_terminals_name_trgm
            ON terminals
            USING gin (lower(btrim(name)) gin_trgm_ops);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // pg_trgm itself is deliberately left installed: the branches index depends on it, and
        // dropping a shared extension on one entity's rollback would break the other.
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminals_name_trgm;");

        migrationBuilder.DropForeignKey(
            name: "fk_terminals_branches_branch_id",
            table: "terminals");

        migrationBuilder.DropForeignKey(
            name: "fk_terminals_terminals_merged_into_id",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_branch_id",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_code",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_deleted_at",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_is_active",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_merged_into_id",
            table: "terminals");

        migrationBuilder.DropIndex(
            name: "ix_terminals_updated_at",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "branch_id",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "is_active",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "merged_into_id",
            table: "terminals");

        migrationBuilder.DropColumn(
            name: "references_repointed",
            table: "master_data_merge_histories");

        migrationBuilder.AlterColumn<string>(
            name: "code",
            table: "terminals",
            type: "character varying(5)",
            maxLength: 5,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30);

        migrationBuilder.CreateIndex(
            name: "ix_terminals_code",
            table: "terminals",
            column: "code",
            unique: true);
    }
}
