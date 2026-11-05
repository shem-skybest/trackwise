using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A cash advance Ops holds for a period - Cold Chain's "operational funds" of ₱15,000 per half
/// month - and what the crews paid out of it (passways, parking, tolls). Accounting releases the
/// advance through nSFS (<see cref="AdvanceReference" />); this is where it is spent and liquidated,
/// replacing Monday's CCL OPERATION FUNDS LIQUIDATION board.
/// <para>
/// Liquidating needs every receipt reviewed by the office. The balance is calculated, never typed:
/// the advance less everything that counts against it.
/// </para>
/// </summary>
public class CashFund : AuditableEntity, ITenantEntity
{
    private string? advanceReference;
    private string? notes;

    /// <summary>Who holds the cash and answers for it.</summary>
    public Guid CustodianId { get; private set; }

    public virtual Employee Custodian { get; private set; } = null!;

    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>The advance released, in the tenant's currency.</summary>
    public decimal Amount { get; private set; }

    /// <summary>The nSFS cash advance number, e.g. <c>CA-RFV2609111546-235-M</c>.</summary>
    public string? AdvanceReference
    {
        get => advanceReference;
        private set => advanceReference = Clean(value);
    }

    public string? Notes
    {
        get => notes;
        set => notes = Clean(value);
    }

    public CashFundStatus Status { get; private set; } = CashFundStatus.Open;
    public string? LiquidatedBy { get; private set; }
    public DateTime? LiquidatedAt { get; private set; }

    public virtual List<CashFundExpense> Expenses { get; private set; } = [];

    /// <summary>Everything that counts against the advance, reviewed or not.</summary>
    public decimal SpentTotal => Expenses.Where(e => e.CountsAgainstFund).Sum(e => e.Amount);

    /// <summary>Disallowed receipts: the crew owes these back.</summary>
    public decimal DisallowedTotal => Expenses.Where(e => !e.CountsAgainstFund).Sum(e => e.Amount);

    /// <summary>Cash that should be left: positive is returned, negative is owed to the custodian.</summary>
    public decimal Balance => Amount - SpentTotal;

    public static CashFund Create(
        Guid custodianId, DateOnly periodStart, DateOnly periodEnd, decimal amount, string? advanceReference)
    {
        var fund = new CashFund { CustodianId = custodianId };
        fund.SetTerms(periodStart, periodEnd, amount, advanceReference);
        return fund;
    }

    public void UpdateTerms(
        Guid custodianId, DateOnly periodStart, DateOnly periodEnd, decimal amount, string? advanceReference)
    {
        EnsureOpen();
        CustodianId = custodianId;
        SetTerms(periodStart, periodEnd, amount, advanceReference);
    }

    public CashFundExpense AddExpense(CashExpenseSpec spec)
    {
        EnsureOpen();
        var expense = CashFundExpense.Create(Id, spec);
        Expenses.Add(expense);
        return expense;
    }

    public void UpdateExpense(Guid expenseId, CashExpenseSpec spec)
    {
        EnsureOpen();
        Find(expenseId).Update(spec);
    }

    /// <summary>Takes a line off the fund; the caller deletes the row it returns.</summary>
    public CashFundExpense RemoveExpense(Guid expenseId)
    {
        EnsureOpen();
        var expense = Find(expenseId);
        if (expense.Status != CashExpenseStatus.WithOps)
        {
            throw new InvalidOperationException(
                $"'{expense.Description}' was already forwarded to the office, so it can't be deleted.");
        }

        Expenses.Remove(expense);
        return expense;
    }

    /// <summary>
    /// Sends receipts still with Ops to the office - the named ones, or all of them when none are
    /// named. Returns how many went.
    /// </summary>
    public int Forward(IReadOnlyCollection<Guid> expenseIds, DateOnly on)
    {
        EnsureOpen();
        var lines = expenseIds.Count == 0
            ? Expenses.Where(e => e.Status == CashExpenseStatus.WithOps).ToList()
            : expenseIds.Select(Find).ToList();

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("There are no receipts with Ops to forward.");
        }

        if (lines.FirstOrDefault(e => e.Status != CashExpenseStatus.WithOps) is { } sent)
        {
            throw new InvalidOperationException($"'{sent.Description}' was already forwarded.");
        }

        foreach (var line in lines)
        {
            line.Forward(on);
        }

        return lines.Count;
    }

    /// <summary>The office accepts the receipts. A disallowed one can be accepted after all.</summary>
    public void Check(IReadOnlyCollection<Guid> expenseIds, string reviewedBy, DateTime at)
    {
        EnsureOpen();
        foreach (var line in Pick(expenseIds))
        {
            line.Check(reviewedBy, at);
        }
    }

    public void Disallow(IReadOnlyCollection<Guid> expenseIds, string reason, string reviewedBy, DateTime at)
    {
        EnsureOpen();
        foreach (var line in Pick(expenseIds))
        {
            line.Disallow(reason, reviewedBy, at);
        }
    }

    public void Liquidate(string liquidatedBy, DateTime at)
    {
        EnsureOpen();

        var pending = Expenses.Count(e => e.Status is CashExpenseStatus.WithOps or CashExpenseStatus.Forwarded);
        if (pending > 0)
        {
            throw new InvalidOperationException(
                $"{pending} receipt(s) haven't been checked by the office yet.");
        }

        Status = CashFundStatus.Liquidated;
        LiquidatedBy = liquidatedBy;
        LiquidatedAt = at;
    }

    /// <summary>Opens a liquidated fund again, e.g. for a receipt that turned up late.</summary>
    public void Reopen()
    {
        if (Status != CashFundStatus.Liquidated)
        {
            throw new InvalidOperationException("Only a liquidated fund can be reopened.");
        }

        Status = CashFundStatus.Open;
        LiquidatedBy = null;
        LiquidatedAt = null;
    }

    private void SetTerms(DateOnly periodStart, DateOnly periodEnd, decimal amount, string? reference)
    {
        if (periodEnd < periodStart)
        {
            throw new InvalidOperationException("The period can't end before it starts.");
        }

        if (amount < 0)
        {
            throw new InvalidOperationException("The advance can't be negative.");
        }

        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Amount = amount;
        AdvanceReference = reference;
    }

    private List<CashFundExpense> Pick(IReadOnlyCollection<Guid> expenseIds)
    {
        if (expenseIds.Count == 0)
        {
            throw new InvalidOperationException("Pick the receipts to review.");
        }

        return expenseIds.Select(Find).ToList();
    }

    private CashFundExpense Find(Guid expenseId) =>
        Expenses.FirstOrDefault(e => e.Id == expenseId)
        ?? throw new InvalidOperationException("That expense isn't on this fund.");

    private void EnsureOpen()
    {
        if (Status != CashFundStatus.Open)
        {
            throw new InvalidOperationException("This fund is liquidated. Reopen it to change it.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
