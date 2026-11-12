using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddPaymentGatewayColumns : Migration
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

            migrationBuilder.CreateIndex(
                name: "ix_payments_gateway_gateway_reference",
                table: "payments",
                columns: new[] { "gateway", "gateway_reference" },
                unique: true,
                filter: "gateway_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
