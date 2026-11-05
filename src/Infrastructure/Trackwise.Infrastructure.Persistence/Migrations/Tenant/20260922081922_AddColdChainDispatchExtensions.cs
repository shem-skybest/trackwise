using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddColdChainDispatchExtensions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "tonnage_class",
            table: "trucks",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "helper_id",
            table: "trips",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "departed_at",
            table: "trip_stops",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "temperature_at_arrival",
            table: "trip_stops",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "temperature_at_departure",
            table: "trip_stops",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_backload",
            table: "loads",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "ix_trips_helper_id",
            table: "trips",
            column: "helper_id");

        migrationBuilder.AddForeignKey(
            name: "fk_trips_helpers_helper_id",
            table: "trips",
            column: "helper_id",
            principalTable: "helpers",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_trips_helpers_helper_id",
            table: "trips");

        migrationBuilder.DropIndex(
            name: "ix_trips_helper_id",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "tonnage_class",
            table: "trucks");

        migrationBuilder.DropColumn(
            name: "helper_id",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "departed_at",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "temperature_at_arrival",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "temperature_at_departure",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "is_backload",
            table: "loads");
    }
}
