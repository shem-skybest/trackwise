using Microsoft.Extensions.Logging;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities.Note;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds standard operational notes, such as standard quotation terms and conditions.
/// </summary>
internal class NoteSeeder(ILogger<NoteSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(NoteSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 300;
    public override IReadOnlyList<string> DependsOn => [];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Note>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<Note>();
        var created = new List<Note>();

        var termsContent = """
        Domestic Trucking Terms and Conditions:

        1. The above rates are subject to 12% VAT.
        2. Cargo Weight: 18–20 Tons/40'FT.
        3. Term: Door-to-Door.
        4. Inclusive of MANO, both origin and destination.
        5. Inclusive of trucking services, both origin and destination.
        6. Warehouse handling and loading/unloading charges are billed per actual usage.
        7. Rates are valid from October 1, 2026 until further notice.
        """;

        var note = Note.Create(
            title: "Standard Quotation Terms",
            content: termsContent
        );

        // Optionally pin this note so it stays visible on top of note boards
        note.TogglePin();

        await repo.AddAsync(note, cancellationToken);
        created.Add(note);

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);

        // Pass to context if subsequent seeders (like QuotationSeeder) need to attach it
        context.CreatedNotes = created;

        LogCompleted(created.Count);
    }
}
