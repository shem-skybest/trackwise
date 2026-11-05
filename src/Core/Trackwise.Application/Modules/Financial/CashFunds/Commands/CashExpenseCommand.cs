using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Financial.CashFunds.Commands;

/// <summary>One fund expense as Ops types it.</summary>
public abstract class CashExpenseCommand
{
    /// <summary>The fund it is paid from. Taken from the route.</summary>
    public Guid CashFundId { get; set; }

    public DateOnly SpentOn { get; set; }
    public CashExpenseCategory Category { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? ReceiptNumber { get; set; }

    /// <summary>Taken from the trip when omitted.</summary>
    public Guid? TruckId { get; set; }

    public Guid? TripId { get; set; }

    /// <summary>Who paid; the trip's driver when omitted.</summary>
    public Guid? SpentById { get; set; }

    internal CashExpenseSpec ToSpec() =>
        new(SpentOn, Category, Description, Amount, ReceiptNumber, TruckId, TripId, SpentById);
}
