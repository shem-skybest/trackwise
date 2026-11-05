using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a location - a delivery point, warehouse, yard, or the zone that groups them.
/// </summary>
public class CreateLocationCommand : ICommand<Result<LocationDto>>, IMasterDataWriteCommand
{
    /// <summary>Leave blank to have a code allocated from the <c>Location</c> number series.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;
    public LocationType Type { get; set; }

    /// <summary>
    /// Where it is. Omitted for a zone, which is a grouping rather than somewhere a truck goes -
    /// the validator refuses one on a zone rather than storing an address that means nothing.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>The zone this location sits in, or null when it belongs to none.</summary>
    public Guid? ParentLocationId { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this location.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>The client whose outlet this is, or null for a site any client can use.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>When a delivery here is normally due. Pre-fills the load's deliver-by time.</summary>
    public TimeOnly? DefaultCutoffTime { get; set; }

    /// <summary>Arrival geofence radius in metres, for GPS-driven arrival times.</summary>
    public int? ZoneRadiusMeters { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// place. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Location);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["Name"] = Name
    };
}
