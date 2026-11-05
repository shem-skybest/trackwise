using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class UpdateMasterTenantAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "company_address_zip_code",
                table: "tenants",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "company_address_country",
                table: "tenants",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "company_address_state",
                table: "tenants",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "company_address_line2",
                table: "tenants",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "company_address_line1",
                table: "tenants",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "company_address_city",
                table: "tenants",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "billing_address_zip_code",
                table: "payments",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "billing_address_country",
                table: "payments",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "billing_address_state",
                table: "payments",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "billing_address_line2",
                table: "payments",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "billing_address_line1",
                table: "payments",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "billing_address_city",
                table: "payments",
                newName: "province");

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "payments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "barangay",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "payments");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "tenants",
                newName: "company_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "tenants",
                newName: "company_address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "tenants",
                newName: "company_address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "tenants",
                newName: "company_address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "tenants",
                newName: "company_address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "tenants",
                newName: "company_address_city");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "payments",
                newName: "billing_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "payments",
                newName: "billing_address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "payments",
                newName: "billing_address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "payments",
                newName: "billing_address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "payments",
                newName: "billing_address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "payments",
                newName: "billing_address_city");
        }
    }
}
