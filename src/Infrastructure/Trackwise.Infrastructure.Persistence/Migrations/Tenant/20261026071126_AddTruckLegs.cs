using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddTruckLegs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "leg_role",
            table: "workfiles",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "parent_workfile_id",
            table: "workfiles",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "workfile_container_id",
            table: "loads",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "workfile_id",
            table: "loads",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_workfiles_parent_workfile_id",
            table: "workfiles",
            column: "parent_workfile_id");

        migrationBuilder.CreateIndex(
            name: "ix_loads_workfile_container_id",
            table: "loads",
            column: "workfile_container_id");

        migrationBuilder.CreateIndex(
            name: "ix_loads_workfile_id",
            table: "loads",
            column: "workfile_id");

        migrationBuilder.AddForeignKey(
            name: "fk_loads_workfile_containers_workfile_container_id",
            table: "loads",
            column: "workfile_container_id",
            principalTable: "workfile_containers",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_loads_workfiles_workfile_id",
            table: "loads",
            column: "workfile_id",
            principalTable: "workfiles",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_workfiles_workfiles_parent_workfile_id",
            table: "workfiles",
            column: "parent_workfile_id",
            principalTable: "workfiles",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_loads_workfile_containers_workfile_container_id",
            table: "loads");

        migrationBuilder.DropForeignKey(
            name: "fk_loads_workfiles_workfile_id",
            table: "loads");

        migrationBuilder.DropForeignKey(
            name: "fk_workfiles_workfiles_parent_workfile_id",
            table: "workfiles");

        migrationBuilder.DropIndex(
            name: "ix_workfiles_parent_workfile_id",
            table: "workfiles");

        migrationBuilder.DropIndex(
            name: "ix_loads_workfile_container_id",
            table: "loads");

        migrationBuilder.DropIndex(
            name: "ix_loads_workfile_id",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "leg_role",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "parent_workfile_id",
            table: "workfiles");

        migrationBuilder.DropColumn(
            name: "workfile_container_id",
            table: "loads");

        migrationBuilder.DropColumn(
            name: "workfile_id",
            table: "loads");
    }
}
