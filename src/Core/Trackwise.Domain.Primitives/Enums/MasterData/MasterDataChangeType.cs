namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What a change-log entry records. Every master-data change is attributable to a named user with a
/// timestamp and a before/after value, so the log distinguishes how a record reached its state, not
/// only that it changed.
/// </summary>
public enum MasterDataChangeType
{
    Created = 0,
    Updated = 1,

    /// <summary>Soft-deleted. The row survives; the query filter stops surfacing it.</summary>
    Deleted = 2,

    Restored = 3,

    /// <summary>This record was merged into a surviving one and is now a tombstone.</summary>
    Merged = 4,

    /// <summary>Written by an ETL run rather than by a person - the actor is the sync job.</summary>
    Imported = 5,

    /// <summary>
    /// A steward sent the record to an approver. Logged as one event about the record rather than
    /// as an edit to the column carrying the approval state, the same way a delete is.
    /// </summary>
    Submitted = 6,

    /// <summary>A second person accepted the submission and the record went live.</summary>
    Approved = 7,

    /// <summary>
    /// An approver sent the record back to its steward. The record returns to draft, so the log
    /// entry is the only place the rejection survives - the reviewer's note lives on the approval
    /// request.
    /// </summary>
    Rejected = 8
}
