namespace Trackwise.Domain.Primitives.Enums;

/// <summary>Whose unit this is. A null ownership on a truck means nobody has confirmed it yet.</summary>
public enum TruckOwnership
{
    Owned,

    /// <summary>Rented or leased from another company, driven by our own crew.</summary>
    Leased,

    /// <summary>Another trucker's unit and crew, hired for the job.</summary>
    Subcontracted
}
