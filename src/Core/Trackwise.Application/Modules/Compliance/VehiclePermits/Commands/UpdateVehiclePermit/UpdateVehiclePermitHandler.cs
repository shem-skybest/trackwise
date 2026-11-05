using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

internal sealed class UpdateVehiclePermitHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateVehiclePermitCommand, Result>
{
    public async Task<Result> Handle(UpdateVehiclePermitCommand req, CancellationToken ct)
    {
        var permit = await tenantUow.Repository<VehiclePermit>().GetByIdAsync(req.Id, ct);
        if (permit is null)
        {
            return Result.Fail($"Could not find the vehicle paper '{req.Id}'.");
        }

        permit.Type = req.Type;
        permit.Number = req.Number;
        permit.Issuer = req.Issuer;
        permit.IssuedOn = req.IssuedOn;
        permit.SetExpiry(req.ExpiresOn);
        permit.Cost = req.Cost;
        permit.Notes = req.Notes;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
