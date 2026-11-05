using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddPhilippineLicenseCodesAndTruckGvw : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "gross_vehicle_weight_kg",
            table: "trucks",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "dl_codes",
            table: "driver_licenses",
            type: "text",
            nullable: false,
            defaultValue: "none");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "gross_vehicle_weight_kg",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "dl_codes",
            table: "driver_licenses");
    }
}
