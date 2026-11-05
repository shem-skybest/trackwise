using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddPartnersMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_business_partners_code",
            table: "business_partners");

        // "approved", not the scaffolded empty string. Every partner already in this table was
        // created before approval existed and is live right now - on files, in dropdowns, on
        // bills of lading. Landing them as drafts would deactivate the entire partner list on
        // deploy and put it all in an approver's queue, which is the loudest possible way to
        // discover that a default is not a decision.
        //
        // The empty string is worse than wrong, too: it is not a member of the enum, so the
        // converter reads it back as the zero value - Draft - and nothing anywhere would say so.
        migrationBuilder.AddColumn<string>(
            name: "approval_state",
            table: "business_partners",
            type: "text",
            nullable: false,
            defaultValue: "approved");

        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            table: "business_partners",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "business_partners",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "deleted_by",
            table: "business_partners",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "merged_into_id",
            table: "business_partners",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "master_data_approval_requests",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                entity_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                submitted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                submitted_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                submission_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                decision = table.Column<string>(type: "text", nullable: true),
                reviewed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                reviewed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                review_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_master_data_approval_requests", x => x.id);
            });

        // The default was the backfill, not the rule. Left in place it would quietly approve any
        // row inserted without naming the column - which is what a bulk load or a repair script
        // does - so it goes as soon as it has done its job. The entity's own default is Draft,
        // and the model carries no default at all, so leaving one here would also put the
        // database out of step with the snapshot.
        migrationBuilder.Sql(
            "ALTER TABLE business_partners ALTER COLUMN approval_state DROP DEFAULT;");

        // The column now stores digits only - BusinessPartner.NormalizeTaxId does it on write -
        // and the duplicate probe matches it exactly. Existing rows were typed by hand with
        // whatever punctuation the certificate used, so without this backfill the probe would
        // miss every duplicate of an existing partner: "123-456-789-000" and "123456789000"
        // would be different TINs to it, and the index below would be built over both forms.
        migrationBuilder.Sql("""
            UPDATE business_partners
            SET tax_id = NULLIF(regexp_replace(tax_id, '[^0-9]', '', 'g'), '')
            WHERE tax_id IS NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_approval_state",
            table: "business_partners",
            column: "approval_state");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_branch_id",
            table: "business_partners",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_code",
            table: "business_partners",
            column: "code",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_deleted_at",
            table: "business_partners",
            column: "deleted_at");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_merged_into_id",
            table: "business_partners",
            column: "merged_into_id");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_tax_id",
            table: "business_partners",
            column: "tax_id",
            filter: "tax_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_updated_at",
            table: "business_partners",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "ix_master_data_approval_requests_entity_type_entity_id",
            table: "master_data_approval_requests",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "ix_master_data_approval_requests_open",
            table: "master_data_approval_requests",
            column: "submitted_at",
            filter: "decision IS NULL");

        migrationBuilder.AddForeignKey(
            name: "fk_business_partners_branch_branch_id",
            table: "business_partners",
            column: "branch_id",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_business_partners_business_partners_merged_into_id",
            table: "business_partners",
            column: "merged_into_id",
            principalTable: "business_partners",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        // The probe indexes EF cannot express. The expressions must match DuplicateProbe's
        // predicates exactly - lower(btrim(...)) for the trigram arm, upper(btrim(...)) for
        // each exact arm - or PostgreSQL cannot use them and every probe is a sequential scan.
        // Partners are the largest list in the module by an order of magnitude, so this is the
        // first table where that scan would be felt rather than merely true.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_business_partners_name_trgm
            ON business_partners
            USING gin (lower(btrim(name)) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_business_partners_code_probe
            ON business_partners (upper(btrim(code)));
            """);

        // The TIN rule needs its own, for the reason the vessel IMO rule does: without one the
        // OR has only two of its three arms indexed and the planner abandons the BitmapOr.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_business_partners_tax_id_probe
            ON business_partners (upper(btrim(tax_id)));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_business_partners_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_business_partners_code_probe;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_business_partners_tax_id_probe;");

        migrationBuilder.DropForeignKey(
            name: "fk_business_partners_branch_branch_id",
            table: "business_partners");

        migrationBuilder.DropForeignKey(
            name: "fk_business_partners_business_partners_merged_into_id",
            table: "business_partners");

        migrationBuilder.DropTable(
            name: "master_data_approval_requests");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_approval_state",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_branch_id",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_code",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_deleted_at",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_merged_into_id",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_tax_id",
            table: "business_partners");

        migrationBuilder.DropIndex(
            name: "ix_business_partners_updated_at",
            table: "business_partners");

        migrationBuilder.DropColumn(
            name: "approval_state",
            table: "business_partners");

        migrationBuilder.DropColumn(
            name: "branch_id",
            table: "business_partners");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "business_partners");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "business_partners");

        migrationBuilder.DropColumn(
            name: "merged_into_id",
            table: "business_partners");

        migrationBuilder.CreateIndex(
            name: "ix_business_partners_code",
            table: "business_partners",
            column: "code",
            unique: true);
    }
}
