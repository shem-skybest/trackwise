using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public abstract partial class Workfile
{
    /// <summary>
    /// Whether ops may still change this file. A pending reference-closure request vetoes editing
    /// in every status, which is why this is not just a status check.
    /// </summary>
    public bool IsEditable => !IsReferenceLocked && WorkfileStatusMachine.IsEditable(Status);

    public void SubmitForApproval()
    {
        Transition(WorkfileStatus.ManagerReview);
        DomainEvents.Add(new WorkfileSubmittedForApproval(Id, ReferenceNumber));
    }

    /// <summary>Sends the file back to ops after a disapproval.</summary>
    public void ReturnToOps(string reason)
    {
        Transition(WorkfileStatus.Open);
        DomainEvents.Add(new WorkfileReturnedToOps(Id, reason));
    }

    /// <summary>
    /// Records that charges have left ops for Accounting. Called by the billing-forward flow once
    /// the charges themselves have been marked forwarded.
    /// </summary>
    public void MarkForwardedToAccounting()
    {
        Transition(WorkfileStatus.AccountingReview);
        IsForwardedToAccounting = true;
        ForwardedToAccountingAt = DateTimeOffset.UtcNow;
        DomainEvents.Add(new WorkfileForwardedToAccounting(Id, ReferenceNumber));
    }

    public void Close(string closedBy)
    {
        Transition(WorkfileStatus.Closed);
        ClosedBy = closedBy;
        ClosedAt = DateTimeOffset.UtcNow;
        DomainEvents.Add(new WorkfileClosed(Id, closedBy));
    }

    public void Cancel(string cancelledBy, string reason)
    {
        Transition(WorkfileStatus.Cancelled);
        CancelledBy = cancelledBy;
        CancelledAt = DateTimeOffset.UtcNow;
        CancellationReason = reason;
        DomainEvents.Add(new WorkfileCancelled(Id, cancelledBy, reason));
    }

    /// <summary>
    /// Locks the file while ops' closure request awaits Accounting. Ops cannot clear this itself -
    /// that is the whole point of the handshake.
    /// </summary>
    public void LockReference()
    {
        IsReferenceLocked = true;
        DomainEvents.Add(new WorkfileReferenceLocked(Id, ReferenceNumber));
    }

    public void ReleaseReference()
    {
        IsReferenceLocked = false;
        DomainEvents.Add(new WorkfileReferenceReleased(Id, ReferenceNumber));
    }

    public void SetAccountingRemarks(string? remarks) => AccountingRemarks = remarks;

    public void RecordGarageDeparture(DateTimeOffset at)
    {
        EnsureGarageTracking();
        GarageDepartureAt = at;
        DomainEvents.Add(new WorkfileGarageDepartureRecorded(Id, at));
    }

    public void RecordGarageArrival(DateTimeOffset at)
    {
        EnsureGarageTracking();
        GarageArrivalAt = at;
        DomainEvents.Add(new WorkfileGarageArrivalRecorded(Id, at));
    }

    private void EnsureGarageTracking()
    {
        if (!SupportsGarageTracking)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' is a {Type} file and does not track garage movements.");
        }
    }

    private void Transition(WorkfileStatus target)
    {
        if (!WorkfileStatusMachine.CanTransition(Status, target))
        {
            throw new InvalidOperationException(
                $"Cannot change workfile status from '{Status}' to '{target}'.");
        }

        Status = target;
    }
}
