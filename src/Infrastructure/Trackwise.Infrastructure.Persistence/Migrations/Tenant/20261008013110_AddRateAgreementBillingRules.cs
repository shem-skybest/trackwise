using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddRateAgreementBillingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "bill_extra_drops",
                table: "rate_agreements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "rates_include_vat",
                table: "rate_agreements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Pacific's and HFI's Monday cards quote VAT-inclusive rates; nSFS bills them net.
            // A no-op in any tenant without these clients.
            migrationBuilder.Sql("""
                UPDATE rate_agreements SET rates_include_vat = TRUE
                WHERE customer_id IN (
                    SELECT id FROM customers WHERE name IN ('Hacienda Verde Inc.', 'Pacific Foods Solutions'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bill_extra_drops",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "rates_include_vat",
                table: "rate_agreements");
        }
    }
}
