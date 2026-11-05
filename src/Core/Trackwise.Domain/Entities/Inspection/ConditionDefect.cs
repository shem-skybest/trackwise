using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A defect documented against a load's cargo during pickup or delivery inspection.
/// Mirrors the shape of <see cref="Safety.DvirDefect"/> but uses
/// <see cref="CargoInspectionPartCategory"/> (cargo-side parts) instead of the
/// truck-side <c>DvirInspectionCategory</c>.
/// </summary>
public class ConditionDefect : Entity, ITenantEntity
{
    public Guid LoadConditionReportId { get; set; }
    public virtual LoadConditionReport LoadConditionReport { get; set; } = null!;

    /// <summary>
    /// Which part of the cargo this defect affects. Must belong to the catalog
    /// returned by <c>CargoInspectionPartCategoryExtensions.GetCatalogFor(loadType)</c>
    /// for the parent report's load.
    /// </summary>
    public required CargoInspectionPartCategory PartCategory { get; set; }

    public required string Description { get; set; }

    public required DefectSeverity Severity { get; set; }
}
