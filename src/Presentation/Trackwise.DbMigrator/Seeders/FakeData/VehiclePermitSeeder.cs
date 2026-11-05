using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the registration papers the vehicle register carries for each real truck and trailer (the
/// <c>Papers</c> of the <c>Fleet</c> and <c>Trailers</c> seed sections). Per record: a paper is skipped
/// when its vehicle already has one of the same type expiring the same day.
/// </summary>
internal class VehiclePermitSeeder(ILogger<VehiclePermitSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(VehiclePermitSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 214;
    public override IReadOnlyList<string> DependsOn => [nameof(FleetSeeder), nameof(TrailerSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedFleet().Length == 0 && context.GetSeedTrailers().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var uow = context.TenantUnitOfWork;
        var trucksByNumber = (await uow.Repository<Truck>().GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Number, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var trailersByCode = (await uow.Repository<Trailer>().GetListAsync(ct: cancellationToken))
            .ToDictionary(t => t.Code, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var existing = (await uow.Repository<VehiclePermit>().GetListAsync(ct: cancellationToken))
            .Select(p => (p.TruckId ?? p.TrailerId, p.Type, p.ExpiresOn))
            .ToHashSet();

        var papers = context.GetSeedFleet()
            .Select(t => (TruckId: trucksByNumber.GetValueOrDefault(Truck.NormalizeCode(t.Plate)), TrailerId: Guid.Empty, t.Papers))
            .Concat(context.GetSeedTrailers()
                .Select(t => (TruckId: Guid.Empty, TrailerId: trailersByCode.GetValueOrDefault(Trailer.NormalizeCode(t.Code)), t.Papers)))
            .SelectMany(v => v.Papers.Select(paper => (v.TruckId, v.TrailerId, Paper: paper)));

        var created = 0;
        foreach (var (truckId, trailerId, paper) in papers)
        {
            if (truckId == Guid.Empty && trailerId == Guid.Empty)
            {
                continue;
            }

            if (!Enum.TryParse<VehiclePermitType>(paper.Kind, out var type))
            {
                logger.LogWarning("Unknown paper kind '{Kind}' - skipped", paper.Kind);
                continue;
            }

            var vehicleId = truckId != Guid.Empty ? truckId : trailerId;
            if (!existing.Add((vehicleId, type, paper.ExpiresOn)))
            {
                continue;
            }

            var permit = truckId != Guid.Empty
                ? VehiclePermit.ForTruck(truckId, type, paper.ExpiresOn)
                : VehiclePermit.ForTrailer(trailerId, type, paper.ExpiresOn);
            permit.IssuedOn = paper.IssuedOn;
            permit.Cost = paper.Amount;
            permit.Notes = paper.Notes;

            await uow.Repository<VehiclePermit>().AddAsync(permit, cancellationToken);
            created++;
        }

        await uow.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }
}
