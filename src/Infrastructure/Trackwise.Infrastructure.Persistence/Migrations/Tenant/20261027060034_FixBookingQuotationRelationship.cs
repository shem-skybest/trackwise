using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class FixBookingQuotationRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_quotation_quotation_id1",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_quotation_id1",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "quotation_id1",
                table: "bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "quotation_id1",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_bookings_quotation_id1",
                table: "bookings",
                column: "quotation_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_quotation_quotation_id1",
                table: "bookings",
                column: "quotation_id1",
                principalTable: "quotations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
