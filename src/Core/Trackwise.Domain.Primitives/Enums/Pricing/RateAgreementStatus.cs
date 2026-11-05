namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Where a client's rate agreement is in its life. Only <see cref="Active"/> prices anything; a
/// revision is a new agreement, so the one it replaces becomes <see cref="Superseded"/> and keeps
/// pricing the trips that ran while it was in force.
/// </summary>
public enum RateAgreementStatus
{
    /// <summary>Being prepared; editable, prices nothing.</summary>
    Draft,

    /// <summary>Approved and in force from its effective date.</summary>
    Active,

    /// <summary>Replaced by a later revision; its effective period is closed.</summary>
    Superseded,

    /// <summary>Called off without a replacement.</summary>
    Withdrawn
}
