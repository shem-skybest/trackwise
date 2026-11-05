using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Fixes address column naming on loads and posted_trucks. The previous migration
/// renamed origin_address_* and destination_address_* columns to unprefixed names, but
/// both OriginAddress and DestinationAddress (complex types) were mapped to the same
/// unprefixed columns, causing data collision. This migration:
/// - Adds prefixed origin_address_* and destination_address_* columns to loads
/// - Migrates origin data from the unprefixed columns to origin_address_*
/// - Adds available_at_address_* and destination_preference_* columns to posted_trucks
/// - Migrates available-at data from the unprefixed columns to available_at_address_*
/// - Drops the orphaned unprefixed columns
/// </summary>
public partial class FixAddressColumnPrefixes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── loads ──────────────────────────────────────────────────────────────────
        // Step 1: Add all the new prefixed columns (null-safe / with defaults for required ones)
        migrationBuilder.AddColumn<string>(
            name: "origin_address_barangay",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "origin_address_city_or_municipality",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_country",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_province",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_region",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_street_address",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_subdivision",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "origin_address_zipcode",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_barangay",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_address_city_or_municipality",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_country",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_province",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_region",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_street_address",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_subdivision",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_address_zipcode",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        // Step 2: Migrate origin address data from the current unprefixed columns
        // (these currently hold the origin data after the previous migration renamed them)
        migrationBuilder.Sql(@"
            UPDATE loads SET
                origin_address_barangay         = NULL,
                origin_address_city_or_municipality = COALESCE(city_or_municipality, ''),
                origin_address_country         = COALESCE(country, ''),
                origin_address_province        = COALESCE(province, ''),
                origin_address_region         = COALESCE(region, ''),
                origin_address_street_address = COALESCE(street_address, ''),
                origin_address_subdivision     = subdivision,
                origin_address_zipcode        = COALESCE(zipcode, '');
        ");

        // Step 3: Migrate destination address data from the unprefixed columns.
        // Note: the previous migration dropped destination_address_city/country/line1/state,
        // so destination_address_country/province/region/street_address will be empty strings.
        // barangay and city_or_municipality were kept (renamed from destination_line2 and
        // destination_zip_code respectively).
        migrationBuilder.Sql(@"
            UPDATE loads SET
                destination_address_barangay             = barangay,
                destination_address_city_or_municipality = COALESCE(city_or_municipality, ''),
                destination_address_country              = '',
                destination_address_province             = '',
                destination_address_region               = '',
                destination_address_street_address       = '',
                destination_address_subdivision          = NULL,
                destination_address_zipcode             = '';
        ");

        // Step 4: Drop the orphaned unprefixed address columns (origin data is now in origin_address_*)
        migrationBuilder.DropColumn(name: "barangay", table: "loads");
        migrationBuilder.DropColumn(name: "city_or_municipality", table: "loads");
        migrationBuilder.DropColumn(name: "country", table: "loads");
        migrationBuilder.DropColumn(name: "province", table: "loads");
        migrationBuilder.DropColumn(name: "region", table: "loads");
        migrationBuilder.DropColumn(name: "street_address", table: "loads");
        migrationBuilder.DropColumn(name: "subdivision", table: "loads");
        migrationBuilder.DropColumn(name: "zipcode", table: "loads");

        // ── load_board_listings ───────────────────────────────────────────────────
        // Same structural fix: rename unprefixed address columns to origin_address_*
        // (destination_address_* columns were dropped by the previous migration)
        migrationBuilder.AddColumn<string>(
            name: "origin_address_barangay",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "origin_address_city_or_municipality",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_country",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_province",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_region",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_street_address",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "origin_address_subdivision",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "origin_address_zipcode",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_barangay",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_address_city_or_municipality",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_country",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_province",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_region",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_street_address",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_address_subdivision",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_address_zipcode",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(@"
            UPDATE load_board_listings SET
                origin_address_barangay             = NULL,
                origin_address_city_or_municipality = COALESCE(city_or_municipality, ''),
                origin_address_country              = COALESCE(country, ''),
                origin_address_province             = COALESCE(province, ''),
                origin_address_region               = COALESCE(region, ''),
                origin_address_street_address       = COALESCE(street_address, ''),
                origin_address_subdivision          = subdivision,
                origin_address_zipcode             = COALESCE(zipcode, '');
        ");

        migrationBuilder.Sql(@"
            UPDATE load_board_listings SET
                destination_address_barangay             = barangay,
                destination_address_city_or_municipality = COALESCE(city_or_municipality, ''),
                destination_address_country              = '',
                destination_address_province             = '',
                destination_address_region               = '',
                destination_address_street_address      = '',
                destination_address_subdivision         = NULL,
                destination_address_zipcode             = '';
        ");

        migrationBuilder.DropColumn(name: "barangay", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "city_or_municipality", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "country", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "province", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "region", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "street_address", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "subdivision", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "zipcode", table: "load_board_listings");

        // ── posted_trucks ─────────────────────────────────────────────────────────
        // Both AvailableAtAddress and DestinationPreference were mapped to the same
        // unprefixed columns. Migrate AvailableAtAddress data to available_at_address_*,
        // then add destination_preference_* columns.
        migrationBuilder.AddColumn<string>(
            name: "available_at_address_barangay",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_city_or_municipality",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_country",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_province",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_region",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_street_address",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_subdivision",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "available_at_address_zipcode",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_barangay",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_city_or_municipality",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_country",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_province",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_region",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_street_address",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_subdivision",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "destination_preference_zipcode",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        // Migrate available-at address data (unprefixed columns currently hold available-at data)
        migrationBuilder.Sql(@"
            UPDATE posted_trucks SET
                available_at_address_barangay             = barangay,
                available_at_address_city_or_municipality = COALESCE(city_or_municipality, ''),
                available_at_address_country              = COALESCE(country, ''),
                available_at_address_province             = COALESCE(province, ''),
                available_at_address_region               = COALESCE(region, ''),
                available_at_address_street_address       = COALESCE(street_address, ''),
                available_at_address_subdivision           = subdivision,
                available_at_address_zipcode             = COALESCE(zipcode, '');
        ");

        // DestinationPreference stays empty (the previous migration renamed destination_preference_*
        // columns to the unprefixed names, so the destination preference data was lost)
        migrationBuilder.Sql(@"
            UPDATE posted_trucks SET
                destination_preference_barangay             = NULL,
                destination_preference_city_or_municipality = '',
                destination_preference_country              = '',
                destination_preference_province            = '',
                destination_preference_region              = '',
                destination_preference_street_address      = '',
                destination_preference_subdivision         = NULL,
                destination_preference_zipcode             = '';
        ");

        migrationBuilder.DropColumn(name: "barangay", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "city_or_municipality", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "country", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "province", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "region", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "street_address", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "subdivision", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "zipcode", table: "posted_trucks");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── loads ──────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "barangay",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "city_or_municipality",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "country",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "province",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "region",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "street_address",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "subdivision",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "zipcode",
            table: "loads",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(@"
            UPDATE loads SET
                barangay          = destination_address_barangay,
                city_or_municipality = destination_address_city_or_municipality,
                country           = destination_address_country,
                province          = destination_address_province,
                region            = destination_address_region,
                street_address    = destination_address_street_address,
                subdivision       = destination_address_subdivision,
                zipcode           = destination_address_zipcode;
        ");

        migrationBuilder.DropColumn(name: "origin_address_barangay", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_city_or_municipality", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_country", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_province", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_region", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_street_address", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_subdivision", table: "loads");
        migrationBuilder.DropColumn(name: "origin_address_zipcode", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_barangay", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_city_or_municipality", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_country", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_province", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_region", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_street_address", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_subdivision", table: "loads");
        migrationBuilder.DropColumn(name: "destination_address_zipcode", table: "loads");

        // ── load_board_listings ───────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "barangay",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "city_or_municipality",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "country",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "province",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "region",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "street_address",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "subdivision",
            table: "load_board_listings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "zipcode",
            table: "load_board_listings",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(@"
            UPDATE load_board_listings SET
                barangay          = destination_address_barangay,
                city_or_municipality = destination_address_city_or_municipality,
                country           = destination_address_country,
                province          = destination_address_province,
                region            = destination_address_region,
                street_address    = destination_address_street_address,
                subdivision       = destination_address_subdivision,
                zipcode           = destination_address_zipcode;
        ");

        migrationBuilder.DropColumn(name: "origin_address_barangay", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_city_or_municipality", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_country", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_province", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_region", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_street_address", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_subdivision", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "origin_address_zipcode", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_barangay", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_city_or_municipality", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_country", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_province", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_region", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_street_address", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_subdivision", table: "load_board_listings");
        migrationBuilder.DropColumn(name: "destination_address_zipcode", table: "load_board_listings");

        // ── posted_trucks ─────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "barangay",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "city_or_municipality",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "country",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "province",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "region",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "street_address",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "subdivision",
            table: "posted_trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "zipcode",
            table: "posted_trucks",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(@"
            UPDATE posted_trucks SET
                barangay          = available_at_address_barangay,
                city_or_municipality = available_at_address_city_or_municipality,
                country           = available_at_address_country,
                province          = available_at_address_province,
                region            = available_at_address_region,
                street_address    = available_at_address_street_address,
                subdivision       = available_at_address_subdivision,
                zipcode           = available_at_address_zipcode;
        ");

        migrationBuilder.DropColumn(name: "available_at_address_barangay", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_city_or_municipality", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_country", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_province", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_region", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_street_address", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_subdivision", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "available_at_address_zipcode", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_barangay", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_city_or_municipality", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_country", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_province", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_region", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_street_address", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_subdivision", table: "posted_trucks");
        migrationBuilder.DropColumn(name: "destination_preference_zipcode", table: "posted_trucks");
    }
}
