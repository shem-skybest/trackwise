namespace Trackwise.Domain.Core;

/// <summary>
///     Marks an entity that is never physically deleted. A global query filter hides deleted rows,
///     so ordinary queries need no <c>Where</c> clause of their own.
///     <para>
///     Deliberately separate from an entity's <c>IsActive</c> flag: "deactivated" is a business
///     state a steward can reverse, "deleted" is a record that should no longer surface at all.
///     Legacy nSFS conflates the two into a single <c>stat</c> column - that conflation is absorbed
///     by the compatibility layer and must not reach the domain.
///     </para>
/// </summary>
public interface ISoftDeletable
{
    /// <summary>When the row was soft-deleted; <c>null</c> for live rows.</summary>
    DateTime? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}
