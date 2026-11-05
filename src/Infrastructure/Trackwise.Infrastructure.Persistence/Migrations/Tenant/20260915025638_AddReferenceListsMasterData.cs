using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// The three reference lists that follow ports: vessels, commodities and locations.
/// <para>
/// Every table here is new, and two of the three have no legacy table behind them at all -
/// nSFS keeps the vessel and the commodity as free text, which is the duplication this module
/// exists to stop rather than to migrate.
/// </para>
/// <para>
/// The hand-written SQL at the end of Up is the duplicate probe's index set, which EF cannot
/// generate: the probe compares <c>lower(btrim(col))</c> and <c>upper(btrim(col))</c>, and an
/// index on the plain column cannot answer either. Both halves of the probe's OR need one, or
/// PostgreSQL has no BitmapOr to build and falls back to a sequential scan - see
/// <c>AddDuplicateProbeIndexes</c>, which is where that was found out the hard way.
/// </para>
/// </summary>
public partial class AddReferenceListsMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "commodities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                hs_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_hazardous = table.Column<bool>(type: "boolean", nullable: false),
                un_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                requires_temperature_control = table.Column<bool>(type: "boolean", nullable: false),
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
                table.PrimaryKey("pk_commodities", x => x.id);
                table.ForeignKey(
                    name: "fk_commodities_branches_branch_id",
                    column: x => x.branch_id,
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_commodities_commodities_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "commodities",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "locations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                parent_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                latitude = table.Column<double>(type: "double precision", nullable: true),
                longitude = table.Column<double>(type: "double precision", nullable: true),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                branch_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                table.PrimaryKey("pk_locations", x => x.id);
                table.ForeignKey(
                    name: "fk_locations_branches_branch_id",
                    column: x => x.branch_id,
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_locations_locations_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_locations_locations_parent_location_id",
                    column: x => x.parent_location_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "vessels",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                imo_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                call_sign = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                flag_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                type = table.Column<string>(type: "text", nullable: false),
                teu_capacity = table.Column<int>(type: "integer", nullable: true),
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
                table.PrimaryKey("pk_vessels", x => x.id);
                table.ForeignKey(
                    name: "fk_vessels_branches_branch_id",
                    column: x => x.branch_id,
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_vessels_vessels_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "vessels",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_commodities_branch_id",
            table: "commodities",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_code",
            table: "commodities",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_deleted_at",
            table: "commodities",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_hs_code",
            table: "commodities",
            column: "hs_code");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_is_active",
            table: "commodities",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_merged_into_id",
            table: "commodities",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_commodities_updated_at",
            table: "commodities",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_locations_branch_id",
            table: "locations",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_locations_code",
            table: "locations",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_locations_deleted_at",
            table: "locations",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_locations_is_active",
            table: "locations",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_locations_merged_into_id",
            table: "locations",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_locations_parent_location_id",
            table: "locations",
            column: "parent_location_id");

        migrationBuilder.CreateIndex(
            name: "ix_locations_updated_at",
            table: "locations",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_branch_id",
            table: "vessels",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_code",
            table: "vessels",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_deleted_at",
            table: "vessels",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_imo_number",
            table: "vessels",
            column: "imo_number",
            unique: true,
            filter: "deleted_at IS NULL AND imo_number IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_is_active",
            table: "vessels",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_merged_into_id",
            table: "vessels",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_vessels_updated_at",
            table: "vessels",
            column: "updated_at");

        // Trigram indexes for the fuzzy name rules. The expression must match the probe's
        // predicate exactly, and only the % operator the probe pre-filters with can be answered
        // from gin_trgm_ops - similarity() is a per-row function call no index reaches.
        // pg_trgm itself was installed by AddTrigramDuplicateProbeSupport.
        foreach (var table in new[] { "vessels", "commodities", "locations" })
        {
            migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS ix_{table}_name_trgm
                ON {table}
                USING gin (lower(btrim(name)) gin_trgm_ops);
                """);

            migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS ix_{table}_code_probe
                ON {table} (upper(btrim(code)));
                """);
        }

        // Vessel is the first entity with a second exact rule, and it needs its own expression
        // index for the same reason the code rule does: without one the OR has only two of its
        // three arms indexed, and the planner gives up on the BitmapOr entirely.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_vessels_imo_number_probe
            ON vessels (upper(btrim(imo_number)));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Dropped ahead of the tables they sit on. Redundant - dropping a table takes its
        // indexes - but explicit, so a partial rollback that stops here leaves nothing behind.
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_vessels_imo_number_probe;");

        foreach (var table in new[] { "vessels", "commodities", "locations" })
        {
            migrationBuilder.Sql($"DROP INDEX IF EXISTS ix_{table}_name_trgm;");
            migrationBuilder.Sql($"DROP INDEX IF EXISTS ix_{table}_code_probe;");
        }

        migrationBuilder.DropTable(
            name: "commodities");

        migrationBuilder.DropTable(
            name: "locations");

        migrationBuilder.DropTable(
            name: "vessels");
    }
}
