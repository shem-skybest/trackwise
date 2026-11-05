using System.Text.RegularExpressions;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the tenant's real trucks (the <c>Fleet</c> seed section) with their home branch and, where
/// one exists, the driver who holds the unit. Like <see cref="CrewSeeder" />, these are never
/// drafted into generated loads or trips - the demo data keeps running on <see cref="TruckSeeder" />'s
/// numbered units - and it seeds per record, keyed on the plate. A truck seeded before the register
/// fields existed gets the blank ones filled in, never overwritten.
/// </summary>
internal class FleetSeeder(ILogger<FleetSeeder> log) : SeederBase(log)
{
    public override string Name => nameof(FleetSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 211;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder), nameof(CrewSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedFleet().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var truckRepository = context.TenantUnitOfWork.Repository<Truck>();
        var existing = (await truckRepository.GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Number, StringComparer.OrdinalIgnoreCase);
        var branchesByCode = (await context.TenantUnitOfWork.Repository<Branch>().GetListAsync(ct: cancellationToken))
            .ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var employeesByName = (await context.TenantUnitOfWork.Repository<Employee>().GetListAsync(ct: cancellationToken))
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var unit in context.GetSeedFleet())
        {
            var plate = Truck.NormalizeCode(unit.Plate);
            if (!Enum.TryParse<TruckType>(unit.Type, out var type))
            {
                logger.LogWarning("Unknown truck type '{Type}' for {Plate} - skipped", unit.Type, plate);
                continue;
            }

            TruckTonnageClass? tonnage = Enum.TryParse<TruckTonnageClass>(unit.TonnageClass, out var parsed) ? parsed : null;

            if (existing.TryGetValue(plate, out var known))
            {
                FillRegister(known, unit);
                continue;
            }

            var truck = new Truck
            {
                Number = plate,
                Name = $"{plate} ({Label(type, tonnage)})",
                Type = type,
                TonnageClass = tonnage,
                Make = unit.Make,
                Model = unit.Model,
                Vin = unit.ChassisNumber,
                LicensePlate = DisplayPlate(plate),
                LicensePlateState = "PH",
                IsActive = unit.IsActive
            };
            FillRegister(truck, unit);

            if (unit.Branch is { } branchCode && branchesByCode.TryGetValue(branchCode, out var branch))
            {
                truck.BranchId = branch.Id;
            }

            if (unit.MainDriver is { } driverName)
            {
                if (employeesByName.TryGetValue(driverName, out var driver))
                {
                    truck.MainDriverId = driver.Id;
                }
                else
                {
                    logger.LogWarning("Main driver '{Driver}' for {Plate} not found - left unassigned", driverName, plate);
                }
            }

            await truckRepository.AddAsync(truck, cancellationToken);
            existing[plate] = truck;
            created++;
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }

    private static void FillRegister(Truck truck, FleetTruckData unit)
    {
        truck.AssetCode ??= unit.AssetCode;
        truck.ConductionSticker ??= unit.ConductionSticker;
        truck.EngineNumber ??= unit.EngineNumber;
        truck.AcquiredOn ??= unit.AcquiredOn;
        truck.Supplier ??= unit.Supplier;
        truck.Ownership ??= Enum.TryParse<TruckOwnership>(unit.Ownership, out var ownership) ? ownership : null;
    }

    /// <summary><c>ABC1267</c> is printed <c>ABC 1267</c>; odd formats (<c>TST184</c>) stay as they are.</summary>
    private static string DisplayPlate(string plate)
    {
        var match = Regex.Match(plate, "^([A-Z]{3})([0-9]{3,4})$");
        return match.Success ? $"{match.Groups[1].Value} {match.Groups[2].Value}" : plate;
    }

    private static string Label(TruckType type, TruckTonnageClass? tonnage) => type switch
    {
        TruckType.Reefer when tonnage is { } t => $"{TonnageLabel(t)} Reefer",
        TruckType.ContainerTruck => "Tractor Head",
        _ => type.GetDescription()
    };

    private static string TonnageLabel(TruckTonnageClass tonnage) => tonnage switch
    {
        TruckTonnageClass.TwoTonner => "2T",
        TruckTonnageClass.ThreeTonner => "3T",
        TruckTonnageClass.FourTonner => "4T",
        _ => "10T"
    };
}
