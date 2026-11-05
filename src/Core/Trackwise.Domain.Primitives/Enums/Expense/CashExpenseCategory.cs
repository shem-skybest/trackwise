namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What a crew paid for out of the operational fund - the kinds that fill Monday's CCL OPERATION
/// FUNDS LIQUIDATION board. Fuel isn't here: it goes on the fleet card, not the fund.
/// </summary>
public enum CashExpenseCategory
{
    /// <summary>A warehouse, mall or DC's entry fee: passway, entry pass, road user fee.</summary>
    Passway = 0,

    Parking = 1,
    Toll = 2,

    /// <summary>A courier sending documents, e.g. Lalamove.</summary>
    Courier = 3,

    Other = 4
}
