using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddedBranchIdInQuotation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            table: "quotations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "ix_quotations_branch_id",
            table: "quotations",
            column: "branch_id",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_quotations_branch_branch_id",
            table: "quotations",
            column: "branch_id",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_quotations_branch_branch_id",
            table: "quotations");

        migrationBuilder.DropIndex(
            name: "ix_quotations_branch_id",
            table: "quotations");

        migrationBuilder.DropColumn(
            name: "branch_id",
            table: "quotations");
    }
}
