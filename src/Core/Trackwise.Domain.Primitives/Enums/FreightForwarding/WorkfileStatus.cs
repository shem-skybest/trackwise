namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Where a workfile sits in the ops -> management -> accounting hand-off. Drives which fields are
/// editable; see <c>WorkfileStatusMachine</c> for the legal transitions.
/// </summary>
public enum WorkfileStatus
{
    Open = 0,
    ManagerReview = 1,
    AccountingReview = 2,
    Closed = 3,
    Cancelled = 4
}
