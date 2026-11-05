using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddedTariffChargeCodeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "charge_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    applicable_modes = table.Column<string>(type: "text", nullable: false),
                    default_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_charge_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tariffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    service_level = table.Column<string>(type: "text", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_lane = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    destination_lane = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    equipment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    supersedes_tariff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariffs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tariff_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_version = table.Column<int>(type: "integer", nullable: false),
                    new_version = table.Column<int>(type: "integer", nullable: false),
                    changed_by = table.Column<string>(type: "text", nullable: true),
                    change_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariff_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_tariff_history_tariff_tariff_id",
                        column: x => x.tariff_id,
                        principalTable: "tariffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tariff_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_model = table.Column<string>(type: "text", nullable: false),
                    base_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maximum = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    free_days = table.Column<int>(type: "integer", nullable: true),
                    rate_after_free_days = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    percentage_base = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    breakpoint_json = table.Column<string>(type: "text", nullable: true),
                    charge_code_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tariff_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_tariff_rates_charge_codes_charge_code_id",
                        column: x => x.charge_code_id,
                        principalTable: "charge_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tariff_rates_charge_codes_charge_code_id1",
                        column: x => x.charge_code_id1,
                        principalTable: "charge_codes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tariff_rates_tariffs_tariff_id",
                        column: x => x.tariff_id,
                        principalTable: "tariffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_charge_codes_tenant_id_category",
                table: "charge_codes",
                columns: new[] { "tenant_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_charge_codes_tenant_id_code_is_active",
                table: "charge_codes",
                columns: new[] { "tenant_id", "code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_tariff_history_tariff_id",
                table: "tariff_history",
                column: "tariff_id");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_rates_charge_code_id",
                table: "tariff_rates",
                column: "charge_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_rates_charge_code_id1",
                table: "tariff_rates",
                column: "charge_code_id1");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_rates_tariff_id_charge_code_id",
                table: "tariff_rates",
                columns: new[] { "tariff_id", "charge_code_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tariffs_name_mode_service_level_contract_id_is_active",
                table: "tariffs",
                columns: new[] { "name", "mode", "service_level", "contract_id", "is_active" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tariff_history");

            migrationBuilder.DropTable(
                name: "tariff_rates");

            migrationBuilder.DropTable(
                name: "charge_codes");

            migrationBuilder.DropTable(
                name: "tariffs");
        }
    }
}
