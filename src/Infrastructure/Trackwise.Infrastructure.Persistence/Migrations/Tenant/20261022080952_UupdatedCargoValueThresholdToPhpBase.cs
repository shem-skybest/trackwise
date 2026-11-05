using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class UupdatedCargoValueThresholdToPhpBase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "declared_cargo_value_usd",
            table: "quotations",
            newName: "declared_cargo_value_php");

        migrationBuilder.AddForeignKey(
            name: "fk_quotations_customers_customer_id",
            table: "quotations",
            column: "customer_id",
            principalTable: "customers",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_quotations_customers_customer_id",
            table: "quotations");

        migrationBuilder.RenameColumn(
            name: "declared_cargo_value_php",
            table: "quotations",
            newName: "declared_cargo_value_usd");
    }
}
