using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class FixQuotationBranchIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_quotations_branch_id",
            table: "quotations");

        migrationBuilder.CreateIndex(
            name: "ix_quotations_branch_id",
            table: "quotations",
            column: "branch_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_quotations_branch_id",
            table: "quotations");

        migrationBuilder.CreateIndex(
            name: "ix_quotations_branch_id",
            table: "quotations",
            column: "branch_id",
            unique: true);
    }
}
