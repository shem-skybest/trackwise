using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddedQuotationEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "quotations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                approved_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                rejected_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_quotations", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "quotation_lines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                tariff_rate_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                fx_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                markup_type = table.Column<string>(type: "text", nullable: false),
                markup_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_quotation_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_quotation_lines_quotations_quotation_id",
                    column: x => x.quotation_id,
                    principalTable: "quotations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_quotation_lines_tariff_rate_tariff_rate_id",
                    column: x => x.tariff_rate_id,
                    principalTable: "tariff_rates",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_quotation_lines_quotation_id",
            table: "quotation_lines",
            column: "quotation_id");

        migrationBuilder.CreateIndex(
            name: "ix_quotation_lines_tariff_rate_id",
            table: "quotation_lines",
            column: "tariff_rate_id");

        migrationBuilder.CreateIndex(
            name: "ix_quotations_customer_id_status",
            table: "quotations",
            columns: new[] { "customer_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "quotation_lines");

        migrationBuilder.DropTable(
            name: "quotations");
    }
}
