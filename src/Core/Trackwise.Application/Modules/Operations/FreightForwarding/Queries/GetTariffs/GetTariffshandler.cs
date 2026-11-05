// ─────────────────────────────────────────────
// GetTariffsHandler.cs
// ─────────────────────────────────────────────
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetTariffsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTariffsQuery, PagedResult<TariffDto>>
{
    public async Task<PagedResult<TariffDto>> Handle(GetTariffsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Tariff>().Query().AsNoTracking();

        if (req.IsActive.HasValue)
            query = query.Where(t => t.IsActive == req.IsActive.Value);
        else if (req.OnlyActive)
            query = query.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search) ||
                (t.OriginLane != null && t.OriginLane.ToLower().Contains(search)) ||
                (t.DestinationLane != null && t.DestinationLane.ToLower().Contains(search)) ||
                t.Currency.ToLower().Contains(search) ||
                (t.ContractId != null && t.ContractId.ToString()!.ToLower().Contains(search)));
        }

        if (req.Mode.HasValue)
            query = query.Where(t => t.Mode == req.Mode.Value);

        if (req.ServiceLevel.HasValue)
            query = query.Where(t => t.ServiceLevel == req.ServiceLevel.Value);

        if (req.CarrierId.HasValue)
            query = query.Where(t => t.CarrierId == req.CarrierId.Value);

        if (req.ContractId.HasValue)
            query = query.Where(t => t.ContractId == req.ContractId.Value);

        if (!string.IsNullOrWhiteSpace(req.OriginLane))
            query = query.Where(t => t.OriginLane == req.OriginLane);

        if (!string.IsNullOrWhiteSpace(req.DestinationLane))
            query = query.Where(t => t.DestinationLane == req.DestinationLane);

        if (req.EffectiveOn.HasValue)
        {
            var date = req.EffectiveOn.Value;
            query = query.Where(t =>
                t.EffectiveFrom <= date &&
                (t.EffectiveTo == null || t.EffectiveTo >= date));
        }

        var totalItems = await query.CountAsync(ct);

        var tariffs = await query
            .OrderByDescending(t => t.EffectiveFrom)
            .ThenBy(t => t.Id)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new TariffDto
            {
                Id = t.Id,
                Name = t.Name,
                Mode = t.Mode,
                ServiceLevel = t.ServiceLevel,
                CarrierId = t.CarrierId,
                ContractId = t.ContractId,
                OriginLane = t.OriginLane,
                DestinationLane = t.DestinationLane,
                ContainerTypeId = t.ContainerTypeId,
                EffectiveFrom = t.EffectiveFrom,
                EffectiveTo = t.EffectiveTo,
                Version = t.Version,
                SupersedesTariffId = t.SupersedesTariffId,
                Currency = t.Currency,
                IsActive = t.IsActive,
                RateCount = t.Rates.Count
            })
            .ToListAsync(ct);

        // Resolved past the soft-delete filter, in one query for the page: projecting through
        // t.ContainerType would honour the filter and blank the equipment on every tariff priced
        // for a type a steward has since retired.
        var containerTypes = await ContainerTypeLookup.ResolveAsync(
            tenantUow, tariffs.Select(t => t.ContainerTypeId), ct);

        foreach (var tariff in tariffs)
        {
            if (tariff.ContainerTypeId is { } typeId
                && containerTypes.TryGetValue(typeId, out var type))
            {
                tariff.ContainerTypeCode = type.Code;
                tariff.ContainerTypeName = type.Name;
            }
        }

        return PagedResult<TariffDto>.Ok(tariffs, totalItems, req.PageSize);
    }
}
