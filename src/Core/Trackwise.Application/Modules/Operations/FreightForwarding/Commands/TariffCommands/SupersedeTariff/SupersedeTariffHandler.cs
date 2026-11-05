using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.SupersedeTariff;

internal sealed class SupersedeTariffHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SupersedeTariffCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SupersedeTariffCommand req, CancellationToken ct)
    {
        var source = await tenantUow.Repository<Tariff>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (source is null)
        {
            return Result<Guid>.Fail($"Tariff with ID '{req.Id}' not found.");
        }

        if (!source.IsActive)
        {
            return Result<Guid>.Fail("Only an active tariff can be superseded.");
        }

        var replacement = Tariff.CreateSuperseding(source, req.EffectiveFrom, req.EffectiveTo);

        await tenantUow.Repository<Tariff>().AddAsync(replacement, ct);

        foreach (var sourceRate in source.Rates)
        {
            var replacementRate = replacement.AddRate(
                chargeCodeId: sourceRate.ChargeCodeId,
                pricingModel: sourceRate.PricingModel,
                baseAmount: sourceRate.BaseAmount,
                unit: sourceRate.Unit,
                minimum: sourceRate.Minimum,
                maximum: sourceRate.Maximum,
                freeDays: sourceRate.FreeDays,
                rateAfterFreeDays: sourceRate.RateAfterFreeDays,
                percentage: sourceRate.Percentage,
                percentageBase: sourceRate.PercentageBase,
                breakpointJson: sourceRate.BreakpointJson);

            await tenantUow.Repository<TariffRate>().AddAsync(replacementRate, ct);
        }

        source.Deactivate(req.EffectiveFrom.AddDays(-1));
        tenantUow.Repository<Tariff>().Update(source);

        var history = TariffHistory.Create(
            tariffId: source.Id,
            oldVersion: source.Version,
            newVersion: replacement.Version,
            changedBy: null,
            changeReason: req.Reason ?? $"Superseded by tariff {replacement.Id}");

        await tenantUow.Repository<TariffHistory>().AddAsync(history, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(replacement.Id);
    }
}
