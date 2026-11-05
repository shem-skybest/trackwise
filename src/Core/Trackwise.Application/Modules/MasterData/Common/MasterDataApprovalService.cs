using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Common;

internal sealed class MasterDataApprovalService(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    IMasterDataPolicyProvider policies) : IMasterDataApprovalService
{
    public async Task<Result> SubmitAsync(
        string entityType, IMasterDataApprovable record, string? note, CancellationToken ct = default)
    {
        if (Ungoverned(entityType) is { } refusal)
        {
            return refusal;
        }

        if (record.ApprovalState is MasterDataApprovalState.Pending)
        {
            return Result.Fail($"{record.Code} is already waiting for an approver.");
        }

        if (record.ApprovalState is MasterDataApprovalState.Approved)
        {
            return Result.Fail($"{record.Code} is already approved and live.");
        }

        var submitter = Actor();

        if (submitter is null)
        {
            // Without an identity there is nobody for the approver to be different from, which is
            // the entire guarantee. Refusing is the only honest answer - an anonymous submission
            // approvable by anyone is worse than no submission.
            return Result.Fail("Could not identify who is submitting this record.");
        }

        await tenantUow.Repository<MasterDataApprovalRequest>().AddAsync(
            new MasterDataApprovalRequest
            {
                EntityType = entityType,
                EntityId = record.Id,

                // Snapshotted so the queue renders without a join per entity type.
                EntityCode = record.Code,
                EntityName = record.Name,
                SubmittedBy = submitter,
                SubmittedByName = currentUser.GetUserName(),
                SubmissionNote = note
            },
            ct);

        record.ApprovalState = MasterDataApprovalState.Pending;

        return Result.Ok();
    }

    public async Task<Result> ApproveAsync(
        string entityType, IMasterDataApprovable record, string? note, CancellationToken ct = default)
    {
        var (request, failure) = await OpenRequestAsync(entityType, record, ct);

        if (failure is not null)
        {
            return failure;
        }

        Decide(request!, MasterDataApprovalDecision.Approved, note);

        record.ApprovalState = MasterDataApprovalState.Approved;

        // The only place IsActive is set on an approvable record. Everything else that could set it
        // - the create handler, the update command - deliberately cannot, because "live" and
        // "approved" have to be the same event or the approval is decoration.
        record.IsActive = true;

        return Result.Ok();
    }

    public async Task<Result> RejectAsync(
        string entityType, IMasterDataApprovable record, string reason, CancellationToken ct = default)
    {
        var (request, failure) = await OpenRequestAsync(entityType, record, ct);

        if (failure is not null)
        {
            return failure;
        }

        Decide(request!, MasterDataApprovalDecision.Rejected, reason);

        // Back to where it started rather than into a state of its own: the steward corrects the
        // record and submits it again, and the rejection survives on the request row and in the
        // change log.
        record.ApprovalState = MasterDataApprovalState.Draft;

        return Result.Ok();
    }

    /// <summary>
    /// Finds the submission being decided, and applies the two rules every decision shares: there
    /// has to be one open, and the person deciding cannot be the person who raised it.
    /// </summary>
    private async Task<(MasterDataApprovalRequest? Request, Result? Failure)> OpenRequestAsync(
        string entityType, IMasterDataApprovable record, CancellationToken ct)
    {
        if (Ungoverned(entityType) is { } refusal)
        {
            return (null, refusal);
        }

        var request = await tenantUow.Repository<MasterDataApprovalRequest>().Query()
            .Where(r => r.EntityType == entityType && r.EntityId == record.Id && r.Decision == null)
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefaultAsync(ct);

        if (request is null)
        {
            return (null, Result.Fail(
                $"{record.Code} has not been submitted for approval, so there is nothing to decide."));
        }

        var reviewer = Actor();

        if (reviewer is null)
        {
            return (null, Result.Fail("Could not identify who is reviewing this record."));
        }

        if (string.Equals(reviewer, request.SubmittedBy, StringComparison.Ordinal))
        {
            return (null, Result.Fail(
                $"{record.Code} was submitted by you. Master data that needs approval has to be "
                + "reviewed by somebody else - ask another approver to look at it.",
                ErrorCodes.MasterDataSelfApproval));
        }

        return (request, null);
    }

    private void Decide(MasterDataApprovalRequest request, MasterDataApprovalDecision decision, string? note)
    {
        request.Decision = decision;
        request.ReviewedBy = Actor();
        request.ReviewedByName = currentUser.GetUserName();
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNote = note;
    }

    /// <summary>
    /// Refuses an entity whose policy does not ask for approval, rather than quietly creating a
    /// request nothing will ever look at. Approval is declared in <c>MasterDataPolicies</c>, and an
    /// endpoint that worked for an entity the table says needs no approval would be a second answer
    /// to a question the table already answers.
    /// </summary>
    private Result? Ungoverned(string entityType) =>
        policies.Find(entityType)?.RequiresApproval is true
            ? null
            : Result.Fail($"{entityType} records do not go through approval.");

    private string? Actor() => currentUser.GetUserId()?.ToString();
}
