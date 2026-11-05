using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class UpdatedAddressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_at_address_city",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_line2",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "destination_preference_city",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "destination_preference_line1",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "destination_address_city",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_country",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_line1",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_state",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_city",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_country",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_line1",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_state",
                table: "load_board_listings");

            migrationBuilder.RenameColumn(
                name: "current_address_zip_code",
                table: "trucks",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "current_address_country",
                table: "trucks",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "current_address_state",
                table: "trucks",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "current_address_line2",
                table: "trucks",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "current_address_line1",
                table: "trucks",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "current_address_city",
                table: "trucks",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "address_zip_code",
                table: "trip_stops",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "address_country",
                table: "trip_stops",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "address_state",
                table: "trip_stops",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "trip_stops",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "trip_stops",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "address_city",
                table: "trip_stops",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "address_zip_code",
                table: "terminals",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "address_country",
                table: "terminals",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "address_state",
                table: "terminals",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "terminals",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "terminals",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "address_city",
                table: "terminals",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "destination_preference_zip_code",
                table: "posted_trucks",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "destination_preference_country",
                table: "posted_trucks",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "destination_preference_state",
                table: "posted_trucks",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "destination_preference_line2",
                table: "posted_trucks",
                newName: "barangay");

            migrationBuilder.RenameColumn(
                name: "available_at_address_zip_code",
                table: "posted_trucks",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "available_at_address_state",
                table: "posted_trucks",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "available_at_address_line1",
                table: "posted_trucks",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "available_at_address_country",
                table: "posted_trucks",
                newName: "city_or_municipality");

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

            migrationBuilder.RenameColumn(
                name: "origin_address_zip_code",
                table: "loads",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "origin_address_country",
                table: "loads",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "origin_address_state",
                table: "loads",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "origin_address_line2",
                table: "loads",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "origin_address_line1",
                table: "loads",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "origin_address_city",
                table: "loads",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "destination_address_zip_code",
                table: "loads",
                newName: "city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "destination_address_line2",
                table: "loads",
                newName: "barangay");

            migrationBuilder.RenameColumn(
                name: "origin_address_zip_code",
                table: "load_board_listings",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "origin_address_country",
                table: "load_board_listings",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "origin_address_state",
                table: "load_board_listings",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "origin_address_line2",
                table: "load_board_listings",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "origin_address_line1",
                table: "load_board_listings",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "origin_address_city",
                table: "load_board_listings",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "destination_address_zip_code",
                table: "load_board_listings",
                newName: "city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "destination_address_line2",
                table: "load_board_listings",
                newName: "barangay");

            migrationBuilder.RenameColumn(
                name: "address_zip_code",
                table: "employees",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "address_country",
                table: "employees",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "address_state",
                table: "employees",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "employees",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "employees",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "address_city",
                table: "employees",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "address_zip_code",
                table: "customers",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "address_country",
                table: "customers",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "address_state",
                table: "customers",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "customers",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "customers",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "address_city",
                table: "customers",
                newName: "province");

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "trip_stops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "trip_stops",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "terminals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "terminals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "zipcode",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_or_municipality",
                table: "customers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "barangay",
                table: "trucks");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "trucks");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "trip_stops");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "terminals");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "terminals");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "city_or_municipality",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "trucks",
                newName: "current_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "trucks",
                newName: "current_address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "trucks",
                newName: "current_address_state");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "trucks",
                newName: "current_address_line2");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "trucks",
                newName: "current_address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "trucks",
                newName: "current_address_city");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "trip_stops",
                newName: "address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "trip_stops",
                newName: "address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "trip_stops",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "trip_stops",
                newName: "address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "trip_stops",
                newName: "address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "trip_stops",
                newName: "address_city");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "terminals",
                newName: "address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "terminals",
                newName: "address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "terminals",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "terminals",
                newName: "address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "terminals",
                newName: "address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "terminals",
                newName: "address_city");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "posted_trucks",
                newName: "destination_preference_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "posted_trucks",
                newName: "destination_preference_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "posted_trucks",
                newName: "destination_preference_state");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "posted_trucks",
                newName: "available_at_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "posted_trucks",
                newName: "available_at_address_state");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "posted_trucks",
                newName: "available_at_address_line1");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "posted_trucks",
                newName: "available_at_address_country");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "posted_trucks",
                newName: "destination_preference_line2");

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

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "loads",
                newName: "origin_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "loads",
                newName: "origin_address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "loads",
                newName: "origin_address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "loads",
                newName: "origin_address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "loads",
                newName: "origin_address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "loads",
                newName: "origin_address_city");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "loads",
                newName: "destination_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "loads",
                newName: "destination_address_line2");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "load_board_listings",
                newName: "origin_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "load_board_listings",
                newName: "origin_address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "load_board_listings",
                newName: "origin_address_line2");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "load_board_listings",
                newName: "origin_address_state");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "load_board_listings",
                newName: "origin_address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "load_board_listings",
                newName: "origin_address_city");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "load_board_listings",
                newName: "destination_address_zip_code");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "load_board_listings",
                newName: "destination_address_line2");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "employees",
                newName: "address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "employees",
                newName: "address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "employees",
                newName: "address_state");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "employees",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "employees",
                newName: "address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "employees",
                newName: "address_city");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "customers",
                newName: "address_zip_code");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "customers",
                newName: "address_country");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "customers",
                newName: "address_state");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "customers",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "customers",
                newName: "address_line1");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "customers",
                newName: "address_city");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_zip_code",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_country",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_city",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_line2",
                table: "posted_trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_preference_city",
                table: "posted_trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_preference_line1",
                table: "posted_trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_address_city",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_country",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_line1",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_state",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_city",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_country",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_line1",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_state",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
