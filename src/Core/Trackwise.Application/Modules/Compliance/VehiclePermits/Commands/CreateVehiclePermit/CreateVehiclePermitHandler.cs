using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

internal sealed class CreateVehiclePermitHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateVehiclePermitCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehiclePermitCommand req, CancellationToken ct)
    {
        VehiclePermit permit;
        if (req.TruckId is { } truckId)
        {
            if (await tenantUow.Repository<Truck>().GetByIdAsync(truckId, ct) is null)
            {
                return Result<Guid>.Fail($"Could not find the truck '{truckId}'.");
            }

            permit = VehiclePermit.ForTruck(truckId, req.Type, req.ExpiresOn);
        }
        else
        {
            var trailerId = req.TrailerId!.Value;
            if (await tenantUow.Repository<Trailer>().GetByIdAsync(trailerId, ct) is null)
            {
                return Result<Guid>.Fail($"Could not find the trailer '{trailerId}'.");
            }

            permit = VehiclePermit.ForTrailer(trailerId, req.Type, req.ExpiresOn);
        }

        permit.Number = req.Number;
        permit.Issuer = req.Issuer;
        permit.IssuedOn = req.IssuedOn;
        permit.Cost = req.Cost;
        permit.Notes = req.Notes;

        await tenantUow.Repository<VehiclePermit>().AddAsync(permit, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(permit.Id);
    }
}
