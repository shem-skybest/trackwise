using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddIncidentsAndTripRescues : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_documents_accident_reports_accident_report_id",
            table: "documents");

        migrationBuilder.DropIndex(
            name: "ix_documents_accident_report_id",
            table: "documents");

        migrationBuilder.DropColumn(
            name: "accident_report_id",
            table: "documents");

        migrationBuilder.AddColumn<Guid>(
            name: "rescue_id",
            table: "trip_stops",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "incident_id",
            table: "maintenance_records",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "incidents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                kind = table.Column<string>(type: "text", nullable: false),
                severity = table.Column<string>(type: "text", nullable: false),
                accident_type = table.Column<string>(type: "text", nullable: true),
                occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                place = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                latitude = table.Column<double>(type: "double precision", nullable: true),
                longitude = table.Column<double>(type: "double precision", nullable: true),
                trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                truck_id = table.Column<Guid>(type: "uuid", nullable: true),
                driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                second_helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                any_injuries = table.Column<bool>(type: "boolean", nullable: false),
                number_of_injuries = table.Column<int>(type: "integer", nullable: true),
                injury_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                vehicle_damaged = table.Column<bool>(type: "boolean", nullable: false),
                vehicle_damage_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                vehicle_drivable = table.Column<bool>(type: "boolean", nullable: false),
                estimated_damage_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                police_report_filed = table.Column<bool>(type: "boolean", nullable: false),
                police_report_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                police_station = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                barangay_blotter_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                insurance_claim_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                insurance_notified_on = table.Column<DateOnly>(type: "date", nullable: true),
                third_party_compensation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                crew_charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                crew_charge_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                corrective_action = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                resolved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                reopen_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                closed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_incidents", x => x.id);
                table.ForeignKey(
                    name: "fk_incidents_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_incidents_employees_driver_id",
                    column: x => x.driver_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_incidents_helper_helper_id",
                    column: x => x.helper_id,
                    principalTable: "helpers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_incidents_helper_second_helper_id",
                    column: x => x.second_helper_id,
                    principalTable: "helpers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_incidents_trip_trip_id",
                    column: x => x.trip_id,
                    principalTable: "trips",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_incidents_truck_truck_id",
                    column: x => x.truck_id,
                    principalTable: "trucks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "incident_third_parties",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                driver_license = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                vehicle_make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                vehicle_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                vehicle_year = table.Column<int>(type: "integer", nullable: true),
                vehicle_license_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                vehicle_vin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                vehicle_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                insurance_company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                insurance_policy_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                insurance_agent_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_incident_third_parties", x => x.id);
                table.ForeignKey(
                    name: "fk_incident_third_parties_incidents_incident_id",
                    column: x => x.incident_id,
                    principalTable: "incidents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "incident_witnesses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                statement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_incident_witnesses", x => x.id);
                table.ForeignKey(
                    name: "fk_incident_witnesses_incidents_incident_id",
                    column: x => x.incident_id,
                    principalTable: "incidents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Accident reports become incidents of kind Accident: the old seven statuses fold into four,
        // a (0, 0) pin was the old "no pin", and US-only fields (officer badge, signature, weather)
        // are dropped.
        migrationBuilder.Sql("""
            INSERT INTO incidents (
                id, kind, severity, accident_type, occurred_at, summary, place, latitude, longitude,
                trip_id, truck_id, driver_id, any_injuries, number_of_injuries, injury_description,
                vehicle_damaged, vehicle_damage_description, vehicle_drivable, estimated_damage_cost,
                police_report_filed, police_report_number, police_station, insurance_claim_number,
                insurance_notified_on, status, reviewed_at, review_notes,
                created_at, created_by, updated_at, updated_by)
            SELECT
                id, 'accident', severity, accident_type, accident_date_time,
                COALESCE(NULLIF(TRIM(description), ''), '(no description)'), address,
                CASE WHEN latitude = 0 AND longitude = 0 THEN NULL ELSE latitude END,
                CASE WHEN latitude = 0 AND longitude = 0 THEN NULL ELSE longitude END,
                trip_id, truck_id, driver_id, any_injuries,
                CASE WHEN any_injuries THEN GREATEST(COALESCE(number_of_injuries, 1), 1) END, injury_description,
                vehicle_damaged, vehicle_damage_description, vehicle_drivable, estimated_damage_cost,
                police_report_filed, police_report_number, police_department, insurance_claim_number,
                insurance_notified_at::date,
                CASE status
                    WHEN 'resolved' THEN 'resolved'
                    WHEN 'closed' THEN 'closed'
                    WHEN 'under_review' THEN 'under_review'
                    WHEN 'pending_documentation' THEN 'under_review'
                    WHEN 'insurance_filed' THEN 'under_review'
                    ELSE 'open'
                END,
                reviewed_at, review_notes, created_at, created_by, updated_at, updated_by
            FROM accident_reports;

            INSERT INTO incident_witnesses (id, incident_id, name, phone_number, email, address, statement)
            SELECT id, accident_report_id, name, phone_number, email, address, statement
            FROM accident_witnesses;

            INSERT INTO incident_third_parties (
                id, incident_id, name, phone_number, address, driver_license, vehicle_make, vehicle_model,
                vehicle_year, vehicle_license_plate, vehicle_vin, vehicle_color, insurance_company,
                insurance_policy_number, insurance_agent_phone)
            SELECT
                id, accident_report_id, name, phone_number, address, driver_license, vehicle_make, vehicle_model,
                vehicle_year, vehicle_license_plate, vehicle_vin, vehicle_color, insurance_company,
                insurance_policy_number, insurance_agent_phone
            FROM accident_third_parties;
            """);

        migrationBuilder.DropTable(
            name: "accident_third_parties");

        migrationBuilder.DropTable(
            name: "accident_witnesses");

        migrationBuilder.DropTable(
            name: "accident_reports");

        migrationBuilder.CreateTable(
            name: "trip_rescues",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                truck_id = table.Column<Guid>(type: "uuid", nullable: false),
                driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                second_helper_id = table.Column<Guid>(type: "uuid", nullable: true),
                transferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                place = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                incident_id = table.Column<Guid>(type: "uuid", nullable: true),
                recorded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_trip_rescues", x => x.id);
                table.ForeignKey(
                    name: "fk_trip_rescues_employees_driver_id",
                    column: x => x.driver_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_trip_rescues_helpers_helper_id",
                    column: x => x.helper_id,
                    principalTable: "helpers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_trip_rescues_helpers_second_helper_id",
                    column: x => x.second_helper_id,
                    principalTable: "helpers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_trip_rescues_incidents_incident_id",
                    column: x => x.incident_id,
                    principalTable: "incidents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_trip_rescues_trips_trip_id",
                    column: x => x.trip_id,
                    principalTable: "trips",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_trip_rescues_truck_truck_id",
                    column: x => x.truck_id,
                    principalTable: "trucks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_trip_stops_rescue_id",
            table: "trip_stops",
            column: "rescue_id");

        migrationBuilder.CreateIndex(
            name: "ix_maintenance_records_incident_id",
            table: "maintenance_records",
            column: "incident_id");

        migrationBuilder.CreateIndex(
            name: "ix_incident_third_parties_incident_id",
            table: "incident_third_parties",
            column: "incident_id");

        migrationBuilder.CreateIndex(
            name: "ix_incident_witnesses_incident_id",
            table: "incident_witnesses",
            column: "incident_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_customer_id",
            table: "incidents",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_driver_id_occurred_at",
            table: "incidents",
            columns: new[] { "driver_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_incidents_helper_id",
            table: "incidents",
            column: "helper_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_number",
            table: "incidents",
            column: "number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_incidents_occurred_at",
            table: "incidents",
            column: "occurred_at");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_second_helper_id",
            table: "incidents",
            column: "second_helper_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_status",
            table: "incidents",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_trip_id",
            table: "incidents",
            column: "trip_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_truck_id_occurred_at",
            table: "incidents",
            columns: new[] { "truck_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_driver_id",
            table: "trip_rescues",
            column: "driver_id");

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_helper_id",
            table: "trip_rescues",
            column: "helper_id");

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_incident_id",
            table: "trip_rescues",
            column: "incident_id");

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_second_helper_id",
            table: "trip_rescues",
            column: "second_helper_id");

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_trip_id",
            table: "trip_rescues",
            column: "trip_id");

        migrationBuilder.CreateIndex(
            name: "ix_trip_rescues_truck_id",
            table: "trip_rescues",
            column: "truck_id");

        migrationBuilder.AddForeignKey(
            name: "fk_maintenance_records_incidents_incident_id",
            table: "maintenance_records",
            column: "incident_id",
            principalTable: "incidents",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_trip_stops_trip_rescues_rescue_id",
            table: "trip_stops",
            column: "rescue_id",
            principalTable: "trip_rescues",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_maintenance_records_incidents_incident_id",
            table: "maintenance_records");

        migrationBuilder.DropForeignKey(
            name: "fk_trip_stops_trip_rescues_rescue_id",
            table: "trip_stops");

        migrationBuilder.DropTable(
            name: "incident_third_parties");

        migrationBuilder.DropTable(
            name: "incident_witnesses");

        migrationBuilder.DropTable(
            name: "trip_rescues");

        migrationBuilder.DropTable(
            name: "incidents");

        migrationBuilder.DropIndex(
            name: "ix_trip_stops_rescue_id",
            table: "trip_stops");

        migrationBuilder.DropIndex(
            name: "ix_maintenance_records_incident_id",
            table: "maintenance_records");

        migrationBuilder.DropColumn(
            name: "rescue_id",
            table: "trip_stops");

        migrationBuilder.DropColumn(
            name: "incident_id",
            table: "maintenance_records");

        migrationBuilder.AddColumn<Guid>(
            name: "accident_report_id",
            table: "documents",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "accident_reports",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                truck_id = table.Column<Guid>(type: "uuid", nullable: false),
                accident_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                accident_type = table.Column<string>(type: "text", nullable: false),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                any_injuries = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                driver_signature = table.Column<string>(type: "text", nullable: true),
                driver_signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                driver_statement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                estimated_damage_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                injury_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                insurance_claim_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                insurance_notified = table.Column<bool>(type: "boolean", nullable: false),
                insurance_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                latitude = table.Column<double>(type: "double precision", nullable: false),
                longitude = table.Column<double>(type: "double precision", nullable: false),
                number_of_injuries = table.Column<int>(type: "integer", nullable: true),
                police_department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                police_officer_badge = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                police_officer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                police_report_filed = table.Column<bool>(type: "boolean", nullable: false),
                police_report_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                road_conditions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                severity = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                vehicle_damage_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                vehicle_damaged = table.Column<bool>(type: "boolean", nullable: false),
                vehicle_drivable = table.Column<bool>(type: "boolean", nullable: false),
                weather_conditions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_accident_reports", x => x.id);
                table.ForeignKey(
                    name: "fk_accident_reports_employee_driver_id",
                    column: x => x.driver_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_accident_reports_employee_reviewed_by_id",
                    column: x => x.reviewed_by_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_accident_reports_trip_trip_id",
                    column: x => x.trip_id,
                    principalTable: "trips",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_accident_reports_truck_truck_id",
                    column: x => x.truck_id,
                    principalTable: "trucks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "accident_third_parties",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                accident_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                driver_license = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                insurance_agent_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                insurance_company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                insurance_policy_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                vehicle_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                vehicle_license_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                vehicle_make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                vehicle_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                vehicle_vin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                vehicle_year = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_accident_third_parties", x => x.id);
                table.ForeignKey(
                    name: "fk_accident_third_parties_accident_reports_accident_report_id",
                    column: x => x.accident_report_id,
                    principalTable: "accident_reports",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "accident_witnesses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                accident_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                statement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_accident_witnesses", x => x.id);
                table.ForeignKey(
                    name: "fk_accident_witnesses_accident_reports_accident_report_id",
                    column: x => x.accident_report_id,
                    principalTable: "accident_reports",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_documents_accident_report_id",
            table: "documents",
            column: "accident_report_id");

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_driver_id_accident_date_time",
            table: "accident_reports",
            columns: new[] { "driver_id", "accident_date_time" });

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_reviewed_by_id",
            table: "accident_reports",
            column: "reviewed_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_severity",
            table: "accident_reports",
            column: "severity");

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_status",
            table: "accident_reports",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_trip_id",
            table: "accident_reports",
            column: "trip_id");

        migrationBuilder.CreateIndex(
            name: "ix_accident_reports_truck_id",
            table: "accident_reports",
            column: "truck_id");

        migrationBuilder.CreateIndex(
            name: "ix_accident_third_parties_accident_report_id",
            table: "accident_third_parties",
            column: "accident_report_id");

        migrationBuilder.CreateIndex(
            name: "ix_accident_witnesses_accident_report_id",
            table: "accident_witnesses",
            column: "accident_report_id");

        migrationBuilder.AddForeignKey(
            name: "fk_documents_accident_reports_accident_report_id",
            table: "documents",
            column: "accident_report_id",
            principalTable: "accident_reports",
            principalColumn: "id");
    }
}
