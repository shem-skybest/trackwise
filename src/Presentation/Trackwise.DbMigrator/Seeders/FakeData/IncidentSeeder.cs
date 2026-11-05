using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Extensions;
using Trackwise.DbMigrator.Models;
using Trackwise.DbMigrator.Regions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Identity.Roles;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds sample road-accident incidents for testing.
/// </summary>
internal class IncidentSeeder(ILogger<IncidentSeeder> logger) : SeederBase(logger)
{
    private static readonly string[] AccidentDescriptions =
    [
        "Minor fender bender in parking lot while backing up.",
        "Rear-ended by another vehicle at red light. Other driver admitted fault.",
        "Sideswiped by merging vehicle on highway. Minor damage to door panel.",
        "Struck debris on highway causing tire blowout and minor damage.",
        "Other vehicle ran red light and struck passenger side.",
        "Lost traction on icy road and slid into guardrail.",
        "Backing into dock, misjudged distance and hit dock equipment.",
        "Cargo shifted during hard brake causing trailer damage.",
        "Side mirror clipped by passing vehicle in construction zone.",
        "Hit pothole at highway speed causing suspension damage."
    ];

    private static readonly string[] InjuryDescriptions =
    [
        "Minor whiplash, driver went to urgent care for evaluation.",
        "Small cuts from broken glass, treated on scene.",
        "Back pain reported, driver taken to hospital for x-rays.",
        "No visible injuries but driver reported headache."
    ];

    private static readonly string[] DamageDescriptions =
    [
        "Dent and scratches on rear bumper.",
        "Cracked taillight and minor body damage.",
        "Driver side mirror broken, door dented.",
        "Front bumper damage and cracked headlight.",
        "Trailer door mechanism damaged.",
        "Tire blown, rim scratched.",
        "Paint scraped on passenger side.",
        "Windshield cracked from debris impact."
    ];

    public override string Name => nameof(IncidentSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 160;
    public override IReadOnlyList<string> DependsOn =>
        [nameof(TruckSeeder), nameof(EmployeeSeeder)];

    // A tenant with real cases from Monday gets those instead (MondayIncidentSeeder).
    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return context.GetSeedIncidents().Length > 0
            || await context.TenantUnitOfWork.Repository<Incident>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var accidentRepository = context.TenantUnitOfWork.Repository<Incident>();
        var employeeRepository = context.TenantUnitOfWork.Repository<Employee>();
        var truckRepository = context.TenantUnitOfWork.Repository<Truck>();

        // Get drivers from context or load from database
        var drivers = context.CreatedEmployees?.Drivers
            ?? await employeeRepository.GetListAsync(e => e.Role != null && e.Role.Name == TenantRoles.Driver, ct: cancellationToken);

        if (drivers.Count == 0)
        {
            logger.LogWarning("No drivers available for incident seeding");
            LogCompleted(0);
            return;
        }

        // Get trucks
        var trucks = context.CreatedTrucks
            ?? await truckRepository.GetListAsync(ct: cancellationToken);

        if (trucks.Count == 0)
        {
            logger.LogWarning("No trucks available for incident seeding");
            LogCompleted(0);
            return;
        }

        var count = 0;
        var reportsToCreate = random.Next(context.Scale(5), context.Scale(12));

        for (var i = 0; i < reportsToCreate; i++)
        {
            var driver = random.Pick(drivers);
            var truck = random.Pick(trucks);
            var location = random.Pick((IList<RoutePoint>)(context.Region?.RoutePoints ?? []));
            var accidentDate = random.UtcDate(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddDays(-1));

            var incident = CreateAccident($"{Incident.DefaultCaseNumberPrefix}-{i + 1:000}", driver, truck, location, accidentDate);
            await accidentRepository.AddAsync(incident, cancellationToken);
            count++;
        }

        await context.TenantUnitOfWork.Repository<NumberSeries>().AddAsync(new NumberSeries
        {
            Series = Incident.CaseNumberSeries,
            Prefix = Incident.DefaultCaseNumberPrefix,
            PadWidth = Incident.CaseNumberPadWidth,
            LastAllocated = count
        }, cancellationToken);

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(count);
    }

    private Incident CreateAccident(
        string caseNumber,
        Employee driver,
        Truck truck,
        RoutePoint location,
        DateTime accidentDate)
    {
        var hasInjuries = random.NextDouble() < 0.15;
        var hasDamage = random.NextDouble() < 0.85;
        var hasPoliceReport = random.NextDouble() < 0.6;
        var severity = PickSeverity(hasInjuries);

        var details = new IncidentDetails
        {
            Kind = IncidentKind.Accident,
            AccidentType = random.Pick(Enum.GetValues<AccidentType>()),
            Severity = severity,
            OccurredAt = accidentDate,
            Latitude = location.Latitude + (random.NextDouble() - 0.5) * 0.01,
            Longitude = location.Longitude + (random.NextDouble() - 0.5) * 0.01,
            Place = $"{location.Address.StreetAddress}, {location.Address.CityOrMunicipality}, {location.Address.Province}",
            Summary = random.Pick(AccidentDescriptions),
            AnyInjuries = hasInjuries,
            NumberOfInjuries = hasInjuries ? random.Next(1, 4) : null,
            InjuryDescription = hasInjuries ? random.Pick(InjuryDescriptions) : null,
            VehicleDamaged = hasDamage,
            VehicleDamageDescription = hasDamage ? random.Pick(DamageDescriptions) : null,
            EstimatedDamageCost = hasDamage ? random.Next(500, 15000) : null,
            VehicleDrivable = severity is not (IncidentSeverity.Severe or IncidentSeverity.Fatal),
            PoliceReportFiled = hasPoliceReport,
            PoliceReportNumber = hasPoliceReport ? $"PR-{random.Next(100000, 999999)}" : null,
            PoliceStation = hasPoliceReport ? $"{location.Address.CityOrMunicipality} Police Station" : null,
            InsuranceClaimNumber = random.NextDouble() < 0.4 ? $"CLM-{accidentDate.Year}-{random.Next(100000, 999999)}" : null
        };

        var incident = Incident.Record(
            caseNumber,
            details, new IncidentParties(null, null, truck.Id, driver.Id, null, null), DateTime.UtcNow);

        var roll = random.NextDouble();
        if (roll >= 0.3)
        {
            incident.StartReview(null, "seed", accidentDate.AddDays(1));
        }

        if (roll >= 0.6)
        {
            incident.Resolve("Driver briefed on defensive driving; repair booked.", "seed", accidentDate.AddDays(3));
        }

        return incident;
    }

    private IncidentSeverity PickSeverity(bool hasInjuries)
    {
        if (hasInjuries)
        {
            return random.NextDouble() < 0.8 ? IncidentSeverity.Moderate : IncidentSeverity.Severe;
        }

        var roll = random.NextDouble();
        return roll switch
        {
            < 0.7 => IncidentSeverity.Minor,
            < 0.95 => IncidentSeverity.Moderate,
            _ => IncidentSeverity.Severe
        };
    }
}
