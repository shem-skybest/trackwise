using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Primitives.Enums.Maintenance;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the trucks' repair history (the <c>Repairs</c> seed section) from nSFS's parts requisitions:
/// one service record per truck per requisition. Per record: skipped when the truck already has one
/// on the same requisition.
/// </summary>
internal class RepairHistorySeeder(ILogger<RepairHistorySeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(RepairHistorySeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 216;
    public override IReadOnlyList<string> DependsOn => [nameof(FleetSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedRepairs().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var uow = context.TenantUnitOfWork;
        var trucksByNumber = (await uow.Repository<Truck>().GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Number, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var existing = (await uow.Repository<MaintenanceRecord>().GetListAsync(ct: cancellationToken))
            .Select(r => (r.TruckId, r.RequisitionNumber))
            .ToHashSet();

        var created = 0;
        foreach (var data in context.GetSeedRepairs())
        {
            if (!trucksByNumber.TryGetValue(Truck.NormalizeCode(data.Plate), out var truckId))
            {
                logger.LogWarning("Truck '{Plate}' not found - its repair on {Requisition} was skipped", data.Plate, data.RequisitionNumber);
                continue;
            }

            if (!Enum.TryParse<MaintenanceType>(data.Type, out var type))
            {
                logger.LogWarning("Unknown maintenance type '{Type}' - skipped", data.Type);
                continue;
            }

            var record = new MaintenanceRecord
            {
                TruckId = truckId,
                MaintenanceType = type,
                VehicleSystem = Enum.TryParse<VehicleSystem>(data.System, out var system) ? system : null,
                ServiceDate = data.ServiceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                RequisitionNumber = data.RequisitionNumber,
                VendorName = data.Vendor,
                InvoiceNumber = data.Invoice,
                Description = data.Description,
                WorkPerformed = data.Notes
            };

            if (!existing.Add((truckId, record.RequisitionNumber)))
            {
                continue;
            }

            var parts = data.Parts.Select(p => new MaintenancePartSpec(p.Name, p.Description, null, p.Quantity, p.UnitCost));
            record.ReplaceParts(parts, data.LaborCost, partsCostWithoutLines: 0);

            await uow.Repository<MaintenanceRecord>().AddAsync(record, cancellationToken);
            created++;
        }

        await uow.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }
}
