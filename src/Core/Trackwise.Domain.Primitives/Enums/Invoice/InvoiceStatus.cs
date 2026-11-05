namespace Trackwise.Domain.Primitives.Enums;

public enum InvoiceStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Issued,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}
