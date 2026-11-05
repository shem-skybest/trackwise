using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddWorkfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workfile_number_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_allocated = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workfile_number_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workfiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    bill_of_lading_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_indicator = table.Column<string>(type: "text", nullable: true),
                    vessel_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    voyage_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    etd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    origin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    commodity = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    co_loader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forwarder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trucker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    broker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_term_days = table.Column<int>(type: "integer", nullable: true),
                    credit_type = table.Column<string>(type: "text", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    final_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    tagging_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_advance_billing = table.Column<bool>(type: "boolean", nullable: false),
                    report_category = table.Column<string>(type: "text", nullable: false),
                    garage_departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    garage_arrival_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_forwarded_to_accounting = table.Column<bool>(type: "boolean", nullable: false),
                    forwarded_to_accounting_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accounting_remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_reference_locked = table.Column<bool>(type: "boolean", nullable: false),
                    closed_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    direction = table.Column<string>(type: "text", nullable: true),
                    variant = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workfiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_workfiles_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workfiles_business_partners_broker_id",
                        column: x => x.broker_id,
                        principalTable: "business_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workfiles_business_partners_co_loader_id",
                        column: x => x.co_loader_id,
                        principalTable: "business_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workfiles_business_partners_forwarder_id",
                        column: x => x.forwarder_id,
                        principalTable: "business_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workfiles_business_partners_shipping_line_id",
                        column: x => x.shipping_line_id,
                        principalTable: "business_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workfiles_business_partners_trucker_id",
                        column: x => x.trucker_id,
                        principalTable: "business_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workfile_number_series_series",
                table: "workfile_number_series",
                column: "series",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_bill_of_lading_number",
                table: "workfiles",
                column: "bill_of_lading_number");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_booking_id",
                table: "workfiles",
                column: "booking_id",
                unique: true,
                filter: "booking_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_broker_id",
                table: "workfiles",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_co_loader_id",
                table: "workfiles",
                column: "co_loader_id");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_customer_id_status",
                table: "workfiles",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_forwarder_id",
                table: "workfiles",
                column: "forwarder_id");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_reference_number",
                table: "workfiles",
                column: "reference_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_shipping_line_id",
                table: "workfiles",
                column: "shipping_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_trucker_id",
                table: "workfiles",
                column: "trucker_id");

            migrationBuilder.CreateIndex(
                name: "ix_workfiles_type",
                table: "workfiles",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workfile_number_series");

            migrationBuilder.DropTable(
                name: "workfiles");
        }
    }
}
