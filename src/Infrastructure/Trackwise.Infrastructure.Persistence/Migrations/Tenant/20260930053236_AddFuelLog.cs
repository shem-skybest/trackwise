using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddFuelLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fleet_card",
                table: "expenses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "trip_id",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expenses_trip_id",
                table: "expenses",
                column: "trip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_trips_trip_id",
                table: "expenses",
                column: "trip_id",
                principalTable: "trips",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_trips_trip_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expenses_trip_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "fleet_card",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "trip_id",
                table: "expenses");
        }
    }
}
