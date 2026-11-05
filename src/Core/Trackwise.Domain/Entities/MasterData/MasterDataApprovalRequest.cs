using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One trip through maker-checker: a steward submitted a record, and an approver either accepted it
/// or sent it back.
/// <para>
/// A table rather than a set of columns on the record, for two reasons. A record can be submitted,
/// rejected and resubmitted any number of times, and columns remember only the last round - which
/// is precisely the history an auditor asks for. And "what is waiting for me" has to be answerable
/// across every approvable entity at once; reading it off each entity's own table would mean a
/// query per entity and a new one every time an entity joins the module.
/// </para>
/// <para>
/// <b>The submitter is stored here, not on the record</b>, because the rule is about this
/// submission: the approver of <i>this</i> request may not be the person who raised it.
/// </para>
/// </summary>
public class MasterDataApprovalRequest : Entity, ITenantEntity
{
    /// <summary>CLR type name of the record, e.g. <c>BusinessPartner</c>. Matches the policy key.</summary>
    public required string EntityType { get; set; }

    public required Guid EntityId { get; set; }

    /// <summary>
    /// Code and name as they stood when the record was submitted.
    /// <para>
    /// Snapshotted so the queue is one query. Reading them live would mean either a join per entity
    /// type - the thing this table exists to avoid - or a lookup per row, which is the N+1 every
    /// master-data list has already had to fix once.
    /// </para>
    /// </summary>
    public required string EntityCode { get; set; }

    public required string EntityName { get; set; }

    /// <summary>
    /// The steward's user id - the value the "approver may not be the submitter" check compares
    /// against, so it is an identity rather than a label.
    /// </summary>
    public required string SubmittedBy { get; set; }

    /// <summary>
    /// The steward's display name at submission time, snapshotted purely so the approver's queue
    /// reads as a list of people rather than a column of GUIDs. Never compared against anything -
    /// two people can share a name, and a rename must not change who submitted what.
    /// </summary>
    public string? SubmittedByName { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>What the steward wants the approver to know. Optional - most records speak for themselves.</summary>
    public string? SubmissionNote { get; set; }

    /// <summary>
    /// Null while the request is open. That nullability is the queue's filter, so nothing else is
    /// allowed to mean "still waiting".
    /// </summary>
    public MasterDataApprovalDecision? Decision { get; set; }

    public string? ReviewedBy { get; set; }

    /// <inheritdoc cref="SubmittedByName"/>
    public string? ReviewedByName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// The approver's note. Required on a rejection - a record sent back without a reason is a
    /// record the steward resubmits unchanged - and optional on an approval.
    /// </summary>
    public string? ReviewNote { get; set; }

    public bool IsOpen => Decision is null;
}
