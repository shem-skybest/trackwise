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

        foreach (var (def, index) in Notes.Select((def, index) => (def, index)))
        {
            var note = Note.Create(def.Title, def.Content);
            note.DisplayOrder = index;

            // Pinned notes stay on top of the note list
            if (def.Pinned)
                note.TogglePin();

            await repo.AddAsync(note, cancellationToken);
            created.Add(note);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);

        // Pass to context if subsequent seeders (like QuotationSeeder) need to attach it
        context.CreatedNotes = created;

        LogCompleted(created.Count);
    }

    private sealed record NoteDef(string Title, string Content, bool Pinned = false);

    private static readonly IReadOnlyList<NoteDef> Notes =
    [
        new("Standard Quotation Terms",
            """
            Domestic Trucking Terms and Conditions:

            1. The above rates are subject to 12% VAT.
            2. Cargo Weight: 18–20 Tons/40'FT.
            3. Term: Door-to-Door.
            4. Inclusive of MANO, both origin and destination.
            5. Inclusive of trucking services, both origin and destination.
            6. Warehouse handling and loading/unloading charges are billed per actual usage.
            7. Rates are valid from October 1, 2026 until further notice.
            """,
            Pinned: true),

        new("Reefer Notes",
            """
            NOTES:
            A. Above rates are subject to 12% VAT.
            B. (3) personnel included.
            C. Toll fees & Parking fee if any, will be charged at cost, receipt.
            D. Maximum of one drop allowed. Any extra drop at a different delivery location will incur an additional charge of ₱2,000.00.
            E. Backload – Charged at 80% of the quoted rate(s).
            F. Foul Trip – Charged at 80% of the quoted rate(s).
            G. Demurrage fees for the first 2 hours will be free, but exceeding that timeframe will incur a charge of 250 pesos per hour. (waiting time: Warehouse loading and Outlet unloading)
            H. Maximum Cargo Weight: 16footer – 10.2 tons

            We trust everything is in order and we are looking forward to be of service to you.
            """),
    ];
}
