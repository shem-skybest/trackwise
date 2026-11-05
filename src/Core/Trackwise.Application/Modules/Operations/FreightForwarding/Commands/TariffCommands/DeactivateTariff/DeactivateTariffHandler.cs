using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.DeactivateTariff;

internal sealed class DeactivateTariffHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeactivateTariffCommand, Result>
{
    public async Task<Result> Handle(DeactivateTariffCommand req, CancellationToken ct)
    {
        var tariff = await tenantUow.Repository<Tariff>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (tariff is null)
        {
            return Result.Fail($"Tariff with ID '{req.Id}' not found.");
        }

        if (!tariff.IsActive)
        {
            return Result.Fail("Tariff is already inactive.");
        }

        var version = tariff.Version;
        tariff.Deactivate(req.EffectiveTo);
        tenantUow.Repository<Tariff>().Update(tariff);

        var history = TariffHistory.Create(
            tariffId: tariff.Id,
            oldVersion: version,
            newVersion: version,
            changedBy: null,
            changeReason: req.Reason ?? "Tariff deactivated");

        await tenantUow.Repository<TariffHistory>().AddAsync(history, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
