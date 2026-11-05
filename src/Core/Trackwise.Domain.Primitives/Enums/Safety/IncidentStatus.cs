namespace Trackwise.Domain.Primitives.Enums.Safety;

/// <summary>Open -> Under review -> Resolved -> Closed; a resolved incident can be reopened.</summary>
public enum IncidentStatus
{
    Open,
    UnderReview,
    Resolved,
    Closed
}
