namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What an extra charge on a billing batch is for - the quotation terms ops bill case by case,
/// on top of the truck-trip rates.
/// </summary>
public enum BillingChargeKind
{
    Demurrage = 0,
    Backload = 1,
    DifferentLocation = 2,

    /// <summary>Tolls, parking and passways billed at cost - non-VAT, refunded by the client.</summary>
    TollsAndParking = 3,

    Other = 4
}
