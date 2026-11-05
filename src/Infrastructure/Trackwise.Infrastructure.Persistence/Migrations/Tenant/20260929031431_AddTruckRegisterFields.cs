using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddTruckRegisterFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "acquired_on",
            table: "trucks",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "asset_code",
            table: "trucks",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "conduction_sticker",
            table: "trucks",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "engine_number",
            table: "trucks",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ownership",
            table: "trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "supplier",
            table: "trucks",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "acquired_on",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "asset_code",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "conduction_sticker",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "engine_number",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "ownership",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "supplier",
            table: "trucks");
    }
}
