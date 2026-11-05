using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <summary>
    /// Enables trigram similarity so the duplicate probe can catch "San Miguel Corp" against
    /// "San Miguel Corporation" - the class of duplicate exact matching never finds, and the main
    /// reason the module exists.
    /// <para>
    /// No model change, so the migration is hand-written. The index expression must match the
    /// probe's predicate exactly (<c>lower(btrim(col))</c>) or PostgreSQL cannot use it and every
    /// probe degrades to a sequential scan.
    /// </para>
    /// </summary>
    public partial class AddTrigramDuplicateProbeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_branches_name_trgm
                ON branches
                USING gin (lower(btrim(name)) gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_branches_name_trgm;");

            // The extension is deliberately left installed: other entities' probe indexes depend on
            // it, and dropping a shared extension on one entity's rollback would break them.
        }
    }
}
