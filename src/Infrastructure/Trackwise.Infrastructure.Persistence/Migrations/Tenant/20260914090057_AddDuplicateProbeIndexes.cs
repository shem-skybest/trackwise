using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Makes the duplicate probe's exact-match rules index-usable, which is what lets the planner
/// combine them with the trigram index instead of reading the whole table.
/// <para>
/// The probe compares identifiers as <c>upper(btrim(code))</c>, and an index on plain
/// <c>code</c> cannot answer that. Until both sides of the probe's OR have an index, PostgreSQL
/// has no BitmapOr to build and falls back to a sequential scan - so the trigram index added
/// alongside each entity was doing nothing. Branches is included because the defect is in the
/// shared probe, not in ports.
/// </para>
/// <para>
/// No model change, so the migration is hand-written. The expression must match the probe's
/// predicate exactly.
/// </para>
/// </summary>
public partial class AddDuplicateProbeIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_branches_code_probe
            ON branches (upper(btrim(code)));
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_terminals_code_probe
            ON terminals (upper(btrim(code)));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_branches_code_probe;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminals_code_probe;");
    }
}
