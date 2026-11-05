using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class UpdatedLoadConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "posted_trucks",
                newName: "destination_preference_zipcode");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "posted_trucks",
                newName: "destination_preference_subdivision");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "posted_trucks",
                newName: "destination_preference_street_address");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "posted_trucks",
                newName: "destination_preference_region");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "posted_trucks",
                newName: "destination_preference_province");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "posted_trucks",
                newName: "destination_preference_country");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "posted_trucks",
                newName: "destination_preference_city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "posted_trucks",
                newName: "destination_preference_barangay");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "loads",
                newName: "origin_address_zipcode");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "loads",
                newName: "origin_address_subdivision");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "loads",
                newName: "origin_address_street_address");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "loads",
                newName: "origin_address_region");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "loads",
                newName: "origin_address_province");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "loads",
                newName: "origin_address_country");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "loads",
                newName: "origin_address_city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "loads",
                newName: "origin_address_barangay");

            migrationBuilder.RenameColumn(
                name: "zipcode",
                table: "load_board_listings",
                newName: "origin_address_zipcode");

            migrationBuilder.RenameColumn(
                name: "subdivision",
                table: "load_board_listings",
                newName: "origin_address_subdivision");

            migrationBuilder.RenameColumn(
                name: "street_address",
                table: "load_board_listings",
                newName: "origin_address_street_address");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "load_board_listings",
                newName: "origin_address_region");

            migrationBuilder.RenameColumn(
                name: "province",
                table: "load_board_listings",
                newName: "origin_address_province");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "load_board_listings",
                newName: "origin_address_country");

            migrationBuilder.RenameColumn(
                name: "city_or_municipality",
                table: "load_board_listings",
                newName: "origin_address_city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "barangay",
                table: "load_board_listings",
                newName: "origin_address_barangay");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_zipcode",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_street_address",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_region",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_province",
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

            migrationBuilder.AlterColumn<string>(
                name: "destination_preference_city_or_municipality",
                table: "posted_trucks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_barangay",
                table: "posted_trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_city_or_municipality",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_country",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_province",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_region",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_street_address",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_subdivision",
                table: "posted_trucks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "available_at_address_zipcode",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_barangay",
                table: "loads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_address_city_or_municipality",
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
                name: "destination_address_province",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_region",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_street_address",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_subdivision",
                table: "loads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_address_zipcode",
                table: "loads",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_barangay",
                table: "load_board_listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_address_city_or_municipality",
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
                name: "destination_address_province",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_region",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_street_address",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_address_subdivision",
                table: "load_board_listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destination_address_zipcode",
                table: "load_board_listings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_at_address_barangay",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_city_or_municipality",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_country",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_province",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_region",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_street_address",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_subdivision",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "available_at_address_zipcode",
                table: "posted_trucks");

            migrationBuilder.DropColumn(
                name: "destination_address_barangay",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_city_or_municipality",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_country",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_province",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_region",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_street_address",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_subdivision",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_zipcode",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "destination_address_barangay",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_city_or_municipality",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_country",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_province",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_region",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_street_address",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_subdivision",
                table: "load_board_listings");

            migrationBuilder.DropColumn(
                name: "destination_address_zipcode",
                table: "load_board_listings");

            migrationBuilder.RenameColumn(
                name: "destination_preference_zipcode",
                table: "posted_trucks",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "destination_preference_subdivision",
                table: "posted_trucks",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "destination_preference_street_address",
                table: "posted_trucks",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "destination_preference_region",
                table: "posted_trucks",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "destination_preference_province",
                table: "posted_trucks",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "destination_preference_country",
                table: "posted_trucks",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "destination_preference_city_or_municipality",
                table: "posted_trucks",
                newName: "city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "destination_preference_barangay",
                table: "posted_trucks",
                newName: "barangay");

            migrationBuilder.RenameColumn(
                name: "origin_address_zipcode",
                table: "loads",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "origin_address_subdivision",
                table: "loads",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "origin_address_street_address",
                table: "loads",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "origin_address_region",
                table: "loads",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "origin_address_province",
                table: "loads",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "origin_address_country",
                table: "loads",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "origin_address_city_or_municipality",
                table: "loads",
                newName: "city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "origin_address_barangay",
                table: "loads",
                newName: "barangay");

            migrationBuilder.RenameColumn(
                name: "origin_address_zipcode",
                table: "load_board_listings",
                newName: "zipcode");

            migrationBuilder.RenameColumn(
                name: "origin_address_subdivision",
                table: "load_board_listings",
                newName: "subdivision");

            migrationBuilder.RenameColumn(
                name: "origin_address_street_address",
                table: "load_board_listings",
                newName: "street_address");

            migrationBuilder.RenameColumn(
                name: "origin_address_region",
                table: "load_board_listings",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "origin_address_province",
                table: "load_board_listings",
                newName: "province");

            migrationBuilder.RenameColumn(
                name: "origin_address_country",
                table: "load_board_listings",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "origin_address_city_or_municipality",
                table: "load_board_listings",
                newName: "city_or_municipality");

            migrationBuilder.RenameColumn(
                name: "origin_address_barangay",
                table: "load_board_listings",
                newName: "barangay");

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
                name: "street_address",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "region",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "province",
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

            migrationBuilder.AlterColumn<string>(
                name: "city_or_municipality",
                table: "posted_trucks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
