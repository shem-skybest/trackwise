using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a location. Carries no <c>Code</c>: it is the handle other systems join on and it survives
/// a merge, so a rename would strand every legacy key pointing at this record.
/// </summary>
public class UpdateLocationCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public LocationType Type { get; set; }
    public Address? Address { get; set; }

    /// <summary>The zone this location sits in, or null to take it out of every zone.</summary>
    public Guid? ParentLocationId { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>The client whose outlet this is, or null for a site any client can use.</summary>
    public Guid? CustomerId { get; set; }

    public TimeOnly? DefaultCutoffTime { get; set; }
    public int? ZoneRadiusMeters { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every load that references it; it only stops the location
    /// being offered for new work.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this location and is deactivating it
    /// anyway. Requires <see cref="DependencyReason"/>. Ignored while the location stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Location);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
