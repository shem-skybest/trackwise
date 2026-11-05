using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Promotes <c>employees</c> to a master-data record: soft delete, the active flag, the merge
/// tombstone pointer, branch scoping, a generated code and a synced display name - plus the
/// trigram and exact-match probe indexes the duplicate probe needs.
/// <para>
/// Hand-written because the model cannot express any of: backfilling <c>is_active</c> and
/// <c>name</c> for rows that predate both columns; allocating a <c>code</c> for existing rows,
/// which - unlike every other promoted entity - had no prior identifier to reuse, so this
/// migration also seeds the <c>Employee</c> row in <c>number_series</c> so the first code
/// <see cref="Trackwise.Infrastructure.Persistence.Services.MasterData.MasterDataCodeGenerator"/>
/// allocates afterwards continues the sequence rather than colliding with it; and the trigram /
/// exact-match indexes, whose expressions must match the probe predicate exactly
/// (<c>lower(btrim(col))</c> / <c>upper(btrim(col))</c>) or PostgreSQL cannot use them.
/// </para>
/// </summary>
public partial class AddEmployeesMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            table: "employees",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "code",
            table: "employees",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "employees",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<string>(
            name: "created_by",
            table: "employees",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "employees",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deleted_by",
            table: "employees",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "employees",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "merged_into_id",
            table: "employees",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "name",
            table: "employees",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "employees",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "updated_by",
            table: "employees",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        // Every employee that existed before this migration predates all three columns and must
        // be backfilled BEFORE the unique index on code is created below - every row starts out
        // with the AddColumn default ('' for code and name, false for is_active), and a filtered
        // unique index over a table full of duplicate empty strings fails to build. Taking the
        // column defaults permanently would also silently deactivate the whole roster and leave
        // every employee nameless; defaults stay as-is only because EF writes the real value
        // explicitly on insert from here on.
        migrationBuilder.Sql("UPDATE employees SET is_active = true;");
        migrationBuilder.Sql("UPDATE employees SET name = btrim(first_name || ' ' || last_name);");

        // Unlike every other promoted entity, Employee has no prior identifier to reuse as its
        // code - it is always allocated. Assign existing rows EMP-000001.. in a stable order and
        // seed number_series so the generator's first real allocation continues the sequence
        // instead of colliding with what this backfill just assigned.
        migrationBuilder.Sql("""
            WITH numbered AS (
                SELECT id, ROW_NUMBER() OVER (ORDER BY created_at, id) AS rn
                FROM employees
            )
            UPDATE employees
            SET code = 'EMP-' || LPAD(numbered.rn::text, 6, '0')
            FROM numbered
            WHERE employees.id = numbered.id;
            """);

        migrationBuilder.Sql("""
            INSERT INTO number_series (id, series, prefix, separator, pad_width, last_allocated, created_at)
            SELECT gen_random_uuid(), 'Employee', 'EMP', '-', 6, COUNT(*), CURRENT_TIMESTAMP
            FROM employees
            ON CONFLICT (series) DO UPDATE
            SET last_allocated = GREATEST(number_series.last_allocated, EXCLUDED.last_allocated);
            """);

        migrationBuilder.CreateIndex(
            name: "ix_employees_branch_id",
            table: "employees",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_employees_code",
            table: "employees",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_employees_deleted_at",
            table: "employees",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_employees_is_active",
            table: "employees",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_employees_merged_into_id",
            table: "employees",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_employees_updated_at",
            table: "employees",
            column: "updated_at");

        migrationBuilder.AddForeignKey(
            name: "fk_employees_branch_branch_id",
            table: "employees",
            column: "branch_id",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_employees_employees_merged_into_id",
            table: "employees",
            column: "merged_into_id",
            principalTable: "employees",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // Must match DuplicateProbe's predicate character for character, or the index is unused
        // and every probe against an employee degrades to a sequential scan.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_employees_name_trgm
            ON employees
            USING gin (lower(btrim(name)) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_employees_code_probe
            ON employees (upper(btrim(code)));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // pg_trgm itself is deliberately left installed: other entities' indexes depend on it,
        // and dropping a shared extension on one entity's rollback would break the others.
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_employees_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_employees_code_probe;");

        // Leaves the Employee row in number_series behind on purpose: the series is tenant-wide
        // state or another entity's future codes might already reference it, and removing it here
        // would risk handing out a number a later re-run of this migration would collide with.

        migrationBuilder.DropForeignKey(
            name: "fk_employees_branch_branch_id",
            table: "employees");

        migrationBuilder.DropForeignKey(
            name: "fk_employees_employees_merged_into_id",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_branch_id",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_code",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_deleted_at",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_is_active",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_merged_into_id",
            table: "employees");

        migrationBuilder.DropIndex(
            name: "ix_employees_updated_at",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "branch_id",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "code",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "is_active",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "merged_into_id",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "name",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "employees");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "employees");
    }
}
