using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddVehiclePermits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "vehicle_permits",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                truck_id = table.Column<Guid>(type: "uuid", nullable: true),
                trailer_id = table.Column<Guid>(type: "uuid", nullable: true),
                type = table.Column<string>(type: "text", nullable: false),
                number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                issuer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                expires_on = table.Column<DateOnly>(type: "date", nullable: false),
                cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                last_reminder_threshold_days = table.Column<int>(type: "integer", nullable: true),
                last_reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_vehicle_permits", x => x.id);
                table.CheckConstraint("ck_vehicle_permits_one_vehicle", "(truck_id IS NULL) <> (trailer_id IS NULL)");
                table.ForeignKey(
                    name: "fk_vehicle_permits_trailers_trailer_id",
                    column: x => x.trailer_id,
                    principalTable: "trailers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_vehicle_permits_trucks_truck_id",
                    column: x => x.truck_id,
                    principalTable: "trucks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_vehicle_permits_expires_on",
            table: "vehicle_permits",
            column: "expires_on");

        migrationBuilder.CreateIndex(
            name: "ix_vehicle_permits_trailer_id_type",
            table: "vehicle_permits",
            columns: new[] { "trailer_id", "type" });

        migrationBuilder.CreateIndex(
            name: "ix_vehicle_permits_truck_id_type",
            table: "vehicle_permits",
            columns: new[] { "truck_id", "type" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "vehicle_permits");
    }
}
