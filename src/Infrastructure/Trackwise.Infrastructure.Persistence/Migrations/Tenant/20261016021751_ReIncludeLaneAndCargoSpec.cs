using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class ReIncludeLaneAndCargoSpec : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Commodity",
            table: "quotations",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "DeclaredCargoValueAmount",
            table: "quotations",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DeclaredCargoValueCurrency",
            table: "quotations",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "PalletCount",
            table: "quotations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "RequiresActiveRefrigeration",
            table: "quotations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "TotalWeightKg",
            table: "quotations",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "lane",
            table: "quotations",
            type: "jsonb",
            nullable: false,
            defaultValue: "{}");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Commodity",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "DeclaredCargoValueAmount",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "DeclaredCargoValueCurrency",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "PalletCount",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "RequiresActiveRefrigeration",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "TotalWeightKg",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "lane",
            table: "quotations");
    }
}
