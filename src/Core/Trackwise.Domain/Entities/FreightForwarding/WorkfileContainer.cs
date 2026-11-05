using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One container slot on a workfile: the box this move ships in, with what is true of it on this
/// move only (seal, weight, packages). A <see cref="Container"/> is the physical box and comes back
/// on later shipments, so none of this belongs on it.
/// <para>
/// <see cref="ContainerId"/> is optional because forwarding knows the size and count long before
/// the boxes: in nSFS only 7% of forwarding container rows carry a number (checked 2026-10-26) -
/// the numbers arrive with the truck leg that pulls the box out.
/// </para>
/// </summary>
public class WorkfileContainer : Entity, ITenantEntity
{
    public Guid WorkfileId { get; private set; }

    public int SortOrder { get; private set; }

    public Guid ContainerTypeId { get; private set; }

    /// <summary>The physical box from the container register, once its number is known.</summary>
    public Guid? ContainerId { get; private set; }

    public string? SealNumber { get; private set; }
    public decimal? GrossWeightKg { get; private set; }
    public int? PackageCount { get; private set; }
    public string? PackagingType { get; private set; }
    public decimal? Cbm { get; private set; }
    public string? Notes { get; private set; }

    internal static WorkfileContainer Create(Guid workfileId, int sortOrder, WorkfileContainerSpec spec)
    {
        var row = new WorkfileContainer { WorkfileId = workfileId, SortOrder = sortOrder };
        row.Apply(spec);
        return row;
    }

    internal void Apply(WorkfileContainerSpec spec)
    {
        if (spec.GrossWeightKg < 0 || spec.PackageCount < 0 || spec.Cbm < 0)
        {
            throw new InvalidOperationException("Weight, packages and CBM can't be negative.");
        }

        ContainerTypeId = spec.ContainerTypeId;
        ContainerId = spec.ContainerId;
        SealNumber = Clean(spec.SealNumber);
        GrossWeightKg = spec.GrossWeightKg;
        PackageCount = spec.PackageCount;
        PackagingType = Clean(spec.PackagingType);
        Cbm = spec.Cbm;
        Notes = Clean(spec.Notes);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
