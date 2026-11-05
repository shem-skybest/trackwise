using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddCrewAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crew_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    truck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_status = table.Column<string>(type: "text", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                    second_helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crew_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_crew_assignments_employee_driver_id",
                        column: x => x.driver_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_crew_assignments_helper_helper_id",
                        column: x => x.helper_id,
                        principalTable: "helpers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_crew_assignments_helper_second_helper_id",
                        column: x => x.second_helper_id,
                        principalTable: "helpers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_crew_assignments_truck_truck_id",
                        column: x => x.truck_id,
                        principalTable: "trucks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crew_assignments_date_truck_id",
                table: "crew_assignments",
                columns: new[] { "date", "truck_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_crew_assignments_driver_id",
                table: "crew_assignments",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_crew_assignments_helper_id",
                table: "crew_assignments",
                column: "helper_id");

            migrationBuilder.CreateIndex(
                name: "ix_crew_assignments_second_helper_id",
                table: "crew_assignments",
                column: "second_helper_id");

            migrationBuilder.CreateIndex(
                name: "ix_crew_assignments_truck_id",
                table: "crew_assignments",
                column: "truck_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crew_assignments");
        }
    }
}
