using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddRateAgreementTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "backload_percent",
                table: "rate_agreements",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "demurrage_free_hours",
                table: "rate_agreements",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "foul_trip_percent",
                table: "rate_agreements",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "included_drops",
                table: "rate_agreements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tolls_and_parking_at_cost",
                table: "rate_agreements",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rate_extra_drop_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tonnage = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_extra_drop_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_rate_extra_drop_charges_rate_agreements_rate_agreement_id",
                        column: x => x.rate_agreement_id,
                        principalTable: "rate_agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rate_extra_drop_charges_rate_agreement_id_tonnage",
                table: "rate_extra_drop_charges",
                columns: new[] { "rate_agreement_id", "tonnage" },
                unique: true);

            migrationBuilder.AddColumn<decimal>(
                name: "demurrage_hourly_rate",
                table: "rate_agreements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "other_location_charge",
                table: "rate_agreements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // The single extra-drop amount becomes the any-size charge. The fixed foul-trip amount
            // has no percentage to become, and nothing ever set it.
            migrationBuilder.Sql("""
                INSERT INTO rate_extra_drop_charges (id, rate_agreement_id, tonnage, amount)
                SELECT gen_random_uuid(), id, NULL, extra_drop_charge
                FROM rate_agreements
                WHERE extra_drop_charge IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "extra_drop_charge",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "foul_trip_charge",
                table: "rate_agreements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "extra_drop_charge",
                table: "rate_agreements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "foul_trip_charge",
                table: "rate_agreements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE rate_agreements a
                SET extra_drop_charge = c.amount
                FROM rate_extra_drop_charges c
                WHERE c.rate_agreement_id = a.id AND c.tonnage IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "rate_extra_drop_charges");

            migrationBuilder.DropColumn(
                name: "backload_percent",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "demurrage_free_hours",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "foul_trip_percent",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "included_drops",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "tolls_and_parking_at_cost",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "demurrage_hourly_rate",
                table: "rate_agreements");

            migrationBuilder.DropColumn(
                name: "other_location_charge",
                table: "rate_agreements");
        }
    }
}
