using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Imports Cold Chain's weekly-report cases (the <c>Incidents</c> seed section) as incidents, keeping
/// Monday's case numbers, and points the tenant's case-number series past the highest so the next
/// incident logged in TrackWise carries on from Monday's numbering. A numbered case already on
/// record is skipped; so is an unnumbered one with the same day and summary.
/// </summary>
internal class MondayIncidentSeeder(ILogger<MondayIncidentSeeder> logger) : SeederBase(logger)
{
    private const string ImportedBy = "Monday import";

    // Monday has dates only; noon in Manila keeps the day the same in every timezone the office uses.
    private static readonly TimeSpan ManilaNoonUtc = TimeSpan.FromHours(4);

    public override string Name => nameof(MondayIncidentSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 225;
    public override IReadOnlyList<string> DependsOn =>
        [nameof(FleetSeeder), nameof(CrewSeeder), nameof(StaffSeeder), nameof(BusinessPartnerSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedIncidents().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var uow = context.TenantUnitOfWork;
        var cases = context.GetSeedIncidents();
        var seriesData = context.GetSeedIncidentSeries();

        var trucks = (await uow.Repository<Truck>().GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Number, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var customers = (await uow.Repository<Customer>().GetListAsync(ct: cancellationToken))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var drivers = (await uow.Repository<Employee>().GetListAsync(ct: cancellationToken))
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var helpers = (await uow.Repository<Helper>().GetListAsync(ct: cancellationToken))
            .GroupBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var existing = await uow.Repository<Incident>().GetListAsync(ct: cancellationToken);
        var caseNumbers = existing.Select(i => i.CaseNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unnumbered = existing.Select(i => (DateOnly.FromDateTime(i.OccurredAt), i.Summary)).ToHashSet();

        var series = await SeriesAsync(context, seriesData, cancellationToken);
        series.LastAllocated = Math.Max(series.LastAllocated, cases.Max(c => RunningNumber(c.CaseNumber)));

        var created = 0;
        foreach (var data in cases.OrderBy(c => c.CaseNumber is null).ThenBy(c => c.CaseNumber).ThenBy(c => c.OccurredOn))
        {
            if (data.CaseNumber is { } number ? caseNumbers.Contains(number) : unnumbered.Contains((data.OccurredOn, data.Summary)))
            {
                continue;
            }

            if (!Enum.TryParse<IncidentKind>(data.Kind, out var kind)
                || !Enum.TryParse<IncidentSeverity>(data.Severity, out var severity))
            {
                logger.LogWarning("Case {CaseNumber}: unknown kind '{Kind}' or severity '{Severity}' - skipped",
                    data.CaseNumber, data.Kind, data.Severity);
                continue;
            }

            var caseNumber = data.CaseNumber ?? series.Format(++series.LastAllocated);
            var occurredAt = data.OccurredOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + ManilaNoonUtc;
            var parties = Parties(data, trucks, customers, drivers, helpers);

            Incident incident;
            try
            {
                incident = Incident.Record(caseNumber, Details(data, kind, severity, occurredAt), parties, DateTime.UtcNow);
                ApplyStatus(incident, data, occurredAt);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Case {CaseNumber} skipped: {Reason}", caseNumber, ex.Message);
                continue;
            }

            await uow.Repository<Incident>().AddAsync(incident, cancellationToken);
            caseNumbers.Add(caseNumber);
            created++;
        }

        await uow.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }

    /// <summary>The tenant's case-number series, set to the seed data's pattern.</summary>
    private static async Task<NumberSeries> SeriesAsync(
        SeederContext context, IncidentSeriesData? pattern, CancellationToken ct)
    {
        var repository = context.TenantUnitOfWork.Repository<NumberSeries>();
        var series = await repository.GetAsync(s => s.Series == Incident.CaseNumberSeries, ct);
        if (series is null)
        {
            series = new NumberSeries
            {
                Series = Incident.CaseNumberSeries,
                Prefix = Incident.DefaultCaseNumberPrefix,
                PadWidth = Incident.CaseNumberPadWidth
            };
            await repository.AddAsync(series, ct);
        }

        if (pattern is not null)
        {
            series.Prefix = pattern.Prefix;
            series.Separator = pattern.Separator;
            series.PadWidth = pattern.PadWidth;
        }

        return series;
    }

    /// <summary>The running number at the end of a case number - 78 for <c>CCL-CASENO.078</c>.</summary>
    private static long RunningNumber(string? caseNumber)
    {
        var digits = new string((caseNumber ?? "").Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return long.TryParse(digits, out var value) ? value : 0;
    }

    private static IncidentDetails Details(IncidentData data, IncidentKind kind, IncidentSeverity severity, DateTime occurredAt) => new()
    {
        Kind = kind,
        Severity = severity,
        OccurredAt = occurredAt,
        Summary = data.Summary,
        Prevention = data.Prevention,
        ThirdPartyCompensation = data.ThirdPartyCompensation,
        CrewChargeNote = data.CrewChargeNote
    };

    /// <summary>Monday lists drivers and helpers in one column: a name on a driver record is the driver.</summary>
    private static IncidentParties Parties(
        IncidentData data,
        IReadOnlyDictionary<string, Guid> trucks,
        IReadOnlyDictionary<string, Guid> customers,
        IReadOnlyDictionary<string, Guid> drivers,
        IReadOnlyDictionary<string, Guid> helpers)
    {
        Guid? driverId = null;
        var helperIds = new List<Guid>();
        foreach (var name in data.Crew)
        {
            if (driverId is null && drivers.TryGetValue(name, out var driver))
            {
                driverId = driver;
            }
            else if (helpers.TryGetValue(name, out var helper) && !helperIds.Contains(helper))
            {
                helperIds.Add(helper);
            }
        }

        return new IncidentParties(
            TripId: null,
            CustomerId: data.Client is { } client && customers.TryGetValue(client, out var customerId) ? customerId : null,
            TruckId: data.Plate is { } plate && trucks.TryGetValue(Truck.NormalizeCode(plate), out var truckId) ? truckId : null,
            DriverId: driverId,
            HelperId: helperIds.Count > 0 ? helperIds[0] : null,
            SecondHelperId: helperIds.Count > 1 ? helperIds[1] : null);
    }

    private static void ApplyStatus(Incident incident, IncidentData data, DateTime occurredAt)
    {
        var assisted = data.AssistedBy is { } by ? $"Assisted by {by}." : null;
        switch (data.Status)
        {
            case "UnderReview":
                incident.StartReview(Join(data.CorrectiveAction, assisted), ImportedBy, occurredAt);
                break;
            case "Resolved":
                var action = data.CorrectiveAction ?? "Marked done in Monday's weekly report.";
                incident.Resolve(Join(action, assisted)!, ImportedBy, occurredAt);
                break;
        }
    }

    private static string? Join(params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return present.Count == 0 ? null : string.Join("\n\n", present);
    }
}
