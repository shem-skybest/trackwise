using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.CreateTariff;

internal sealed class CreateTariffHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateTariffCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTariffCommand req, CancellationToken ct)
    {
        var existing = await tenantUow.Repository<Tariff>()
            .GetAsync(i => i.Name == req.Name, ct);

        if (existing is not null)
        {
            return Result<Guid>.Fail($"A tariff with name '{req.Name}' already exists.");
        }

        var tariff = Tariff.Create(
            mode: req.Mode,
            serviceLevel: req.ServiceLevel,
            name: req.Name,
            currency: req.Currency,
            originLane: req.OriginLane,
            destinationLane: req.DestinationLane,
            containerTypeId: req.ContainerTypeId,
            carrierId: req.CarrierId,
            effectiveFrom: req.EffectiveFrom,
            contractId: req.ContractId);

        await tenantUow.Repository<Tariff>().AddAsync(tariff, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(tariff.Id);
    }
}
