using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One thing paid for out of a <see cref="CashFund" />: a passway, a parking fee, a toll. Its
/// receipt goes from Ops to the office, which checks or disallows it.
/// </summary>
public class CashFundExpense : AuditableEntity, ITenantEntity
{
    public Guid CashFundId { get; private set; }

    public DateOnly SpentOn { get; private set; }
    public CashExpenseCategory Category { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string? ReceiptNumber { get; private set; }

    public Guid? TruckId { get; private set; }
    public virtual Truck? Truck { get; private set; }

    public Guid? TripId { get; private set; }
    public virtual Trip? Trip { get; private set; }

    /// <summary>The driver or office staff who paid.</summary>
    public Guid? SpentById { get; private set; }

    public virtual Employee? SpentBy { get; private set; }

    public CashExpenseStatus Status { get; private set; } = CashExpenseStatus.WithOps;

    /// <summary>The day the receipt went to the office: Monday's "Date Forward".</summary>
    public DateOnly? ForwardedOn { get; private set; }

    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? DisallowedReason { get; private set; }

    /// <summary>Counts against the fund: everything but a disallowed receipt, which the crew pays back.</summary>
    public bool CountsAgainstFund => Status != CashExpenseStatus.Disallowed;

    internal static CashFundExpense Create(Guid cashFundId, CashExpenseSpec spec)
    {
        var expense = new CashFundExpense { CashFundId = cashFundId };
        expense.Apply(spec);
        return expense;
    }

    internal void Update(CashExpenseSpec spec)
    {
        if (Status != CashExpenseStatus.WithOps)
        {
            throw new InvalidOperationException(
                $"'{Description}' was already forwarded to the office, so it can't be changed.");
        }

        Apply(spec);
    }

    internal void Forward(DateOnly on)
    {
        Status = CashExpenseStatus.Forwarded;
        ForwardedOn = on;
    }

    internal void Check(string reviewedBy, DateTime at)
    {
        EnsureReviewable();
        Status = CashExpenseStatus.Checked;
        DisallowedReason = null;
        ReviewedBy = reviewedBy;
        ReviewedAt = at;
    }

    internal void Disallow(string reason, string reviewedBy, DateTime at)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Say why the receipt is disallowed.");
        }

        EnsureReviewable();
        Status = CashExpenseStatus.Disallowed;
        DisallowedReason = reason.Trim();
        ReviewedBy = reviewedBy;
        ReviewedAt = at;
    }

    private void EnsureReviewable()
    {
        if (Status == CashExpenseStatus.WithOps)
        {
            throw new InvalidOperationException(
                $"'{Description}' is still with Ops. Forward its receipt to the office first.");
        }
    }

    private void Apply(CashExpenseSpec spec)
    {
        if (spec.Amount <= 0)
        {
            throw new InvalidOperationException("An expense needs an amount above zero.");
        }

        if (string.IsNullOrWhiteSpace(spec.Description))
        {
            throw new InvalidOperationException("Say what the expense was for.");
        }

        SpentOn = spec.SpentOn;
        Category = spec.Category;
        Description = spec.Description.Trim();
        Amount = spec.Amount;
        ReceiptNumber = string.IsNullOrWhiteSpace(spec.ReceiptNumber) ? null : spec.ReceiptNumber.Trim();
        TruckId = spec.TruckId;
        TripId = spec.TripId;
        SpentById = spec.SpentById;
    }
}
