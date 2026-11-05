using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class ExtendedQuotationEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresContinuousTelematics",
            table: "quotations",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TemperatureCompliance",
            table: "quotations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "TemperatureSetpointCelsius",
            table: "quotations",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "TemperatureVarianceCelsius",
            table: "quotations",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TemperatureZone",
            table: "quotations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "accepted_at",
            table: "quotations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "approval_escalation_reason",
            table: "quotations",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "freight_mode",
            table: "quotations",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "incoterm",
            table: "quotations",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "is_hazardous",
            table: "quotations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "issued_at",
            table: "quotations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "mode",
            table: "quotations",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "reference_number",
            table: "quotations",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "requires_customs_brokerage",
            table: "quotations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "requires_special_approval",
            table: "quotations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "shipment_type",
            table: "quotations",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "valid_until",
            table: "quotations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "category",
            table: "quotation_lines",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "currency",
            table: "quotation_lines",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "service_type",
            table: "quotation_lines",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "snapshot_buying_amount",
            table: "quotation_lines",
            type: "numeric",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "snapshot_selling_amount",
            table: "quotation_lines",
            type: "numeric",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_quotations_reference_number",
            table: "quotations",
            column: "reference_number");

        migrationBuilder.CreateIndex(
            name: "ix_quotations_status",
            table: "quotations",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_quotations_reference_number",
            table: "quotations");

        migrationBuilder.DropIndex(
            name: "ix_quotations_status",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "RequiresContinuousTelematics",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "TemperatureCompliance",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "TemperatureSetpointCelsius",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "TemperatureVarianceCelsius",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "TemperatureZone",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "accepted_at",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "approval_escalation_reason",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "freight_mode",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "incoterm",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "is_hazardous",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "issued_at",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "mode",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "reference_number",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "requires_customs_brokerage",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "requires_special_approval",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "shipment_type",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "valid_until",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "category",
            table: "quotation_lines");

        migrationBuilder.DropColumn(
            name: "currency",
            table: "quotation_lines");

        migrationBuilder.DropColumn(
            name: "service_type",
            table: "quotation_lines");

        migrationBuilder.DropColumn(
            name: "snapshot_buying_amount",
            table: "quotation_lines");

        migrationBuilder.DropColumn(
            name: "snapshot_selling_amount",
            table: "quotation_lines");
    }
}
