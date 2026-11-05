using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Gives every active reefer (the <c>ReeferPms</c> seed section) a weekly reefer PMS schedule, the
/// interval Cold Chain's Monday PMS board tracked. A unit with no last done date starts out due.
/// Skipped per truck when it already has a reefer PMS schedule.
/// </summary>
internal class ReeferPmsSeeder(ILogger<ReeferPmsSeeder> logger) : SeederBase(logger)
{
    private const int WeeklyDays = 7;

    public override string Name => nameof(ReeferPmsSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 217;
    public override IReadOnlyList<string> DependsOn => [nameof(FleetSeeder), nameof(RepairHistorySeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedReeferPms().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var uow = context.TenantUnitOfWork;
        var trucksByNumber = (await uow.Repository<Truck>().GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Number, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var scheduled = (await uow.Repository<MaintenanceSchedule>()
                .GetListAsync(s => s.MaintenanceType == MaintenanceType.ReeferPms, cancellationToken))
            .Select(s => s.TruckId)
            .ToHashSet();

        var created = 0;
        foreach (var data in context.GetSeedReeferPms())
        {
            if (!trucksByNumber.TryGetValue(Truck.NormalizeCode(data.Plate), out var truckId) || !scheduled.Add(truckId))
            {
                continue;
            }

            var schedule = new MaintenanceSchedule
            {
                TruckId = truckId,
                MaintenanceType = MaintenanceType.ReeferPms,
                IntervalType = MaintenanceIntervalType.TimeBased,
                LastServiceDate = data.LastDoneOn?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Notes = "Weekly, as on Monday's REEFER SYSTEM PMS board."
            };
            schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, WeeklyDays, null);

            await uow.Repository<MaintenanceSchedule>().AddAsync(schedule, cancellationToken);
            created++;
        }

        await uow.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }
}
