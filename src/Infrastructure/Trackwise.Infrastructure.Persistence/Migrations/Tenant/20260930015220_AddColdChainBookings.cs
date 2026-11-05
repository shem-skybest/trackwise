using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddColdChainBookings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "quotation_id",
            table: "bookings",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<TimeOnly>(
            name: "call_time",
            table: "bookings",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "cancelled_by_party",
            table: "bookings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "commodity",
            table: "bookings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "estimate_note",
            table: "bookings",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "estimated_base_amount",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "estimated_extra_drop_amount",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "estimated_fuel_amount",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "loss_reason",
            table: "bookings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "pickup_site_id",
            table: "bookings",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "rate_agreement_id",
            table: "bookings",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "reference_number",
            table: "bookings",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "service_date",
            table: "bookings",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "temperature_max_c",
            table: "bookings",
            type: "numeric(5,1)",
            precision: 5,
            scale: 1,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "temperature_min_c",
            table: "bookings",
            type: "numeric(5,1)",
            precision: 5,
            scale: 1,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "truck_size",
            table: "bookings",
            type: "text",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "booking_drops",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                site_id = table.Column<Guid>(type: "uuid", nullable: false),
                planned_boxes = table.Column<int>(type: "integer", nullable: true),
                planned_bags = table.Column<int>(type: "integer", nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_booking_drops", x => x.id);
                table.ForeignKey(
                    name: "fk_booking_drops_bookings_booking_id",
                    column: x => x.booking_id,
                    principalTable: "bookings",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_booking_drops_location_site_id",
                    column: x => x.site_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_pickup_site_id",
            table: "bookings",
            column: "pickup_site_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_rate_agreement_id",
            table: "bookings",
            column: "rate_agreement_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_reference_number",
            table: "bookings",
            column: "reference_number",
            unique: true,
            filter: "reference_number IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_service_date",
            table: "bookings",
            column: "service_date");

        migrationBuilder.CreateIndex(
            name: "ix_booking_drops_booking_id_sort_order",
            table: "booking_drops",
            columns: new[] { "booking_id", "sort_order" });

        migrationBuilder.CreateIndex(
            name: "ix_booking_drops_site_id",
            table: "booking_drops",
            column: "site_id");

        migrationBuilder.AddForeignKey(
            name: "fk_bookings_location_pickup_site_id",
            table: "bookings",
            column: "pickup_site_id",
            principalTable: "locations",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_bookings_rate_agreement_rate_agreement_id",
            table: "bookings",
            column: "rate_agreement_id",
            principalTable: "rate_agreements",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_bookings_location_pickup_site_id",
            table: "bookings");

        migrationBuilder.DropForeignKey(
            name: "fk_bookings_rate_agreement_rate_agreement_id",
            table: "bookings");

        migrationBuilder.DropTable(
            name: "booking_drops");

        migrationBuilder.DropIndex(
            name: "ix_bookings_pickup_site_id",
            table: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_bookings_rate_agreement_id",
            table: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_bookings_reference_number",
            table: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_bookings_service_date",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "call_time",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "cancelled_by_party",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "commodity",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "estimate_note",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "estimated_base_amount",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "estimated_extra_drop_amount",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "estimated_fuel_amount",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "loss_reason",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "pickup_site_id",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "rate_agreement_id",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "reference_number",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "service_date",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "temperature_max_c",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "temperature_min_c",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "truck_size",
            table: "bookings");

        migrationBuilder.AlterColumn<Guid>(
            name: "quotation_id",
            table: "bookings",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
