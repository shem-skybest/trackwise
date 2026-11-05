using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// An operational fund and its liquidation. Totals are calculated from its expenses;
/// <see cref="Expenses" /> is empty on list reads.
/// </summary>
public record CashFundDto
{
    public Guid Id { get; set; }
    public Guid CustodianId { get; set; }
    public string? CustodianName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public string? AdvanceReference { get; set; }
    public string? Notes { get; set; }
    public CashFundStatus Status { get; set; }
    public string? LiquidatedBy { get; set; }
    public DateTime? LiquidatedAt { get; set; }

    /// <summary>Everything counting against the advance: all but disallowed receipts.</summary>
    public decimal SpentTotal { get; set; }

    /// <summary>Disallowed receipts, owed back by the crew.</summary>
    public decimal DisallowedTotal { get; set; }

    /// <summary>The advance less <see cref="SpentTotal" />: positive is returned, negative is owed to the custodian.</summary>
    public decimal Balance { get; set; }

    public int ExpenseCount { get; set; }
    public int WithOpsCount { get; set; }
    public int ForwardedCount { get; set; }
    public IReadOnlyList<CashFundExpenseDto> Expenses { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
