using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A place that is not a port: a delivery point, a warehouse, a yard, or the zone that groups
/// them. Legacy source is <c>TBL_DELIVERY_LOC_CONFIG</c>, plus the free-text <c>Origin</c> and
/// <c>Destination</c> columns on <c>TBL_CUSTOMER_SERVICE</c> that never had a lookup table.
/// <para>
/// Kept apart from <see cref="Terminal"/> rather than folded into it: a port is a point on a
/// shipping schedule with a UN/LOCODE and a country, a delivery point is a street address inside a
/// zone. Merging the two would give every record half a set of fields that mean nothing to it.
/// </para>
/// </summary>
public class Location : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Location</c> number series"; immutable afterwards.
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a location code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    public required LocationType Type { get; set; }

    /// <summary>
    /// Where it is. Nullable because a <see cref="LocationType.Zone"/> has no address - it is a
    /// grouping, and giving it the address of one of its members is how a zone quietly becomes a
    /// duplicate delivery point.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// The zone this location sits in, or null for a location that belongs to no zone.
    /// <para>
    /// Self-referencing, which is what makes this the first entity that can depend on itself: the
    /// dependency probe stops a steward retiring a zone while delivery points still point at it,
    /// and a merge repoints the children the same way it repoints any other foreign key. Cycles are
    /// refused on write - see <c>LocationAncestry</c>.
    /// </para>
    /// </summary>
    public Guid? ParentLocationId { get; set; }

    public virtual Location? ParentLocation { get; set; }

    /// <summary>Latitude in decimal degrees, where the site has been surveyed. Null means unknown, not zero.</summary>
    public double? Latitude { get; set; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double? Longitude { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// The client this site belongs to - one of their outlets, stores or schools - or null for a
    /// shared site any client's load can use (a public cold store, a port-side warehouse).
    /// <para>
    /// Deliberately not unique per address: two clients delivering to the same mall are two
    /// outlets with their own cut-off times, and folding them into one record would make every
    /// client-specific field a fight.
    /// </para>
    /// </summary>
    public Guid? CustomerId { get; set; }

    public virtual Customer? Customer { get; set; }

    /// <summary>
    /// The time a delivery here is normally due by. A default only: the load's own
    /// <c>RequestedDeliveryDate</c> is what dispatch is held to, and picking this site pre-fills it.
    /// </summary>
    public TimeOnly? DefaultCutoffTime { get; set; }

    /// <summary>
    /// Radius in metres a truck must be inside to count as arrived. Null until someone sets one -
    /// the GPS arrival integration will read it, and guessing a default would geofence a mall and
    /// a sari-sari store the same way.
    /// </summary>
    public int? ZoneRadiusMeters { get; set; }

    /// <summary>Owning branch, or null when every branch uses this location.</summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the location is still offered for new work. Distinct from deletion: a site that
    /// closes keeps every load that ever delivered there.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
