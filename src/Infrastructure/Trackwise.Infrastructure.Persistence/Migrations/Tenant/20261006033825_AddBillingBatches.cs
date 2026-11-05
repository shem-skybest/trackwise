using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBillingBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_cycle",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "billing_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    trip_count = table.Column<int>(type: "integer", nullable: false),
                    drop_count = table.Column<int>(type: "integer", nullable: false),
                    vatable_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    non_vat_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unlock_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unlocked_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    forwarded_on = table.Column<DateOnly>(type: "date", nullable: true),
                    forwarded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    billed_on = table.Column<DateOnly>(type: "date", nullable: true),
                    billed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_billing_batches_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "billing_batch_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_fund_expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_batch_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_billing_batch_charges_billing_batch_billing_batch_id",
                        column: x => x.billing_batch_id,
                        principalTable: "billing_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_billing_batch_charges_cash_fund_expense_cash_fund_expense_id",
                        column: x => x.cash_fund_expense_id,
                        principalTable: "cash_fund_expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_billing_batch_charges_trip_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "billing_batch_trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_number = table.Column<long>(type: "bigint", nullable: false),
                    trip_status = table.Column<string>(type: "text", nullable: false),
                    billing_date = table.Column<DateOnly>(type: "date", nullable: false),
                    truck_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    booking_references = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    drops = table.Column<int>(type: "integer", nullable: false),
                    foul_trip_drops = table.Column<int>(type: "integer", nullable: false),
                    cancelled_drops = table.Column<int>(type: "integer", nullable: false),
                    partial_drops = table.Column<int>(type: "integer", nullable: false),
                    planned_boxes = table.Column<int>(type: "integer", nullable: true),
                    rate_line_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    base_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    fuel_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    extra_drop_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_price_outdated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_batch_trips", x => x.id);
                    table.ForeignKey(
                        name: "fk_billing_batch_trips_billing_batches_billing_batch_id",
                        column: x => x.billing_batch_id,
                        principalTable: "billing_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_billing_batch_trips_trip_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_batch_charges_billing_batch_id",
                table: "billing_batch_charges",
                column: "billing_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_batch_charges_cash_fund_expense_id",
                table: "billing_batch_charges",
                column: "cash_fund_expense_id",
                unique: true,
                filter: "cash_fund_expense_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_batch_charges_trip_id",
                table: "billing_batch_charges",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_batch_trips_billing_batch_id",
                table: "billing_batch_trips",
                column: "billing_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_batch_trips_trip_id",
                table: "billing_batch_trips",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_batches_customer_id_period_start",
                table: "billing_batches",
                columns: new[] { "customer_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_batches_reference_number",
                table: "billing_batches",
                column: "reference_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_batches_status",
                table: "billing_batches",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_batch_charges");

            migrationBuilder.DropTable(
                name: "billing_batch_trips");

            migrationBuilder.DropTable(
                name: "billing_batches");

            migrationBuilder.DropColumn(
                name: "billing_cycle",
                table: "customers");
        }
    }
}
