using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One row of the approvers' queue: a record somebody submitted, and where it got to.
/// <para>
/// Deliberately entity-agnostic. "What is waiting on me" is one question across every approvable
/// list, and answering it per entity would mean the queue grew a new request each time an entity
/// joined the module - and an approver would have to know which lists exist to know they are done.
/// </para>
/// </summary>
public class MasterDataApprovalDto
{
    public Guid Id { get; set; }

    /// <summary>CLR type name of the record, e.g. <c>BusinessPartner</c>.</summary>
    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    /// <summary>Code and name as they stood when the record was submitted.</summary>
    public required string EntityCode { get; set; }

    public required string EntityName { get; set; }

    public string? SubmittedByName { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string? SubmissionNote { get; set; }

    /// <summary>Null while the request is open - which is exactly what puts it in the queue.</summary>
    public MasterDataApprovalDecision? Decision { get; set; }

    public string? ReviewedByName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    /// <summary>
    /// Whether the caller raised this submission, resolved server-side against their own identity.
    /// <para>
    /// Sent so the queue can say "you submitted this" rather than offering an Approve button that
    /// answers 409. The server refuses it regardless - this is the courtesy, not the control.
    /// </para>
    /// </summary>
    public bool SubmittedByMe { get; set; }
}
