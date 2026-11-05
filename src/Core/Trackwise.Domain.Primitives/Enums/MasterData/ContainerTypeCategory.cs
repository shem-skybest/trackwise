using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What kind of box or equipment a <c>ContainerType</c> is, independent of its size.
/// <para>
/// Kept as an enum rather than promoted to its own list because, unlike the container types
/// themselves, these are the ISO 6346 type groups - a closed set defined by a standard nobody
/// here maintains. A steward adding a new container type picks one; they never add one.
/// </para>
/// </summary>
public enum ContainerTypeCategory
{
    /// <summary>Standard dry box - ISO type group G. Most of the fleet.</summary>
    [Description("General Purpose")]
    GeneralPurpose = 0,

    /// <summary>Refrigerated - ISO type group R. The cold chain.</summary>
    [Description("Reefer")]
    Reefer = 1,

    /// <summary>Open top - ISO type group U, for cargo loaded by crane.</summary>
    [Description("Open Top")]
    OpenTop = 2,

    /// <summary>Flat rack and platform - ISO type group P, for out-of-gauge cargo.</summary>
    [Description("Flat Rack")]
    FlatRack = 3,

    /// <summary>Tank - ISO type group T, and road tankers, which carry no ISO code at all.</summary>
    [Description("Tank")]
    Tank = 4,

    /// <summary>
    /// Bulk. The one category with no box: it describes cargo moved loose rather than the
    /// equipment moving it, and arrived here from the load board's vocabulary rather than ISO's.
    /// </summary>
    [Description("Bulk")]
    Bulk = 5
}
