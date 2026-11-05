using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
///     Expenses related to a specific truck (fuel, maintenance, tires, registration, tolls, parking).
/// </summary>
public class TruckExpense : Expense
{
    public override ExpenseType Type { get; set; } = ExpenseType.Truck;

    /// <summary>
    ///     The truck this expense is for.
    /// </summary>
    public required Guid TruckId { get; set; }

    /// <summary>
    ///     Navigation property for the truck.
    /// </summary>
    public virtual Truck Truck { get; set; } = null!;

    /// <summary>
    ///     Category of the truck expense.
    /// </summary>
    public TruckExpenseCategory Category { get; set; }

    /// <summary>
    ///     Odometer reading at the time of expense (optional).
    /// </summary>
    public int? OdometerReading { get; set; }

    /// <summary>
    ///     Quantity purchased (typically fuel volume). Captured in <see cref="QuantityUnit"/>;
    ///     never auto-converted between units.
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    ///     Unit the <see cref="Quantity"/> is expressed in (Gallons / Liters).
    /// </summary>
    public VolumeUnit? QuantityUnit { get; set; }

    /// <summary>
    ///     Jurisdiction (country + state/province) where the purchase happened.
    ///     Set by fuel-card sync or manual entry; feeds IFTA per-jurisdiction gallons.
    /// </summary>
    public TaxJurisdiction? PurchaseJurisdiction { get; set; }

    /// <summary>
    ///     Fuel card provider this expense was imported from, when auto-created by sync.
    /// </summary>
    public FuelCardProviderType? FuelCardProvider { get; set; }

    /// <summary>
    ///     Provider-side transaction ID for expenses imported from a fuel card (idempotency).
    /// </summary>
    public string? ExternalTransactionId { get; set; }

    /// <summary>
    ///     Unit price paid (per gallon/liter), when known.
    /// </summary>
    public decimal? PricePerUnit { get; set; }

    /// <summary>
    ///     The fleet card a fill-up was charged to; null when it was paid some other way.
    /// </summary>
    public FleetCard? FleetCard { get; set; }

    /// <summary>
    ///     The trip this expense was for, when known.
    /// </summary>
    public Guid? TripId { get; set; }

    public virtual Trip? Trip { get; set; }

    public static TruckExpense ForFuel(FuelFillUp fillUp)
    {
        var expense = new TruckExpense
        {
            TruckId = fillUp.TruckId,
            Amount = fillUp.Amount,
            ExpenseDate = fillUp.FilledAt,
            Category = TruckExpenseCategory.Fuel
        };
        expense.Apply(fillUp);
        return expense;
    }

    /// <summary>
    ///     Corrects a fill-up. A flagged one goes back for review, since the correction is what the
    ///     reviewer asked for.
    /// </summary>
    public void CorrectFuel(FuelFillUp fillUp)
    {
        if (Category != TruckExpenseCategory.Fuel)
        {
            throw new InvalidOperationException("Only a fuel expense has a fill-up to correct.");
        }

        if (Status is ExpenseStatus.Approved or ExpenseStatus.Paid)
        {
            throw new InvalidOperationException("A verified fill-up can't be changed.");
        }

        Apply(fillUp);
        ReopenForReview();
    }

    private void Apply(FuelFillUp fillUp)
    {
        TruckId = fillUp.TruckId;
        TripId = fillUp.TripId;
        ExpenseDate = fillUp.FilledAt;
        Amount = fillUp.Amount;
        Quantity = fillUp.Liters;
        QuantityUnit = VolumeUnit.Liters;
        PricePerUnit = Math.Round(fillUp.Amount.Amount / fillUp.Liters, 4);
        FleetCard = fillUp.FleetCard;
        OdometerReading = fillUp.OdometerReading;
        VendorName = fillUp.Station;
        Notes = fillUp.Notes;
    }
}
