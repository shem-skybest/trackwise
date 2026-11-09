using Microsoft.EntityFrameworkCore;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Converts the Accepted quotations <see cref="QuotationSeeder"/> left behind into Bookings and
/// walks each through a different point of the lifecycle: InProgress (x2, so WorkfileSeeder can
/// promote both), Confirmed, and Cancelled. Quotations are picked by reference number, not by
/// array position, so reordering the QuotationSeeder cannot silently change which lane gets which status.
/// </summary>
internal class BookingSeeder(ILogger<BookingSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(BookingSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 230;
    public override IReadOnlyList<string> DependsOn => [nameof(QuotationSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Booking>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var quotations = context.CreatedQuotations;
        if (quotations is null)
        {
            LogUpstreamSkipped();
            return;
        }

        var accepted = quotations.Where(q => q.Status == QuotationStatus.Accepted).ToList();
        if (accepted.Count == 0)
        {
            context.CreatedBookings = [];
            LogCompleted(0);
            return;
        }

        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<Booking>();
        var created = new List<Booking>();

        // If the QuotationAccepted handler already ran during QuotationSeeder's save, a draft exists.
        // Reuse it instead of creating a second booking for the same quotation (unique index on quotation_id).
        var acceptedIds = accepted.Select(q => q.Id).ToList();
        var existing = await repo.Query()
            .Include(b => b.Documents)
            .Where(b => b.QuotationId.HasValue && acceptedIds.Contains(b.QuotationId.Value))
            .ToListAsync(cancellationToken);

        async Task<Booking?> DraftFor(string quotationReference)
        {
            var quotation = accepted.FirstOrDefault(q => q.ReferenceNumber == quotationReference);
            if (quotation is null) return null;

            var booking = existing.FirstOrDefault(b => b.QuotationId == quotation.Id);

            if (booking is null)
            {
                // No booking exists for this quotation yet (checked above), so this is its first: -01.
                var sequence = existing.Count(b => b.QuotationId == quotation.Id) + 1;

                booking = Booking.CreateDraftFromQuotation(
                    quotation,
                    bookingSequence: sequence,
                    paymentTermsDays: 30,
                    customerCreditCheckRequired: false);

                await repo.AddAsync(booking, cancellationToken);
            }

            created.Add(booking);
            return booking.Status == BookingStatus.Draft ? booking : null; // only drive Draft bookings through the lifecycle
        }

        // #1 first import lane: InProgress - WorkfileSeeder carries it to AccountingReview.
        if (await DraftFor("QT-2026-IMP-001") is { } firstImport)
        {
            SatisfyRequirementsAndConfirm(firstImport);
            firstImport.MarkInProgress();
        }

        // #2 second import lane: InProgress - WorkfileSeeder carries it all the way to Closed.
        if (await DraftFor("QT-2026-IMP-002") is { } secondImport)
        {
            SatisfyRequirementsAndConfirm(secondImport);
            secondImport.MarkInProgress();
        }

        // #3 export lane: left Confirmed - a booking not yet promoted to a workfile.
        if (await DraftFor("QT-2026-EXP-001") is { } confirmedExport)
        {
            SatisfyRequirementsAndConfirm(confirmedExport);
        }

        // #4 confirmed, then cancelled by the customer.
        if (await DraftFor("QT-2026-DOM-CANC") is { } cancelled)
        {
            SatisfyRequirementsAndConfirm(cancelled);
            cancelled.Cancel("Seed Data", "Customer cancelled the shipment.");
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedBookings = created;
        LogCompleted(created.Count);
    }

    /// <summary>
    /// Submits and verifies every open document requirement (KYC, permits, commercial invoice...),
    /// then confirms. Credit check is already NotRequired for seeded bookings.
    /// </summary>
    private static void SatisfyRequirementsAndConfirm(Booking booking)
    {
        foreach (var doc in booking.Documents.ToList())
        {
            if (doc.IsSatisfied) continue;

            var submitted = booking.AddDocument(doc.Type, $"SEED-{doc.Type}".ToUpperInvariant());
            booking.VerifyDocument(submitted.Id, "Seed Data");
        }

        booking.Confirm("Seed Data");
    }
}
