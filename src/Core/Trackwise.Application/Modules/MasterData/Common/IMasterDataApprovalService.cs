using Trackwise.Domain.Core;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// The entity-agnostic half of maker-checker: raising a submission, deciding one, and enforcing
/// that the two are not the same person.
/// <para>
/// Shared for the reason the merge service is shared. Nothing about "who may approve this" varies
/// by entity - the policy already says whether an entity needs approval at all - so an entity that
/// grew its own copy would eventually grow its own subtly different answer to the one question the
/// whole mechanism exists to answer.
/// </para>
/// <para>
/// The caller loads the record and saves; this stages the request row and moves the record's
/// approval state. Keeping <c>SaveChangesAsync</c> with the handler matches every other write in
/// the module and lets a handler do entity-specific work in the same save.
/// </para>
/// </summary>
public interface IMasterDataApprovalService
{
    /// <summary>
    /// Sends a draft record to the approvers' queue.
    /// </summary>
    Task<Result> SubmitAsync(
        string entityType, IMasterDataApprovable record, string? note, CancellationToken ct = default);

    /// <summary>
    /// Accepts the open submission and takes the record live.
    /// <para>
    /// Fails with <see cref="ErrorCodes.MasterDataSelfApproval"/> when the current user raised the
    /// submission. That check is against the open request, not against a column on the record, so a
    /// record resubmitted by a second steward is approvable by the first.
    /// </para>
    /// </summary>
    Task<Result> ApproveAsync(
        string entityType, IMasterDataApprovable record, string? note, CancellationToken ct = default);

    /// <summary>
    /// Sends the record back to its steward with a reason. The record returns to draft - a rejected
    /// submission is not a dead end, it is work that is not finished.
    /// </summary>
    Task<Result> RejectAsync(
        string entityType, IMasterDataApprovable record, string reason, CancellationToken ct = default);
}
