using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class QuotationPricingFixes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "breakpoint_mode",
            table: "tariff_rates",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "declared_cargo_value_usd",
            table: "quotations",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "category",
            table: "quotation_lines",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "quotation_currency",
            table: "quotation_lines",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AlterColumn<string>(
            name: "currency",
            table: "charge_codes",
            type: "character varying(3)",
            maxLength: 3,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "breakpoint_mode",
            table: "tariff_rates");

        migrationBuilder.DropColumn(
            name: "declared_cargo_value_usd",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "category",
            table: "quotation_lines");

        migrationBuilder.DropColumn(
            name: "quotation_currency",
            table: "quotation_lines");

        migrationBuilder.AlterColumn<string>(
            name: "currency",
            table: "charge_codes",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3,
            oldNullable: true);
    }
}
