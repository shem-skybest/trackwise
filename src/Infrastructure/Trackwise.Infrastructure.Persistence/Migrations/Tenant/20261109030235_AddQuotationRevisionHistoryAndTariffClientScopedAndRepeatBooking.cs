using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddQuotationRevisionHistoryAndTariffClientScopedAndRepeatBooking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_tariffs_name_mode_service_level_contract_id_is_active",
            table: "tariffs");

        migrationBuilder.DropIndex(
            name: "ix_bookings_quotation_id",
            table: "bookings");

        migrationBuilder.AddColumn<Guid>(
            name: "customer_id",
            table: "tariffs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "previous_revision_id",
            table: "quotations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "revision_group_id",
            table: "quotations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<int>(
            name: "revision_number",
            table: "quotations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "quotation_reference",
            table: "bookings",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50);

        migrationBuilder.AlterColumn<Guid>(
            name: "quotation_id",
            table: "bookings",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.CreateIndex(
            name: "ix_tariffs_customer_id",
            table: "tariffs",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ix_tariffs_name_mode_service_level_contract_id_customer_id_is_",
            table: "tariffs",
            columns: new[] { "name", "mode", "service_level", "contract_id", "customer_id", "is_active" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_quotations_previous_revision_id",
            table: "quotations",
            column: "previous_revision_id");

        migrationBuilder.CreateIndex(
            name: "ix_quotations_revision_group_id_revision_number",
            table: "quotations",
            columns: new[] { "revision_group_id", "revision_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bookings_booking_number",
            table: "bookings",
            column: "booking_number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bookings_quotation_id",
            table: "bookings",
            column: "quotation_id",
            filter: "\"quotation_id\" IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "fk_quotations_quotations_previous_revision_id",
            table: "quotations",
            column: "previous_revision_id",
            principalTable: "quotations",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_tariffs_customers_customer_id",
            table: "tariffs",
            column: "customer_id",
            principalTable: "customers",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_quotations_quotations_previous_revision_id",
            table: "quotations");

        migrationBuilder.DropForeignKey(
            name: "fk_tariffs_customers_customer_id",
            table: "tariffs");

        migrationBuilder.DropIndex(
            name: "ix_tariffs_customer_id",
            table: "tariffs");

        migrationBuilder.DropIndex(
            name: "ix_tariffs_name_mode_service_level_contract_id_customer_id_is_",
            table: "tariffs");

        migrationBuilder.DropIndex(
            name: "ix_quotations_previous_revision_id",
            table: "quotations");

        migrationBuilder.DropIndex(
            name: "ix_quotations_revision_group_id_revision_number",
            table: "quotations");

        migrationBuilder.DropIndex(
            name: "ix_bookings_booking_number",
            table: "bookings");

        migrationBuilder.DropIndex(
            name: "ix_bookings_quotation_id",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "customer_id",
            table: "tariffs");

        migrationBuilder.DropColumn(
            name: "previous_revision_id",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "revision_group_id",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "revision_number",
            table: "quotations");

        migrationBuilder.AlterColumn<string>(
            name: "quotation_reference",
            table: "bookings",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "",
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

        migrationBuilder.CreateIndex(
            name: "ix_tariffs_name_mode_service_level_contract_id_is_active",
            table: "tariffs",
            columns: new[] { "name", "mode", "service_level", "contract_id", "is_active" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_bookings_quotation_id",
            table: "bookings",
            column: "quotation_id",
            unique: true);
    }
}
