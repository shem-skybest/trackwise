using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddRateAgreements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "rate_agreement_id",
            table: "trips",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "rate_base_amount",
            table: "trips",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "rate_extra_drop_amount",
            table: "trips",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "rate_extra_drops",
            table: "trips",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "rate_fuel_amount",
            table: "trips",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "rate_fuel_basis",
            table: "trips",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "rate_line_label",
            table: "trips",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "rated_at",
            table: "trips",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "rated_on",
            table: "trips",
            type: "date",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "diesel_prices",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                price_per_litre = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_diesel_prices", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "rate_agreements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                fuel_method = table.Column<string>(type: "text", nullable: false),
                extra_drop_charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                foul_trip_charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                supersedes_id = table.Column<Guid>(type: "uuid", nullable: true),
                approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rate_agreements", x => x.id);
                table.ForeignKey(
                    name: "fk_rate_agreements_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_rate_agreements_rate_agreements_supersedes_id",
                    column: x => x.supersedes_id,
                    principalTable: "rate_agreements",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "rate_agreement_lines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                rate_agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                tonnage = table.Column<string>(type: "text", nullable: true),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                min_km = table.Column<int>(type: "integer", nullable: true),
                max_km = table.Column<int>(type: "integer", nullable: true),
                city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                distance_km = table.Column<double>(type: "double precision", nullable: true),
                fuel_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rate_agreement_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_rate_agreement_lines_rate_agreements_rate_agreement_id",
                    column: x => x.rate_agreement_id,
                    principalTable: "rate_agreements",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "rate_fuel_surcharges",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                rate_agreement_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                diesel_price = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rate_fuel_surcharges", x => x.id);
                table.ForeignKey(
                    name: "fk_rate_fuel_surcharges_rate_agreement_lines_rate_agreement_li",
                    column: x => x.rate_agreement_line_id,
                    principalTable: "rate_agreement_lines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_trips_rate_agreement_id",
            table: "trips",
            column: "rate_agreement_id");

        migrationBuilder.CreateIndex(
            name: "ix_diesel_prices_effective_from",
            table: "diesel_prices",
            column: "effective_from",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_rate_agreement_lines_rate_agreement_id_sort_order",
            table: "rate_agreement_lines",
            columns: new[] { "rate_agreement_id", "sort_order" });

        migrationBuilder.CreateIndex(
            name: "ix_rate_agreements_customer_id_status_effective_from",
            table: "rate_agreements",
            columns: new[] { "customer_id", "status", "effective_from" });

        migrationBuilder.CreateIndex(
            name: "ix_rate_agreements_supersedes_id",
            table: "rate_agreements",
            column: "supersedes_id");

        migrationBuilder.CreateIndex(
            name: "ix_rate_fuel_surcharges_rate_agreement_line_id_diesel_price",
            table: "rate_fuel_surcharges",
            columns: new[] { "rate_agreement_line_id", "diesel_price" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_trips_rate_agreements_rate_agreement_id",
            table: "trips",
            column: "rate_agreement_id",
            principalTable: "rate_agreements",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_trips_rate_agreements_rate_agreement_id",
            table: "trips");

        migrationBuilder.DropTable(
            name: "diesel_prices");

        migrationBuilder.DropTable(
            name: "rate_fuel_surcharges");

        migrationBuilder.DropTable(
            name: "rate_agreement_lines");

        migrationBuilder.DropTable(
            name: "rate_agreements");

        migrationBuilder.DropIndex(
            name: "ix_trips_rate_agreement_id",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_agreement_id",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_base_amount",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_extra_drop_amount",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_extra_drops",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_fuel_amount",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_fuel_basis",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rate_line_label",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rated_at",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "rated_on",
            table: "trips");
    }
}
