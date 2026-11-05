using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddRepairRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "odometer_reading",
                table: "maintenance_records",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "requisition_number",
                table: "maintenance_records",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_system",
                table: "maintenance_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "maintenance_parts",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_records_requisition_number",
                table: "maintenance_records",
                column: "requisition_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_maintenance_records_requisition_number",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "requisition_number",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "vehicle_system",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "description",
                table: "maintenance_parts");

            migrationBuilder.AlterColumn<int>(
                name: "odometer_reading",
                table: "maintenance_records",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
