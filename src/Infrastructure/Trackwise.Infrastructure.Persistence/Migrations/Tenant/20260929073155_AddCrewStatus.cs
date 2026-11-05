using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddCrewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employment_type",
                table: "helpers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "home_unit",
                table: "helpers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_type",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "home_unit",
                table: "employees",
                type: "text",
                nullable: true);

            // CrewSeeder used to write these into the helper's notes as one fixed sentence, e.g.
            // "On-call Cold Chain helper (from Monday dispatch boards)." Move them into the columns
            // and drop the sentence, keeping anything after it ("Last name unknown - HR to complete.").
            migrationBuilder.Sql(
                """
                UPDATE helpers SET
                    employment_type = CASE WHEN notes LIKE 'On-call %' THEN 'OnCall' ELSE 'Regular' END,
                    home_unit = CASE
                        WHEN notes LIKE '% wing van helper %' THEN 'WingVan'
                        WHEN notes LIKE '% trucking helper %' THEN 'Trucking'
                        ELSE 'ColdChain' END,
                    notes = NULLIF(TRIM(REGEXP_REPLACE(notes,
                        '^(On-call|Regular) (Cold Chain|wing van|trucking) helper \(from Monday dispatch boards\)\.', '')), '')
                WHERE notes ~ '^(On-call|Regular) (Cold Chain|wing van|trucking) helper \(from Monday dispatch boards\)\.';
                """);

            migrationBuilder.CreateTable(
                name: "crew_status_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crew_status_entries", x => x.id);
                    table.CheckConstraint("ck_crew_status_entries_one_person", "(employee_id IS NULL) <> (helper_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_crew_status_entries_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_crew_status_entries_helper_helper_id",
                        column: x => x.helper_id,
                        principalTable: "helpers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crew_status_entries_date_employee_id",
                table: "crew_status_entries",
                columns: new[] { "date", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_crew_status_entries_date_helper_id",
                table: "crew_status_entries",
                columns: new[] { "date", "helper_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_crew_status_entries_employee_id",
                table: "crew_status_entries",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_crew_status_entries_helper_id",
                table: "crew_status_entries",
                column: "helper_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crew_status_entries");

            migrationBuilder.DropColumn(
                name: "employment_type",
                table: "helpers");

            migrationBuilder.DropColumn(
                name: "home_unit",
                table: "helpers");

            migrationBuilder.DropColumn(
                name: "employment_type",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "home_unit",
                table: "employees");
        }
    }
}
