using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class TariffMapper
{
    /// <summary>
    /// Full read model including rates. History is intentionally excluded here —
    /// pull TariffHistoryDto separately if/when a history endpoint needs it.
    /// </summary>
    [MapperIgnoreSource(nameof(Tariff.DomainEvents))]
    [MapperIgnoreSource(nameof(Tariff.History))]
    [MapperIgnoreSource(nameof(Tariff.ContainerType))]
    [MapperIgnoreTarget(nameof(TariffDto.ContainerTypeCode))]
    [MapperIgnoreTarget(nameof(TariffDto.ContainerTypeName))]
    private static partial TariffDto ToDtoCore(this Tariff entity);

    /// <summary>
    /// Container-type display values are passed in rather than read off
    /// <c>entity.ContainerType</c>: lazy loading makes that nav a query per row, and the type is
    /// soft-deletable, so the nav also returns null for one a steward retired - blanking the
    /// equipment on tariffs that were priced for it.
    /// </summary>
    public static TariffDto ToDto(this Tariff entity, ContainerTypeRef? containerType = null)
    {
        var dto = entity.ToDtoCore();

        if (containerType is { } type)
        {
            dto.ContainerTypeCode = type.Code;
            dto.ContainerTypeName = type.Name;
        }

        return dto;
    }

    /// <summary>
    /// Trimmed projection for list/table views — no rates, no history.
    /// </summary>
    [MapperIgnoreSource(nameof(Tariff.DomainEvents))]
    [MapperIgnoreSource(nameof(Tariff.Rates))]
    [MapperIgnoreSource(nameof(Tariff.History))]
    [MapperIgnoreSource(nameof(Tariff.ContainerType))]
    [MapperIgnoreSource(nameof(Tariff.ContainerTypeId))]
    public static partial TariffSummaryDto ToSummaryDto(this Tariff entity);

    public static partial List<TariffSummaryDto> ToSummaryDtoList(this IEnumerable<Tariff> entities);

    public static partial TariffRateDto ToDto(this TariffRate entity);

    public static partial List<TariffRateDto> ToDtoList(this IEnumerable<TariffRate> entities);

    public static partial TariffHistoryDto ToDto(this TariffHistory entity);

    public static partial List<TariffHistoryDto> ToDtoList(this IEnumerable<TariffHistory> entities);

    /// <summary>
    /// Creates a new Tariff via the domain factory. Not a property-copy mapping —
    /// Mapperly can't generate this since Tariff has no public constructor/setters.
    /// CreatedById comes from the auth context, not the request body.
    /// </summary>
    public static Tariff ToEntity(this CreateTariffRequest request, Guid createdById)
    {
        return Tariff.Create(
            mode: request.Mode,
            serviceLevel: request.ServiceLevel,
            originLane: request.OriginLane,
            destinationLane: request.DestinationLane,
            name: request.Name,
            currency: request.Currency,
            containerTypeId: request.ContainerTypeId,
            carrierId: request.CarrierId,
            effectiveFrom: request.EffectiveFrom,
            contractId: createdById); // Adjust mapping here if contractId vs createdById differs in domain signature
    }

    /// <summary>
    /// Applies an update request onto an existing Tariff via its domain method.
    /// </summary>
    public static void ApplyUpdate(this Tariff entity, UpdateTariffRequest request)
    {
        entity.UpdateDetails(
            mode: request.Mode,
            serviceLevel: request.ServiceLevel,
            name: request.Name,
            currency: request.Currency,
            effectiveFrom: request.EffectiveFrom,
            effectiveTo: request.EffectiveTo,
            isActive: request.IsActive,
            originLane: request.OriginLane,
            destinationLane: request.DestinationLane,
            containerTypeId: request.ContainerTypeId,
            carrierId: request.CarrierId,
            contractId: request.ContractId);
    }

    /// <summary>
    /// Adds a rate to an existing Tariff via its domain method and returns the new rate's DTO.
    /// </summary>
    public static TariffRateDto AddRate(this Tariff entity, AddTariffRateRequest request)
    {
        var rate = entity.AddRate(
            chargeCodeId: request.ChargeCodeId,
            pricingModel: request.PricingModel,
            baseAmount: request.BaseAmount,
            unit: request.Unit,
            minimum: request.Minimum,
            maximum: request.Maximum,
            freeDays: request.FreeDays,
            rateAfterFreeDays: request.RateAfterFreeDays,
            percentage: request.Percentage,
            percentageBase: request.PercentageBase,
            breakpointJson: request.BreakpointJson);

        return rate.ToDto();
    }
}
