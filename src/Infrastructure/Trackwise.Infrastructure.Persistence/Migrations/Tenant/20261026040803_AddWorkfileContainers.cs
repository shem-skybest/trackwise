using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddWorkfileContainers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // These were facts of one move typed onto the box. No workfile link existed to move them
        // to, so keep whatever was typed in the box's notes rather than dropping it.
        migrationBuilder.Sql("""
            UPDATE containers
            SET notes = left(concat_ws(E'\n', nullif(notes, ''),
                'Seal: ' || nullif(seal_number, ''),
                'Booking: ' || nullif(booking_reference, ''),
                'B/L: ' || nullif(bill_of_lading_number, ''),
                'Gross weight: ' || nullif(gross_weight, 0) || ' kg'), 2000)
            WHERE coalesce(seal_number, '') <> ''
               OR coalesce(booking_reference, '') <> ''
               OR coalesce(bill_of_lading_number, '') <> ''
               OR gross_weight <> 0;
            """);

        migrationBuilder.DropColumn(
            name: "bill_of_lading_number",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "booking_reference",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "gross_weight",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "seal_number",
            table: "containers");

        migrationBuilder.CreateTable(
            name: "workfile_containers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                workfile_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                container_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                container_id = table.Column<Guid>(type: "uuid", nullable: true),
                seal_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                gross_weight_kg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                package_count = table.Column<int>(type: "integer", nullable: true),
                packaging_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                cbm = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_workfile_containers", x => x.id);
                table.ForeignKey(
                    name: "fk_workfile_containers_container_type_container_type_id",
                    column: x => x.container_type_id,
                    principalTable: "container_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_workfile_containers_containers_container_id",
                    column: x => x.container_id,
                    principalTable: "containers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_workfile_containers_workfiles_workfile_id",
                    column: x => x.workfile_id,
                    principalTable: "workfiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_workfile_containers_container_id",
            table: "workfile_containers",
            column: "container_id");

        migrationBuilder.CreateIndex(
            name: "ix_workfile_containers_container_type_id",
            table: "workfile_containers",
            column: "container_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_workfile_containers_workfile_id_sort_order",
            table: "workfile_containers",
            columns: new[] { "workfile_id", "sort_order" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "workfile_containers");

        migrationBuilder.AddColumn<string>(
            name: "bill_of_lading_number",
            table: "containers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "booking_reference",
            table: "containers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "gross_weight",
            table: "containers",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "seal_number",
            table: "containers",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);
    }
}
