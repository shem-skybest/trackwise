using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBookingBillingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices_loads_load_id",
                table: "invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "load_invoice_customer_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "booking_charge_id",
                table: "invoice_line_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_booking_id",
                table: "invoices",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_customer_id1",
                table: "invoices",
                column: "load_invoice_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_line_items_booking_charge_id",
                table: "invoice_line_items",
                column: "booking_charge_id");

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_bookings_booking_id",
                table: "invoices",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_customers_customer_id1",
                table: "invoices",
                column: "load_invoice_customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_loads_load_id",
                table: "invoices",
                column: "load_id",
                principalTable: "loads",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices_bookings_booking_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_invoices_customers_customer_id1",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_invoices_loads_load_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_booking_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_customer_id1",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoice_line_items_booking_charge_id",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "load_invoice_customer_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "booking_charge_id",
                table: "invoice_line_items");

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_loads_load_id",
                table: "invoices",
                column: "load_id",
                principalTable: "loads",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
