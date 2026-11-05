using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// Read model for a Tariff, including its rates.
/// </summary>
public class TariffDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public FreightMode Mode { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public Guid? CarrierId { get; set; }
    public Guid? ContractId { get; set; }
    public string? OriginLane { get; set; }
    public string? DestinationLane { get; set; }
    public Guid? ContainerTypeId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? ContainerTypeCode { get; set; }

    public string? ContainerTypeName { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Version { get; set; }
    public Guid? SupersedesTariffId { get; set; }
    public string Currency { get; set; } = default!;
    public int RateCount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<TariffRateDto> Rates { get; set; } = [];
    public List<TariffHistoryDto> History { get; set; } = [];
}

/// <summary>
/// Lightweight version of TariffDto for list/table views (no rates/history).
/// </summary>
public class TariffSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public FreightMode Mode { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public string Currency { get; set; } = default!;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public class TariffRateDto
{
    public Guid Id { get; set; }
    public Guid TariffId { get; set; }
    public Guid ChargeCodeId { get; set; }
    public ChargeCodeDto? ChargeCode { get; set; }
    public PricingModel PricingModel { get; set; }
    public decimal BaseAmount { get; set; }
    public string Unit { get; set; } = default!;
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? FreeDays { get; set; }
    public decimal? RateAfterFreeDays { get; set; }
    public decimal? Percentage { get; set; }
    public string? PercentageBase { get; set; }
    public string? BreakpointJson { get; set; }
}

public class TariffHistoryDto
{
    public Guid Id { get; set; }
    public Guid TariffId { get; set; }
    public int OldVersion { get; set; }
    public int NewVersion { get; set; }
    public string? ChangedBy { get; set; }
    public string? ChangeReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Request body for creating a new Tariff (maps to Tariff.Create).
/// </summary>
public class CreateTariffRequest
{
    public Guid TenantId { get; set; }
    public FreightMode Mode { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string? OriginLane { get; set; }
    public string? DestinationLane { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public Guid? CarrierId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public Guid? ContractId { get; set; }
    // CreatedById typically comes from the authenticated user context, not the request body
}

/// <summary>
/// Request body for updating an existing Tariff (maps to Tariff.UpdateDetails).
/// All fields optional — only provided values are applied.
/// </summary>
public class UpdateTariffRequest
{
    public FreightMode? Mode { get; set; }
    public ServiceLevel? ServiceLevel { get; set; }
    public string? Name { get; set; }
    public string? Currency { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
    public string? OriginLane { get; set; }
    public string? DestinationLane { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public Guid? CarrierId { get; set; }
    public Guid? ContractId { get; set; }
}

/// <summary>
/// Request body for adding a rate to a Tariff (maps to Tariff.AddRate).
/// </summary>
public class AddTariffRateRequest
{
    public Guid ChargeCodeId { get; set; }
    public PricingModel PricingModel { get; set; }
    public decimal BaseAmount { get; set; }
    public string Unit { get; set; } = default!;
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? FreeDays { get; set; }
    public decimal? RateAfterFreeDays { get; set; }
    public decimal? Percentage { get; set; }
    public string? PercentageBase { get; set; }
    public string? BreakpointJson { get; set; }
}

/// <summary>
/// Request body for superseding a Tariff with a new version (maps to Tariff.Supersede).
/// </summary>
public class SupersedeTariffRequest
{
    public Guid NewTariffId { get; set; }
}
