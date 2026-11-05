using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTripEventSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "garage_arrival_recorded_at",
                table: "trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "garage_arrival_source",
                table: "trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "garage_departure_recorded_at",
                table: "trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "garage_departure_source",
                table: "trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "arrival_source",
                table: "trip_stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "departure_source",
                table: "trip_stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handling_finished_recorded_at",
                table: "trip_stops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handling_finished_source",
                table: "trip_stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handling_started_recorded_at",
                table: "trip_stops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handling_started_source",
                table: "trip_stops",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "garage_arrival_recorded_at",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "garage_arrival_source",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "garage_departure_recorded_at",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "garage_departure_source",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "arrival_source",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "departure_source",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "handling_finished_recorded_at",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "handling_finished_source",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "handling_started_recorded_at",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "handling_started_source",
                table: "trip_stops");
        }
    }
}
