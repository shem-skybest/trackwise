using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddBooking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id",
            table: "tariff_rates");

        migrationBuilder.DropForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id1",
            table: "tariff_rates");

        migrationBuilder.DropIndex(
            name: "ix_tariff_rates_charge_code_id1",
            table: "tariff_rates");

        migrationBuilder.DropColumn(
            name: "charge_code_id1",
            table: "tariff_rates");

        migrationBuilder.AddColumn<Guid>(
            name: "booking_id",
            table: "loads",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                carrier_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                equipment_type = table.Column<string>(type: "text", nullable: true),
                requested_pickup_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                confirmed_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
                table.ForeignKey(
                    name: "fk_bookings_quotation_quotation_id",
                    column: x => x.quotation_id,
                    principalTable: "quotations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_loads_booking_id",
            table: "loads",
            column: "booking_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_customer_id_status",
            table: "bookings",
            columns: new[] { "customer_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_number",
            table: "bookings",
            column: "number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bookings_quotation_id",
            table: "bookings",
            column: "quotation_id",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_loads_bookings_booking_id",
            table: "loads",
            column: "booking_id",
            principalTable: "bookings",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id",
            table: "tariff_rates",
            column: "charge_code_id",
            principalTable: "charge_codes",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_loads_bookings_booking_id",
            table: "loads");

        migrationBuilder.DropForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id",
            table: "tariff_rates");

        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_loads_booking_id",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "booking_id",
            table: "loads");

        migrationBuilder.AddColumn<Guid>(
            name: "charge_code_id1",
            table: "tariff_rates",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_tariff_rates_charge_code_id1",
            table: "tariff_rates",
            column: "charge_code_id1");

        migrationBuilder.AddForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id",
            table: "tariff_rates",
            column: "charge_code_id",
            principalTable: "charge_codes",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_tariff_rates_charge_codes_charge_code_id1",
            table: "tariff_rates",
            column: "charge_code_id1",
            principalTable: "charge_codes",
            principalColumn: "id");
    }
}
