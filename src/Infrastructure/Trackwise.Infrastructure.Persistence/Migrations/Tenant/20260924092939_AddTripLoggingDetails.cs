using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddTripLoggingDetails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "garage_arrived_at",
            table: "trips",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "garage_departed_at",
            table: "trips",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "handling_finished_at",
            table: "trip_stops",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "handling_started_at",
            table: "trip_stops",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "commodity",
            table: "loads",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "outcome_note",
            table: "loads",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "planned_bags",
            table: "loads",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "planned_boxes",
            table: "loads",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "return_reason",
            table: "loads",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "returned_bags",
            table: "loads",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "returned_boxes",
            table: "loads",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "garage_arrived_at",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "garage_departed_at",
            table: "trips");

        migrationBuilder.DropColumn(
            name: "handling_finished_at",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "handling_started_at",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "commodity",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "outcome_note",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "planned_bags",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "planned_boxes",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "return_reason",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "returned_bags",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "returned_boxes",
            table: "loads");
    }
}
