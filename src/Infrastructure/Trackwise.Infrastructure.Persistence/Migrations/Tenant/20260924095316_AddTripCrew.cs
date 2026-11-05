using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTripCrew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "driver_id",
                table: "trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "second_helper_id",
                table: "trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_driver_id",
                table: "trips",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_second_helper_id",
                table: "trips",
                column: "second_helper_id");

            migrationBuilder.AddForeignKey(
                name: "fk_trips_employees_driver_id",
                table: "trips",
                column: "driver_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_trips_helpers_second_helper_id",
                table: "trips",
                column: "second_helper_id",
                principalTable: "helpers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Existing trips read "who drove" through the truck's main driver; freeze that onto the
            // trip so history keeps showing the same driver it did before the column existed.
            migrationBuilder.Sql(
                "UPDATE trips SET driver_id = trucks.main_driver_id FROM trucks " +
                "WHERE trips.truck_id = trucks.id AND trips.driver_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_trips_employees_driver_id",
                table: "trips");

            migrationBuilder.DropForeignKey(
                name: "fk_trips_helpers_second_helper_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_driver_id",
                table: "trips");

            migrationBuilder.DropIndex(
                name: "ix_trips_second_helper_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "driver_id",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "second_helper_id",
                table: "trips");
        }
    }
}
