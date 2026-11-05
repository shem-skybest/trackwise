namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>A fund's header, as it is created or corrected.</summary>
public abstract class CashFundTermsCommand
{
    /// <summary>The employee holding the cash.</summary>
    public Guid CustodianId { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>The advance released, in the tenant's currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>The nSFS cash advance number.</summary>
    public string? AdvanceReference { get; set; }

    public string? Notes { get; set; }
}
