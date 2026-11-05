namespace Trackwise.Domain.Primitives.Enums;

public enum BillingBatchStatus
{
    /// <summary>Being prepared: trips and charges can still change.</summary>
    Draft = 0,

    /// <summary>Checked by Ops; the charges are frozen. Unlocking needs a reason.</summary>
    Locked = 1,

    /// <summary>The printed summary went to Accounting.</summary>
    Forwarded = 2,

    /// <summary>Accounting billed it in SAS; the invoice number is recorded.</summary>
    Billed = 3,

    /// <summary>Dropped before it was billed. Its reference stays used, as nSFS's "Cancelled Reference" did.</summary>
    Cancelled = 4
}
