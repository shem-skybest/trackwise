using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddBookingInvoiceRelationships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_invoices_booking_id",
            table: "invoices");

        migrationBuilder.AddColumn<string>(
            name: "cancellation_reason",
            table: "invoices",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "cancelled_at",
            table: "invoices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "purpose",
            table: "invoices",
            type: "text",
            nullable: true,
            defaultValue: "original");

        migrationBuilder.AddColumn<Guid>(
            name: "related_invoice_id",
            table: "invoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_invoices_booking_id",
            table: "invoices",
            column: "booking_id");

        migrationBuilder.CreateIndex(
            name: "ix_invoices_related_invoice_id",
            table: "invoices",
            column: "related_invoice_id");

        migrationBuilder.CreateIndex(
            name: "ux_invoices_booking_id_draft",
            table: "invoices",
            column: "booking_id",
            unique: true,
            filter: "\"booking_id\" IS NOT NULL AND \"status\" = 'draft'");

        migrationBuilder.AddForeignKey(
            name: "fk_invoices_invoices_related_invoice_id",
            table: "invoices",
            column: "related_invoice_id",
            principalTable: "invoices",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_invoices_invoices_related_invoice_id",
            table: "invoices");

        migrationBuilder.DropIndex(
            name: "ix_invoices_booking_id",
            table: "invoices");

        migrationBuilder.DropIndex(
            name: "ix_invoices_related_invoice_id",
            table: "invoices");

        migrationBuilder.DropIndex(
            name: "ux_invoices_booking_id_draft",
            table: "invoices");

        migrationBuilder.DropColumn(
            name: "cancellation_reason",
            table: "invoices");

        migrationBuilder.DropColumn(
            name: "cancelled_at",
            table: "invoices");

        migrationBuilder.DropColumn(
            name: "purpose",
            table: "invoices");

        migrationBuilder.DropColumn(
            name: "related_invoice_id",
            table: "invoices");

        migrationBuilder.CreateIndex(
            name: "ix_invoices_booking_id",
            table: "invoices",
            column: "booking_id",
            unique: true);
    }
}
