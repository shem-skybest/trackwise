namespace Trackwise.Domain.Primitives.Enums;

/// <summary>Where a fund expense's receipt is: Monday's "Status" and "MNL Office" columns in one.</summary>
public enum CashExpenseStatus
{
    /// <summary>Recorded; the receipt is still with Ops.</summary>
    WithOps = 0,

    /// <summary>The receipt went to the office for checking.</summary>
    Forwarded = 1,

    /// <summary>The office accepted it: it counts against the fund.</summary>
    Checked = 2,

    /// <summary>The office refused it; the crew owes the amount back.</summary>
    Disallowed = 3
}
