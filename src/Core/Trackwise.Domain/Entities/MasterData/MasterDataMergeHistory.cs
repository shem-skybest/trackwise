using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Records that a steward merged one master record into another, and what the losing record held
/// at the moment it was merged.
/// <para>
/// The snapshot matters: the loser survives only as a tombstone, so without the code and name
/// captured here a steward reviewing the merge later has no way to tell what was folded in. Pair
/// with <c>MasterDataXref</c>, which carries the forward resolution.
/// </para>
/// </summary>
public class MasterDataMergeHistory : Entity, ITenantEntity
{
    public required string EntityType { get; set; }

    /// <summary>The record that survived and now owns every reference the loser held.</summary>
    public required Guid WinnerId { get; set; }

    public required Guid LoserId { get; set; }

    /// <summary>Loser's code and name as they stood at merge time.</summary>
    public required string LoserCode { get; set; }

    public required string LoserName { get; set; }

    /// <summary>
    /// Per-field record of which side the steward kept, as JSON (<c>field -> "winner" | "loser"</c>).
    /// A merge is rarely "keep everything from the survivor" - the duplicate often holds the better
    /// address or the only TIN.
    /// </summary>
    public string? FieldChoices { get; set; }

    /// <summary>How many cross-reference rows were repointed - the reconciliation report's headline.</summary>
    public int XrefsRepointed { get; set; }

    /// <summary>
    /// How many rows inside this system had their foreign key moved to the survivor - loads,
    /// containers and the like. Counted separately from <see cref="XrefsRepointed"/> because the
    /// two answer different questions: that one is what other systems still resolve, this one is
    /// what this system stopped pointing at a tombstone.
    /// </summary>
    public int ReferencesRepointed { get; set; }

    public string? Reason { get; set; }

    public string? MergedBy { get; set; }

    public DateTime MergedAt { get; set; } = DateTime.UtcNow;
}
