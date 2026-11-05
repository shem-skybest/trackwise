using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>One container slot on a workfile. The number is blank until the box is known.</summary>
public class WorkfileContainerDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public Guid ContainerTypeId { get; set; }
    public string? ContainerTypeCode { get; set; }
    public string? ContainerTypeName { get; set; }
    public Guid? ContainerId { get; set; }
    public string? ContainerNumber { get; set; }
    public string? SealNumber { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public int? PackageCount { get; set; }
    public string? PackagingType { get; set; }
    public decimal? Cbm { get; set; }
    public string? Notes { get; set; }

    /// <summary>Where the box is on this move; filled on forwarding files only.</summary>
    public ContainerMoveStatus MoveStatus { get; set; }

    /// <summary>The port, site or place the status refers to.</summary>
    public string? MoveStatusPlace { get; set; }

    /// <summary>When the box reached its current status.</summary>
    public DateTime? MoveStatusSince { get; set; }
}
