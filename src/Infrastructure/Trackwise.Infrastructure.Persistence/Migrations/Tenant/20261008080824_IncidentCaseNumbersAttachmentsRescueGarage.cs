using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class IncidentCaseNumbersAttachmentsRescueGarage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "garage_arrived_at",
                table: "trip_rescues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "garage_departed_at",
                table: "trip_rescues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "case_number",
                table: "incidents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // Existing incidents keep their running number as CASE-###, and the tenant's Incident
            // series carries on from the highest so new cases don't collide with them.
            migrationBuilder.Sql("""
                UPDATE incidents SET case_number = 'CASE-' || lpad(number::text, 3, '0');

                INSERT INTO number_series (id, series, prefix, separator, pad_width, last_allocated, created_at)
                SELECT gen_random_uuid(), 'Incident', 'CASE', '-', 3, max(number), CURRENT_TIMESTAMP
                FROM incidents
                HAVING count(*) > 0
                ON CONFLICT (series) DO NOTHING;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "case_number",
                table: "incidents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "ix_incidents_number",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "number",
                table: "incidents");

            migrationBuilder.AddColumn<string>(
                name: "prevention",
                table: "incidents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "incident_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_incidents_case_number",
                table: "incidents",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_incident_id",
                table: "documents",
                column: "incident_id");

            migrationBuilder.AddForeignKey(
                name: "fk_documents_incidents_incident_id",
                table: "documents",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_incidents_incident_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_incidents_case_number",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "ix_documents_incident_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "garage_arrived_at",
                table: "trip_rescues");

            migrationBuilder.DropColumn(
                name: "garage_departed_at",
                table: "trip_rescues");

            migrationBuilder.DropColumn(
                name: "case_number",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "prevention",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "incident_id",
                table: "documents");

            migrationBuilder.AddColumn<long>(
                name: "number",
                table: "incidents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.CreateIndex(
                name: "ix_incidents_number",
                table: "incidents",
                column: "number",
                unique: true);
        }
    }
}
