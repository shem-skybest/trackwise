using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Domain.Entities.Safety;

/// <summary>
/// Open -> Under review -> Resolved -> Closed. Review is optional; resolving needs the corrective
/// action, which is what the weekly report asks of every case. A resolved incident can be reopened
/// with a reason; a closed one is final.
/// </summary>
public partial class Incident
{
    public IncidentStatus Status { get; private set; }

    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }

    /// <summary>What was done about it, and what will stop it happening again.</summary>
    public string? CorrectiveAction { get; private set; }

    public string? ResolvedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>Why it was last reopened; kept after it is resolved again.</summary>
    public string? ReopenReason { get; private set; }

    public string? ClosedBy { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public void StartReview(string? notes, string reviewedBy, DateTime at)
    {
        if (Status != IncidentStatus.Open)
        {
            throw new InvalidOperationException("Only an open incident can be put under review.");
        }

        Status = IncidentStatus.UnderReview;
        ReviewNotes = Clean(notes);
        ReviewedBy = reviewedBy;
        ReviewedAt = at;
    }

    public void Resolve(string correctiveAction, string resolvedBy, DateTime at)
    {
        if (Status is not (IncidentStatus.Open or IncidentStatus.UnderReview))
        {
            throw new InvalidOperationException("Only an open incident or one under review can be resolved.");
        }

        CorrectiveAction = Clean(correctiveAction)
            ?? throw new InvalidOperationException("Say what was done about it before resolving it.");
        Status = IncidentStatus.Resolved;
        ResolvedBy = resolvedBy;
        ResolvedAt = at;
    }

    public void Reopen(string reason)
    {
        if (Status != IncidentStatus.Resolved)
        {
            throw new InvalidOperationException("Only a resolved incident can be reopened.");
        }

        ReopenReason = Clean(reason) ?? throw new InvalidOperationException("Say why the incident is being reopened.");
        Status = IncidentStatus.Open;
        ResolvedBy = null;
        ResolvedAt = null;
    }

    /// <summary>A report made by mistake can be deleted while it is open; after review it is part of the record.</summary>
    public void EnsureDeletable()
    {
        if (Status != IncidentStatus.Open || ReviewedAt is not null)
        {
            throw new InvalidOperationException("Only an open incident that was never reviewed can be deleted.");
        }
    }

    public void Close(string closedBy, DateTime at)
    {
        if (Status != IncidentStatus.Resolved)
        {
            throw new InvalidOperationException("Resolve the incident before closing it.");
        }

        Status = IncidentStatus.Closed;
        ClosedBy = closedBy;
        ClosedAt = at;
    }
}
