using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One field's before/after value for one change to one master record.
/// <para>
/// Append-only and written by <c>ChangeLogSaveChangesInterceptor</c> rather than by handlers, so a
/// change is logged no matter which code path made it - including ETL runs and merges. A single
/// save that touches three fields writes three rows, which is what lets the UI show a field-level
/// history instead of an opaque "record updated".
/// </para>
/// </summary>
public class MasterDataChangeLog : Entity, ITenantEntity
{
    public required string EntityType { get; set; }

    public required Guid EntityId { get; set; }

    /// <summary>
    /// Record code as it stood at the time of the change, so the log stays readable after the
    /// record is merged away or renamed.
    /// </summary>
    public string? EntityCode { get; set; }

    public required MasterDataChangeType ChangeType { get; set; }

    /// <summary>
    /// Property name. Null for whole-record events (<see cref="MasterDataChangeType.Created"/>,
    /// <see cref="MasterDataChangeType.Deleted"/>, <see cref="MasterDataChangeType.Merged"/>),
    /// where there is no single field to attribute the change to.
    /// </summary>
    public string? Field { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    /// <summary>
    /// User id, or the sync job's name when the change came from an ETL run. Never null in
    /// practice - an anonymous write is a bug, not a valid state.
    /// </summary>
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the change arrived through a sync rather than a steward.</summary>
    public MasterDataSourceSystem? SourceSystem { get; set; }
}
