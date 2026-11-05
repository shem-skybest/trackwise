namespace Trackwise.Application.Modules.Operations.FreightForwarding.Containers;

/// <summary>What adding and editing a workfile container slot share.</summary>
public abstract class WorkfileContainerFields
{
    public Guid ContainerTypeId { get; set; }

    /// <summary>
    /// The box's ISO 6346 number, once known. A number the register doesn't have yet is added to it.
    /// </summary>
    public string? ContainerNumber { get; set; }

    public string? SealNumber { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public int? PackageCount { get; set; }
    public string? PackagingType { get; set; }
    public decimal? Cbm { get; set; }
    public string? Notes { get; set; }
}
