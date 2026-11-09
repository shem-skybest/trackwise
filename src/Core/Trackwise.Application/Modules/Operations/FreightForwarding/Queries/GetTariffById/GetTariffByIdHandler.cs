// ─────────────────────────────────────────────
// GetTariffByIdHandler.cs
// ─────────────────────────────────────────────
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetTariffByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTariffByIdQuery, Result<TariffDto>>
{
    public async Task<Result<TariffDto>> Handle(GetTariffByIdQuery req, CancellationToken ct)
    {
        var tariff = await tenantUow.Repository<Tariff>()
            .Query()
            .AsNoTracking()
            .Where(t => t.Id == req.Id)
            .Select(t => new TariffDto
            {
                Id = t.Id,
                Name = t.Name,
                Mode = t.Mode,
                ServiceLevel = t.ServiceLevel,
                CarrierId = t.CarrierId,
                ContractId = t.ContractId,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer == null ? null : t.Customer.Name,
                OriginLane = t.OriginLane,
                DestinationLane = t.DestinationLane,
                ContainerTypeId = t.ContainerTypeId,
                EffectiveFrom = t.EffectiveFrom,
                EffectiveTo = t.EffectiveTo,
                Version = t.Version,
                SupersedesTariffId = t.SupersedesTariffId,
                Currency = t.Currency,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                Rates = t.Rates.Select(r => new TariffRateDto
                {
                    Id = r.Id,
                    TariffId = r.TariffId,
                    ChargeCodeId = r.ChargeCodeId,
                    ChargeCode = r.ChargeCode == null ? null : new ChargeCodeDto
                    {
                        Id = r.ChargeCode.Id,
                        Code = r.ChargeCode.Code,
                        Name = r.ChargeCode.Name,
                        Category = r.ChargeCode.Category,
                        DefaultUnit = r.ChargeCode.DefaultUnit,
                        IsActive = r.ChargeCode.IsActive
                    },
                    PricingModel = r.PricingModel,
                    BaseAmount = r.BaseAmount,
                    Unit = r.Unit,
                    Minimum = r.Minimum,
                    Maximum = r.Maximum,
                    FreeDays = r.FreeDays,
                    RateAfterFreeDays = r.RateAfterFreeDays,
                    Percentage = r.Percentage,
                    PercentageBase = r.PercentageBase,
                    BreakpointJson = r.BreakpointJson
                }).ToList(),
                History = t.History.Select(h => new TariffHistoryDto
                {
                    Id = h.Id,
                    TariffId = h.TariffId,
                    OldVersion = h.OldVersion,
                    NewVersion = h.NewVersion,
                    ChangedBy = h.ChangedBy,
                    ChangeReason = h.ChangeReason,
                    CreatedAt = h.CreatedAt
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (tariff is null)
        {
            return Result<TariffDto>.Fail($"Tariff with ID '{req.Id}' not found");
        }

        // Resolved separately rather than projected through t.ContainerType: the type is
        // soft-deletable, so the projection's join honours the global filter and would blank the
        // equipment on a tariff priced for a type a steward has since retired. The lookup reads
        // past the filter for exactly that reason.
        var containerTypes = await ContainerTypeLookup.ResolveAsync(
            tenantUow, [tariff.ContainerTypeId], ct);

        if (tariff.ContainerTypeId is { } typeId
            && containerTypes.TryGetValue(typeId, out var type))
        {
            tariff.ContainerTypeCode = type.Code;
            tariff.ContainerTypeName = type.Name;
        }

        return Result<TariffDto>.Ok(tariff);
    }
}
