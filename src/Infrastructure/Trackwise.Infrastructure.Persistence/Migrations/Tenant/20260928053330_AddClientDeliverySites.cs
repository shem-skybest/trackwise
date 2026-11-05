using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddClientDeliverySites : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "customer_id",
            table: "locations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<TimeOnly>(
            name: "default_cutoff_time",
            table: "locations",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "zone_radius_meters",
            table: "locations",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "destination_site_id",
            table: "loads",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "origin_site_id",
            table: "loads",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_locations_customer_id",
            table: "locations",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ix_loads_destination_site_id",
            table: "loads",
            column: "destination_site_id");

        migrationBuilder.CreateIndex(
            name: "ix_loads_origin_site_id",
            table: "loads",
            column: "origin_site_id");

        migrationBuilder.AddForeignKey(
            name: "fk_loads_location_destination_site_id",
            table: "loads",
            column: "destination_site_id",
            principalTable: "locations",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_loads_location_origin_site_id",
            table: "loads",
            column: "origin_site_id",
            principalTable: "locations",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_locations_customers_customer_id",
            table: "locations",
            column: "customer_id",
            principalTable: "customers",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_loads_location_destination_site_id",
            table: "loads");

        migrationBuilder.DropForeignKey(
            name: "fk_loads_location_origin_site_id",
            table: "loads");

        migrationBuilder.DropForeignKey(
            name: "fk_locations_customers_customer_id",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "ix_locations_customer_id",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "ix_loads_destination_site_id",
            table: "loads");

        migrationBuilder.DropIndex(
            name: "ix_loads_origin_site_id",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "customer_id",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "default_cutoff_time",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "zone_radius_meters",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "destination_site_id",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "origin_site_id",
            table: "loads");
    }
}
