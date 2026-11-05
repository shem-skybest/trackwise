using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Container types and currencies - the two lists step 6 split out because neither was the
/// routine new-table shape the other three reference lists were.
/// <para>
/// Container types are a <b>reconciliation, not an addition</b>. Two enums already described
/// the same boxes in two vocabularies on two aggregates - <c>ContainerIsoType</c> on
/// <c>containers</c>, <c>EquipmentType</c> on <c>bookings</c> and <c>tariffs</c> - and neither
/// was a superset of the other: ISO had no 40' high-cube reefer, the load board had no 45' high
/// cube, and it called a road tanker and a tank container the same thing. This migration seeds
/// their union, cross-references both retired vocabularies into <c>master_data_xrefs</c>,
/// backfills the three foreign keys <i>through those cross-references</i>, and only then drops
/// the enum columns.
/// </para>
/// <para>
/// <b>The statement order in Up is the migration.</b> EF scaffolded the drops first and gave
/// <c>containers.container_type_id</c> a zero-GUID default, which would have pointed every
/// existing container at a row that does not exist and lost the type it actually had. Seed,
/// cross-reference, backfill, verify, then drop - and the column is added nullable and tightened
/// to NOT NULL only once the backfill has provably filled it.
/// </para>
/// <para>
/// Currencies are the list alone. FX rates are effective-dated, and effective dating is the
/// rates step's machinery - a rate table here could not answer "what was the rate in March",
/// which is the only question anyone asks it.
/// </para>
/// </summary>
public partial class AddContainerTypesAndCurrencies : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "container_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                iso_size_type_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                category = table.Column<string>(type: "text", nullable: false),
                length_feet = table.Column<int>(type: "integer", nullable: true),
                is_high_cube = table.Column<bool>(type: "boolean", nullable: false),
                teu_factor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                table.PrimaryKey("pk_container_types", x => x.id);
                table.ForeignKey(
                    name: "fk_container_types_container_types_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "container_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "currencies",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                numeric_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                minor_units = table.Column<int>(type: "integer", nullable: false),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                table.PrimaryKey("pk_currencies", x => x.id);
                table.ForeignKey(
                    name: "fk_currencies_currencies_merged_into_id",
                    column: x => x.merged_into_id,
                    principalTable: "currencies",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_container_types_code",
            table: "container_types",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_container_types_deleted_at",
            table: "container_types",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_container_types_is_active",
            table: "container_types",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_container_types_iso_size_type_code",
            table: "container_types",
            column: "iso_size_type_code",
            unique: true,
            filter: "deleted_at IS NULL AND iso_size_type_code IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_container_types_merged_into_id",
            table: "container_types",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_container_types_updated_at",
            table: "container_types",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_code",
            table: "currencies",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_deleted_at",
            table: "currencies",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_is_active",
            table: "currencies",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_merged_into_id",
            table: "currencies",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_numeric_code",
            table: "currencies",
            column: "numeric_code",
            unique: true,
            filter: "deleted_at IS NULL AND numeric_code IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_currencies_updated_at",
            table: "currencies",
            column: "updated_at");

        migrationBuilder.Sql("""
                -- The union of the two retired vocabularies, in one list. Ids are generated here and every
                -- statement below joins on the code, so nothing depends on a hardcoded GUID.
                INSERT INTO container_types
                    (id, code, name, iso_size_type_code, category, length_feet, is_high_cube, teu_factor, is_active, created_at)
                VALUES
                    (gen_random_uuid(), 'GP20',   '20'' General Purpose',  '22G1', 'general_purpose', 20,   false, 1.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'GP40',   '40'' General Purpose',  '42G1', 'general_purpose', 40,   false, 2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'HC40',   '40'' High Cube',        '45G1', 'general_purpose', 40,   true,  2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'HC45',   '45'' High Cube',        'L5G1', 'general_purpose', 45,   true,  2.25, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'RF20',   '20'' Reefer',           '22R1', 'reefer',          20,   false, 1.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'RF40',   '40'' Reefer',           '42R1', 'reefer',          40,   false, 2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'RF40HC', '40'' High Cube Reefer', '45R1', 'reefer',          40,   true,  2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'OT20',   '20'' Open Top',         '22U1', 'open_top',        20,   false, 1.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'OT40',   '40'' Open Top',         '42U1', 'open_top',        40,   false, 2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'FR20',   '20'' Flat Rack',        '22P1', 'flat_rack',       20,   false, 1.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'FR40',   '40'' Flat Rack',        '42P1', 'flat_rack',       40,   false, 2.00, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'TK20',   '20'' Tank Container',   '22T1', 'tank',            20,   false, 1.00, true, CURRENT_TIMESTAMP),
                    -- The two with no ISO code, because the standard describes containers and neither is one. A
                    -- road tanker is not a 20' tank container, which is why TANKER did not simply become TK20 -
                    -- collapsing them would have been the reconciliation inventing a fact.
                    (gen_random_uuid(), 'TANK',   'Road Tanker',           NULL,   'tank',            NULL, false, NULL, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'BULK',   'Bulk',                  NULL,   'bulk',            NULL, false, NULL, true, CURRENT_TIMESTAMP)
                ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql("""
                -- The three the retired CurrencyCode enum carried. A steward adds the rest; nothing here is a
                -- ceiling, which is the entire point of promoting the enum to a table.
                INSERT INTO currencies (id, code, name, numeric_code, symbol, minor_units, is_active, created_at)
                VALUES
                    (gen_random_uuid(), 'USD', 'US Dollar',       '840', '$', 2, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'EUR', 'Euro',            '978', '€', 2, true, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'PHP', 'Philippine Peso', '608', '₱', 2, true, CURRENT_TIMESTAMP)
                ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql("""
                -- The two retired vocabularies, kept as cross-references rather than thrown away. This is what
                -- makes the reconciliation survivable: a legacy value still resolves to the row it became, the
                -- backfills below join through this table instead of repeating the mapping, and Down reads the
                -- same rows backwards to restore the enum text.
                INSERT INTO master_data_xrefs (id, source_system, source_table, source_key, entity_type, entity_id, match_note, created_at)
                SELECT gen_random_uuid(), 'mdm', v.source_table, v.source_key, 'ContainerType', t.id, v.note, CURRENT_TIMESTAMP
                FROM (VALUES
                    ('ContainerIsoType', 'gp20',         'GP20',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'gp40',         'GP40',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'hc40',         'HC40',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'hc45',         'HC45',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'rf20',         'RF20',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'rf40',         'RF40',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'ot20',         'OT20',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'ot40',         'OT40',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'fr20',         'FR20',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'fr40',         'FR40',   'Retired ContainerIsoType vocabulary'),
                    ('ContainerIsoType', 'tk20',         'TK20',   'Retired ContainerIsoType vocabulary'),
                    ('EquipmentType',    'dry_van_20',   'GP20',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'dry_van_40',   'GP40',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'dry_van_40hc', 'HC40',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'reefer_20',    'RF20',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'reefer_40',    'RF40',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'reefer_40hc',  'RF40HC', 'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'flat_rack_20', 'FR20',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'flat_rack_40', 'FR40',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'open_top_20',  'OT20',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'open_top_40',  'OT40',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'tanker',       'TANK',   'Retired EquipmentType vocabulary'),
                    ('EquipmentType',    'bulk',         'BULK',   'Retired EquipmentType vocabulary')
                ) AS v(source_table, source_key, code, note)
                JOIN container_types t ON t.code = v.code
                ON CONFLICT DO NOTHING;
            """);

        // Added nullable on all three, containers included. The backfill below is what fills
        // it; a NOT NULL column with a default would have been quietly satisfied by the default
        // and every container would now claim to be a box that does not exist.
        migrationBuilder.AddColumn<Guid>(
            name: "container_type_id",
            table: "containers",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "container_type_id",
            table: "bookings",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "container_type_id",
            table: "tariffs",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
                UPDATE containers c
                SET container_type_id = x.entity_id
                FROM master_data_xrefs x
                WHERE x.entity_type = 'ContainerType'
                  AND x.source_table = 'ContainerIsoType'
                  AND x.source_key = c.iso_type;

                UPDATE bookings b
                SET container_type_id = x.entity_id
                FROM master_data_xrefs x
                WHERE x.entity_type = 'ContainerType'
                  AND x.source_table = 'EquipmentType'
                  AND x.source_key = b.equipment_type;

                UPDATE tariffs t
                SET container_type_id = x.entity_id
                FROM master_data_xrefs x
                WHERE x.entity_type = 'ContainerType'
                  AND x.source_table = 'EquipmentType'
                  AND x.source_key = t.equipment_type;
            """);

        migrationBuilder.Sql("""
                -- Stop rather than default the survivors to a box they are not. containers.iso_type was non-null
                -- and every one of its values is mapped above, so a row surviving here means the column held
                -- something this migration has never heard of. The next statement makes the column NOT NULL, and
                -- a default at that point would be a silent relabel nobody would ever notice.
                DO $$
                DECLARE unmapped int;
                BEGIN
                    SELECT count(*) INTO unmapped FROM containers WHERE container_type_id IS NULL;
                    IF unmapped > 0 THEN
                        RAISE EXCEPTION
                            '% container row(s) have an iso_type with no container_types mapping. Add the missing type and its master_data_xrefs row, then re-run.', unmapped;
                    END IF;
                END $$;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "container_type_id",
            table: "containers",
            type: "uuid",
            nullable: false);

        // Dropped last. Every statement above reads them.
        migrationBuilder.DropColumn(
            name: "iso_type",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "equipment_type",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "equipment_type",
            table: "tariffs");

        migrationBuilder.CreateIndex(
            name: "ix_tariffs_container_type_id",
            table: "tariffs",
            column: "container_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_containers_container_type_id",
            table: "containers",
            column: "container_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_container_type_id",
            table: "bookings",
            column: "container_type_id");

        migrationBuilder.AddForeignKey(
            name: "fk_bookings_container_type_container_type_id",
            table: "bookings",
            column: "container_type_id",
            principalTable: "container_types",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_containers_container_type_container_type_id",
            table: "containers",
            column: "container_type_id",
            principalTable: "container_types",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_tariffs_container_type_container_type_id",
            table: "tariffs",
            column: "container_type_id",
            principalTable: "container_types",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // The duplicate probe's index set, which EF cannot generate: the probe compares
        // lower(btrim(col)) and upper(btrim(col)), and an index on the plain column answers
        // neither. Both halves of its OR need one, or PostgreSQL has no BitmapOr to build and
        // falls back to a sequential scan - see AddDuplicateProbeIndexes, where that was found
        // out the hard way. pg_trgm itself was installed by AddTrigramDuplicateProbeSupport.
        foreach (var table in new[] { "container_types", "currencies" })
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

        // Each entity's second exact rule needs its own expression index, for the reason the
        // code rule does: without one the OR has an unindexed arm and the planner abandons the
        // BitmapOr entirely. Vessel's IMO number is the precedent.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_container_types_iso_size_type_code_probe
            ON container_types (upper(btrim(iso_size_type_code)));
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_currencies_numeric_code_probe
            ON currencies (upper(btrim(numeric_code)));
            """);
    }

    /// <summary>
    /// Puts the two enum columns back, <b>with their values</b>. Reversing the reconciliation
    /// is not a matter of restoring the columns: the type each row actually had now lives only
    /// in <c>container_types</c>, so the enum text has to be read back out through the same
    /// cross-references the forward backfill used, before the tables holding them are dropped.
    /// Scaffolding alone would have left every container with an empty <c>iso_type</c>.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_container_types_iso_size_type_code_probe;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_currencies_numeric_code_probe;");

        foreach (var table in new[] { "container_types", "currencies" })
        {
            migrationBuilder.Sql($"DROP INDEX IF EXISTS ix_{table}_name_trgm;");
            migrationBuilder.Sql($"DROP INDEX IF EXISTS ix_{table}_code_probe;");
        }

        migrationBuilder.AddColumn<string>(
            name: "iso_type",
            table: "containers",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "equipment_type",
            table: "bookings",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "equipment_type",
            table: "tariffs",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        // Read back through the cross-references, so a row restores to the vocabulary it came
        // from. A type created after this migration ran has no cross-reference and cannot be
        // expressed in either enum at all - those containers keep the empty string the column
        // defaults to, which is the honest answer and is at least visible.
        migrationBuilder.Sql("""
            UPDATE containers c
            SET iso_type = x.source_key
            FROM master_data_xrefs x
            WHERE x.entity_type = 'ContainerType'
              AND x.source_table = 'ContainerIsoType'
              AND x.entity_id = c.container_type_id;

            UPDATE bookings b
            SET equipment_type = x.source_key
            FROM master_data_xrefs x
            WHERE x.entity_type = 'ContainerType'
              AND x.source_table = 'EquipmentType'
              AND x.entity_id = b.container_type_id;

            UPDATE tariffs t
            SET equipment_type = x.source_key
            FROM master_data_xrefs x
            WHERE x.entity_type = 'ContainerType'
              AND x.source_table = 'EquipmentType'
              AND x.entity_id = t.container_type_id;
            """);

        migrationBuilder.DropForeignKey(
            name: "fk_bookings_container_type_container_type_id",
            table: "bookings");

        migrationBuilder.DropForeignKey(
            name: "fk_containers_container_type_container_type_id",
            table: "containers");

        migrationBuilder.DropForeignKey(
            name: "fk_tariffs_container_type_container_type_id",
            table: "tariffs");

        migrationBuilder.DropIndex(
            name: "ix_bookings_container_type_id",
            table: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_containers_container_type_id",
            table: "containers");

        migrationBuilder.DropIndex(
            name: "ix_tariffs_container_type_id",
            table: "tariffs");

        migrationBuilder.DropColumn(
            name: "container_type_id",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "container_type_id",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "container_type_id",
            table: "tariffs");

        // Only this migration's own cross-references. Narrowed by entity_type and source_table
        // so a rollback cannot take an ETL run's rows with it.
        migrationBuilder.Sql("""
            DELETE FROM master_data_xrefs
            WHERE entity_type = 'ContainerType'
              AND source_table IN ('ContainerIsoType', 'EquipmentType');
            """);

        migrationBuilder.DropTable(
            name: "container_types");

        migrationBuilder.DropTable(
            name: "currencies");
    }
}
