using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddJobBasedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_customized",
                table: "tenant_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "tenant_role_change_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    permission = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    previous_role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_role_change_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_role_change_logs_changed_at",
                table: "tenant_role_change_logs",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_role_change_logs_employee_id",
                table: "tenant_role_change_logs",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_role_change_logs");

            migrationBuilder.DropColumn(
                name: "is_customized",
                table: "tenant_roles");
        }
    }
}
