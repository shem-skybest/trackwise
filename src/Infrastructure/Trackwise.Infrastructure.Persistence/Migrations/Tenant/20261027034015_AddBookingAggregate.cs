using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBookingAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_container_type_container_type_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings_location_pickup_site_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings_rate_agreement_rate_agreement_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_container_type_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_number",
                table: "bookings");

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
                name: "carrier_reference",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "container_type_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "number",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "requested_delivery_date",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "requested_pickup_date",
                table: "bookings",
                newName: "credit_checked_at");

            migrationBuilder.AlterColumn<decimal>(
                name: "temperature_min_c",
                table: "bookings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,1)",
                oldPrecision: 5,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "temperature_max_c",
                table: "bookings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,1)",
                oldPrecision: 5,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reference_number",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_fuel_amount",
                table: "bookings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_extra_drop_amount",
                table: "bookings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_base_amount",
                table: "bookings",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "estimate_note",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "commodity",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "booking_number",
                table: "bookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cargo_spec",
                table: "bookings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_check_notes",
                table: "bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_check_status",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "credit_checked_by",
                table: "bookings",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_reference",
                table: "bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "declared_cargo_value_php",
                table: "bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_required_percent",
                table: "bookings",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "freight_mode",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "incoterm",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_hazardous",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lane",
                table: "bookings",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "payment_terms_days",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "quotation_id1",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "quotation_reference",
                table: "bookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_customs_brokerage",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "shipment_type",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "temperature_spec",
                table: "bookings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_charge_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    quotation_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tariff_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    service_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    buying_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    selling_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_billable = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    billing_status = table.Column<string>(type: "text", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_charge_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_charge_lines_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_documents_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_credit_check_status",
                table: "bookings",
                column: "credit_check_status");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_quotation_id1",
                table: "bookings",
                column: "quotation_id1");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_booking_charge_lines_booking_id_billing_status",
                table: "booking_charge_lines",
                columns: new[] { "booking_id", "billing_status" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_charge_lines_invoice_id",
                table: "booking_charge_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_charge_lines_quotation_line_id",
                table: "booking_charge_lines",
                column: "quotation_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_documents_booking_id_type",
                table: "booking_documents",
                columns: new[] { "booking_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_documents_expires_at",
                table: "booking_documents",
                column: "expires_at");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_customers_customer_id",
                table: "bookings",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_quotation_quotation_id1",
                table: "bookings",
                column: "quotation_id1",
                principalTable: "quotations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_customers_customer_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_bookings_quotation_quotation_id1",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "booking_charge_lines");

            migrationBuilder.DropTable(
                name: "booking_documents");

            migrationBuilder.DropIndex(
                name: "ix_bookings_credit_check_status",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_quotation_id1",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "booking_number",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cargo_spec",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "credit_check_notes",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "credit_check_status",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "credit_checked_by",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "customer_reference",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "declared_cargo_value_php",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deposit_required_percent",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "freight_mode",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "incoterm",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "is_hazardous",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "lane",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "payment_terms_days",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "quotation_id1",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "quotation_reference",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "requires_customs_brokerage",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "shipment_type",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "temperature_spec",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "credit_checked_at",
                table: "bookings",
                newName: "requested_pickup_date");

            migrationBuilder.AlterColumn<decimal>(
                name: "temperature_min_c",
                table: "bookings",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "temperature_max_c",
                table: "bookings",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reference_number",
                table: "bookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "bookings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_fuel_amount",
                table: "bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_extra_drop_amount",
                table: "bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_base_amount",
                table: "bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "estimate_note",
                table: "bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "commodity",
                table: "bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "carrier_reference",
                table: "bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "container_type_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "number",
                table: "bookings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "requested_delivery_date",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_container_type_id",
                table: "bookings",
                column: "container_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_number",
                table: "bookings",
                column: "number",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_container_type_container_type_id",
                table: "bookings",
                column: "container_type_id",
                principalTable: "container_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

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
    }
}
