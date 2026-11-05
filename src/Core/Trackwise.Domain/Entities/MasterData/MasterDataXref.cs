using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Maps a row in a legacy system to the canonical master record built from it.
/// <para>
/// This table is what makes the module's central promise survivable: after two branches' duplicate
/// customers are merged, every historical reference from every system still resolves, because the
/// losing record's cross-reference rows are repointed at the survivor rather than deleted. Without
/// it a merge would orphan whatever the legacy systems still hold.
/// </para>
/// <para>
/// A canonical record may have many cross-reference rows - one per source row that folded into it.
/// The reverse is not allowed: a given (system, table, key) resolves to exactly one record, which
/// the unique index enforces.
/// </para>
/// </summary>
public class MasterDataXref : AuditableEntity, ITenantEntity
{
    public required MasterDataSourceSystem SourceSystem { get; set; }

    /// <summary>Legacy table the row came from, verbatim - e.g. <c>TBL_PORT</c>, <c>TBL_DRIVER_LIST</c>.</summary>
    public required string SourceTable { get; set; }

    /// <summary>
    /// The legacy row's primary key as text. Legacy keys are variously int, char and - in the
    /// tables that join people by <c>FNAME + ' ' + LNAME</c> - a name string, so they are stored as
    /// written rather than coerced. Composite keys are joined with <see cref="KeySeparator"/>.
    /// </summary>
    public required string SourceKey { get; set; }

    /// <summary>CLR type name of the canonical entity, e.g. <c>Terminal</c>.</summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// The canonical record this key currently resolves to. Repointed by a merge; never null.
    /// </summary>
    public required Guid EntityId { get; set; }

    /// <summary>
    /// Free-text note from the ETL run that created the row - why it matched, or why it was
    /// judged a duplicate. Feeds the reconciliation report.
    /// </summary>
    public string? MatchNote { get; set; }

    /// <summary>When the sync job last confirmed this mapping still holds in the source system.</summary>
    public DateTime? LastSeenAt { get; set; }

    public const string KeySeparator = "|";

    /// <summary>Builds a composite source key from a legacy row's key columns, in declaration order.</summary>
    public static string BuildKey(params ReadOnlySpan<string?> parts)
    {
        var trimmed = new string[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            // Legacy columns are riddled with untrimmed padding - nSFS SQL is full of
            // rtrim(ltrim(...)) for exactly this reason. Normalise here so the same logical key
            // never lands twice under two spellings.
            trimmed[i] = parts[i]?.Trim() ?? "";
        }

        return string.Join(KeySeparator, trimmed);
    }
}
