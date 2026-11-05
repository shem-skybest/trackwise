using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.AddTariffRate;

internal sealed class AddTariffRateHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AddTariffRateCommand, Result<TariffRateDto>>
{
    public async Task<Result<TariffRateDto>> Handle(AddTariffRateCommand req, CancellationToken ct)
    {
        var tariff = await tenantUow.Repository<Tariff>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.TariffId, ct);

        if (tariff is null)
        {
            return Result<TariffRateDto>.Fail($"Tariff with ID '{req.TariffId}' not found.");
        }

        var chargeCode = await tenantUow.Repository<ChargeCode>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.ChargeCodeId && i.IsActive, ct);

        if (chargeCode is null)
        {
            return Result<TariffRateDto>.Fail($"Active charge code with ID '{req.ChargeCodeId}' not found.");
        }

        var rate = tariff.AddRate(
            chargeCodeId: chargeCode.Id,
            pricingModel: req.PricingModel,
            baseAmount: req.BaseAmount,
            unit: req.Unit.Trim(),
            minimum: req.Minimum,
            maximum: req.Maximum,
            freeDays: req.FreeDays,
            rateAfterFreeDays: req.RateAfterFreeDays,
            percentage: req.Percentage,
            percentageBase: req.PercentageBase,
            breakpointJson: req.BreakpointJson);

        await tenantUow.Repository<TariffRate>().AddAsync(rate, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<TariffRateDto>.Ok(new TariffRateDto
        {
            Id = rate.Id,
            TariffId = rate.TariffId,
            ChargeCodeId = rate.ChargeCodeId,
            ChargeCode = new ChargeCodeDto
            {
                Id = chargeCode.Id,
                Code = chargeCode.Code,
                Name = chargeCode.Name,
                Category = chargeCode.Category,
                DefaultUnit = chargeCode.DefaultUnit,
                IsActive = chargeCode.IsActive
            },
            PricingModel = rate.PricingModel,
            BaseAmount = rate.BaseAmount,
            Unit = rate.Unit,
            Minimum = rate.Minimum,
            Maximum = rate.Maximum,
            FreeDays = rate.FreeDays,
            RateAfterFreeDays = rate.RateAfterFreeDays,
            Percentage = rate.Percentage,
            PercentageBase = rate.PercentageBase,
            BreakpointJson = rate.BreakpointJson
        });
    }
}
