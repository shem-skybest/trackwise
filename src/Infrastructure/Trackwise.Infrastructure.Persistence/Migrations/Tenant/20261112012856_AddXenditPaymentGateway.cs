using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddXenditPaymentGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gateway",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gateway_reference",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_gateway_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway = table.Column<string>(type: "text", nullable: false),
                    secret_key = table.Column<string>(type: "text", nullable: false),
                    callback_token = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_gateway_configurations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_gateway_gateway_reference",
                table: "payments",
                columns: new[] { "gateway", "gateway_reference" },
                unique: true,
                filter: "gateway_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_gateway_configurations_gateway",
                table: "payment_gateway_configurations",
                column: "gateway",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_gateway_configurations");

            migrationBuilder.DropIndex(
                name: "ix_payments_gateway_gateway_reference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "gateway",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "gateway_reference",
                table: "payments");
        }
    }
}
