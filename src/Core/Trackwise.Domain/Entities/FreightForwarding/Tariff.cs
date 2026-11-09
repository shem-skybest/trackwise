using System.Collections.ObjectModel;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a versioned tariff for freight forwarding.
/// </summary>
public class Tariff : AuditableEntity, ITenantEntity
{
    public string Name { get; private set; } = default!;         // e.g., "Asia-US West Coast Q4 2026"
    public FreightMode Mode { get; private set; }
    public ServiceLevel ServiceLevel { get; private set; }
    public Guid? CarrierId { get; private set; }
    public Guid? ContractId { get; private set; }                // optional customer contract
    public Guid? CustomerId { get; private set; }
    public virtual Customer? Customer { get; private set; }
    public string? OriginLane { get; private set; }              // ZIP / port UN/LOCODE
    public string? DestinationLane { get; private set; }
    public Guid? ContainerTypeId { get; private set; }         // master container-type list

    public virtual ContainerType? ContainerType { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public int Version { get; private set; }
    public Guid? SupersedesTariffId { get; private set; }
    public string Currency { get; private set; } = default!;     // ISO 4217
    public bool IsActive { get; private set; }

    private readonly List<TariffRate> _rates = [];
    public virtual IReadOnlyCollection<TariffRate> Rates => new ReadOnlyCollection<TariffRate>(_rates);

    private readonly List<TariffHistory> _history = [];
    public virtual IReadOnlyCollection<TariffHistory> History => new ReadOnlyCollection<TariffHistory>(_history);

    /// <summary>
    /// Creates a new tariff.
    /// </summary>
    public static Tariff Create(
        FreightMode mode,
        ServiceLevel serviceLevel,
        string name,
        string currency,
        string? originLane,
        string? destinationLane,
        Guid? containerTypeId,
        Guid? carrierId,
        DateOnly effectiveFrom,
        Guid? contractId,
        Guid? customerId = null)
    {
        var t = new Tariff
        {
            Mode = mode,
            ServiceLevel = serviceLevel,
            Name = name,
            Currency = currency.ToUpperInvariant(),
            OriginLane = originLane,
            DestinationLane = destinationLane,
            ContainerTypeId = containerTypeId,
            CarrierId = carrierId,
            ContractId = contractId,
            CustomerId = customerId,
            EffectiveFrom = effectiveFrom,
            Version = 1,
            IsActive = true
        };
        t.DomainEvents.Add(new TariffCreated(t.Id, name, mode, serviceLevel));
        return t;
    }

    public static Tariff CreateSuperseding(Tariff source, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        var replacement = new Tariff
        {
            Name = source.Name,
            Mode = source.Mode,
            ServiceLevel = source.ServiceLevel,
            CarrierId = source.CarrierId,
            ContractId = source.ContractId,
            CustomerId = source.CustomerId,
            OriginLane = source.OriginLane,
            DestinationLane = source.DestinationLane,
            ContainerTypeId = source.ContainerTypeId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Version = source.Version + 1,
            SupersedesTariffId = source.Id,
            Currency = source.Currency,
            IsActive = true
        };
        replacement.DomainEvents.Add(new TariffCreated(replacement.Id, replacement.Name, replacement.Mode, replacement.ServiceLevel));
        return replacement;
    }

    /// <summary>
    /// Adds a rate to the tariff.
    /// </summary>
    public TariffRate AddRate(
        Guid chargeCodeId,
        PricingModel pricingModel,
        decimal baseAmount,
        string unit,
        decimal? minimum,
        decimal? maximum,
        int? freeDays,
        decimal? rateAfterFreeDays,
        decimal? percentage,
        string? percentageBase,
        string? breakpointJson)
    {
        var rate = new TariffRate
        {
            TariffId = Id,
            ChargeCodeId = chargeCodeId,
            PricingModel = pricingModel,
            BaseAmount = baseAmount,
            Unit = unit,
            Minimum = minimum,
            Maximum = maximum,
            FreeDays = freeDays,
            RateAfterFreeDays = rateAfterFreeDays,
            Percentage = percentage,
            PercentageBase = percentageBase,
            BreakpointJson = breakpointJson
        };
        _rates.Add(rate);
        DomainEvents.Add(new TariffRateAdded(Id, chargeCodeId, pricingModel));
        return rate;
    }

    public void Deactivate(DateOnly? effectiveTo = null)
    {
        IsActive = false;
        if (effectiveTo.HasValue) EffectiveTo = effectiveTo.Value;
        DomainEvents.Add(new TariffDeactivated(Id, EffectiveTo));
    }

    public void UpdateDetails(
        FreightMode? mode,
        ServiceLevel? serviceLevel,
        string? name,
        string? currency,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo,
        bool? isActive,
        string? originLane,
        string? destinationLane,
        Guid? containerTypeId,
        Guid? carrierId,
        Guid? contractId)
    {
        if (mode.HasValue) Mode = mode.Value;
        if (serviceLevel.HasValue) ServiceLevel = serviceLevel.Value;
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(currency)) Currency = currency.ToUpperInvariant();
        if (effectiveFrom.HasValue) EffectiveFrom = effectiveFrom.Value;
        if (effectiveTo.HasValue) EffectiveTo = effectiveTo.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (!string.IsNullOrWhiteSpace(originLane)) OriginLane = originLane;
        if (!string.IsNullOrWhiteSpace(destinationLane)) DestinationLane = destinationLane;
        if (containerTypeId.HasValue) ContainerTypeId = containerTypeId.Value;
        if (carrierId.HasValue) CarrierId = carrierId.Value;
        if (contractId.HasValue) ContractId = contractId.Value;
        DomainEvents.Add(new TariffUpdated(Id, Name));
    }
}

// Domain events
public record TariffCreated(Guid Id, string Name, FreightMode Mode, ServiceLevel ServiceLevel) : IDomainEvent;
public record TariffUpdated(Guid Id, string Name) : IDomainEvent;
public record TariffSuperseded(Guid Id, Guid NewTariffId, int NewVersion) : IDomainEvent;
public record TariffDeactivated(Guid Id, DateOnly? EffectiveTo) : IDomainEvent;
public record TariffRateAdded(Guid TariffId, Guid ChargeCodeId, PricingModel PricingModel) : IDomainEvent;
public record TariffHistoryCreated(Guid Id, Guid TariffId, int OldVersion, int NewVersion) : IDomainEvent;
