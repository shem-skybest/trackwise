using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trackwise.Infrastructure.Persistence.Migrations.Tenant;

/// <inheritdoc />
public partial class AddedNoteEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                content = table.Column<string>(type: "text", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notes", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_notes_created_at",
            table: "notes",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "ix_notes_is_pinned_display_order",
            table: "notes",
            columns: new[] { "is_pinned", "display_order" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notes");
    }
}
