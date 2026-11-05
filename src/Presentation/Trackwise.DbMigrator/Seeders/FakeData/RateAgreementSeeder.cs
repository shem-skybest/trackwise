using System.Globalization;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds each client's rate card history (the <c>RateAgreements</c> seed section) from the Monday
/// quotation boards. Every revision is approved in date order, so the latest is in force and each
/// earlier one is closed the day before its successor starts - the history Monday only kept as
/// group titles. A client's current card that the seed has since corrected (its fuel method or
/// quotation terms differ) is withdrawn and seeded again while nothing is priced from it; an older
/// or already used card has to be revised in the app instead.
/// </summary>
internal class RateAgreementSeeder(ILogger<RateAgreementSeeder> log) : SeederBase(log)
{
    private const string ImportedBy = "Monday import";

    public override string Name => nameof(RateAgreementSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 213;
    public override IReadOnlyList<string> DependsOn => [nameof(CustomerSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedRateAgreements().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var repository = context.TenantUnitOfWork.Repository<RateAgreement>();
        var customersByName = (await context.TenantUnitOfWork.Repository<Customer>().GetListAsync(ct: cancellationToken))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var existing = (await repository.Query()
                .Where(a => a.Status != RateAgreementStatus.Withdrawn)
                .ToListAsync(cancellationToken))
            .ToDictionary(a => (a.CustomerId, a.Name));
        var created = 0;

        foreach (var client in context.GetSeedRateAgreements().GroupBy(a => a.Customer))
        {
            if (!customersByName.TryGetValue(client.Key, out var customerId))
            {
                logger.LogWarning("Customer '{Customer}' not found - its rate agreements were skipped", client.Key);
                continue;
            }

            // Re-running adds only the revisions not yet seeded; the client's current one is the
            // latest already in force, so a new revision supersedes it like an approval would.
            var current = await repository.Query()
                .Where(a => a.CustomerId == customerId && a.Status == RateAgreementStatus.Active)
                .OrderByDescending(a => a.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            foreach (var data in client.OrderBy(a => a.EffectiveFrom, StringComparer.Ordinal))
            {
                if (existing.TryGetValue((customerId, data.Name), out var seeded))
                {
                    if (SameQuotationTerms(seeded.Terms, ToTerms(data)))
                    {
                        continue;
                    }

                    // A superseded card is history: it keeps the terms it was in force with.
                    if (seeded.Status is not RateAgreementStatus.Active)
                    {
                        continue;
                    }

                    if (await IsUsedAsync(context, seeded.Id, cancellationToken))
                    {
                        logger.LogWarning(
                            "Rate agreement '{Name}' differs from the seed but is already used - revise it in the app",
                            data.Name);
                        continue;
                    }

                    seeded.Withdraw();
                    current = null;
                }

                var agreement = RateAgreement.Create(
                    customerId,
                    data.Name,
                    DateOnly.ParseExact(data.EffectiveFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    null,
                    ToTerms(data),
                    data.Notes);

                agreement.ReplaceLines(data.Lines.Select(ToSpec));

                if (current is not null && current.EffectiveFrom < agreement.EffectiveFrom)
                {
                    current.SupersedeFrom(agreement.EffectiveFrom);
                }

                agreement.Approve(ImportedBy, DateTime.UtcNow);
                await repository.AddAsync(agreement, cancellationToken);
                current = agreement;
                created++;
            }
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }

    private static async Task<bool> IsUsedAsync(SeederContext context, Guid agreementId, CancellationToken ct) =>
        await context.TenantUnitOfWork.Repository<Trip>().Query().AnyAsync(t => t.RateAgreementId == agreementId, ct)
        || await context.TenantUnitOfWork.Repository<Booking>().Query().AnyAsync(b => b.RateAgreementId == agreementId, ct);

    private static RateAgreementTerms ToTerms(RateAgreementData data) => new(
        Enum.Parse<FuelAdjustmentMethod>(data.FuelMethod),
        data.IncludedDrops,
        data.ExtraDropCharges
            .Select(c => (c.Tonnage is null ? (TruckTonnageClass?)null : Enum.Parse<TruckTonnageClass>(c.Tonnage), c.Amount))
            .ToList(),
        data.FoulTripPercent,
        data.BackloadPercent,
        data.DemurrageFreeHours,
        data.DemurrageHourlyRate,
        data.OtherLocationCharge,
        data.TollsAndParkingAtCost,
        data.BillExtraDrops,
        data.RatesIncludeVat);

    /// <summary>
    /// The quotation terms the seed sets once. The billing switches are left out: after seeding
    /// they are the office's to flip on the agreement, and a re-run mustn't undo that.
    /// </summary>
    private static bool SameQuotationTerms(RateAgreementTerms seeded, RateAgreementTerms data) =>
        seeded.FuelMethod == data.FuelMethod
        && seeded.IncludedDrops == data.IncludedDrops
        && seeded.FoulTripPercent == data.FoulTripPercent
        && seeded.BackloadPercent == data.BackloadPercent
        && seeded.DemurrageFreeHours == data.DemurrageFreeHours
        && seeded.DemurrageHourlyRate == data.DemurrageHourlyRate
        && seeded.OtherLocationCharge == data.OtherLocationCharge
        && seeded.TollsAndParkingAtCost == data.TollsAndParkingAtCost
        && (seeded.ExtraDropCharges ?? []).OrderBy(c => c.Tonnage).SequenceEqual((data.ExtraDropCharges ?? []).OrderBy(c => c.Tonnage));

    private static RateLineSpec ToSpec(RateLineData line) => new(
        line.Label,
        line.Tonnage is null ? null : Enum.Parse<TruckTonnageClass>(line.Tonnage),
        line.Amount,
        line.MinKm,
        line.MaxKm,
        line.City,
        line.Province,
        line.DistanceKm,
        line.FuelPercent,
        line.FuelSurcharges.Select(s => (s.DieselPrice, s.Amount)).ToList());
}
