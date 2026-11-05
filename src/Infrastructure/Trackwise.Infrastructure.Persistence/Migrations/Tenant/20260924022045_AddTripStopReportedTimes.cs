using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTripStopReportedTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "arrival_recorded_at",
                table: "trip_stops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "arrival_recorded_by",
                table: "trip_stops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "departure_recorded_at",
                table: "trip_stops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "departure_recorded_by",
                table: "trip_stops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arrival_recorded_at",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "arrival_recorded_by",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "departure_recorded_at",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "departure_recorded_by",
                table: "trip_stops");
        }
    }
}
