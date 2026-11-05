namespace Trackwise.Domain.Entities;

/// <summary>What a caller sets on a <see cref="WorkfileContainer"/>.</summary>
public sealed record WorkfileContainerSpec(
    Guid ContainerTypeId,
    Guid? ContainerId = null,
    string? SealNumber = null,
    decimal? GrossWeightKg = null,
    int? PackageCount = null,
    string? PackagingType = null,
    decimal? Cbm = null,
    string? Notes = null);
