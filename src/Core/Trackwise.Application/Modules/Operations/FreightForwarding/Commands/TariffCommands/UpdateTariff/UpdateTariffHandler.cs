using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.UpdateTariff;

internal sealed class UpdateTariffHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateTariffCommand, Result>
{
    public async Task<Result> Handle(UpdateTariffCommand req, CancellationToken ct)
    {
        var tariff = await tenantUow.Repository<Tariff>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (tariff is null)
        {
            return Result.Fail($"Tariff with ID '{req.Id}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            var mode = req.Mode ?? tariff.Mode;
            var serviceLevel = req.ServiceLevel ?? tariff.ServiceLevel;
            var contractId = req.ContractId ?? tariff.ContractId;
            var duplicate = await tenantUow.Repository<Tariff>().Query()
                .AnyAsync(i => i.Id != req.Id &&
                               i.Name == req.Name &&
                               i.Mode == mode &&
                               i.ServiceLevel == serviceLevel &&
                               i.ContractId == contractId &&
                               i.CustomerId == tariff.CustomerId &&
                               i.IsActive == tariff.IsActive, ct);

            if (duplicate)
            {
                return Result.Fail($"A tariff with name '{req.Name}' already exists.");
            }
        }

        tariff.UpdateDetails(
            mode: req.Mode,
            serviceLevel: req.ServiceLevel,
            name: req.Name,
            currency: req.Currency,
            effectiveFrom: req.EffectiveFrom,
            effectiveTo: req.EffectiveTo,
            isActive: null,
            originLane: req.OriginLane,
            destinationLane: req.DestinationLane,
            containerTypeId: req.ContainerTypeId,
            carrierId: req.CarrierId,
            contractId: req.ContractId);

        tenantUow.Repository<Tariff>().Update(tariff);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
