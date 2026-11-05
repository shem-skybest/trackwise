using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>What Ops types for one fund expense - one row of Monday's liquidation board.</summary>
/// <param name="TripId">The trip it was spent on, when it was spent on one; office purchases have none.</param>
/// <param name="SpentById">Who paid, usually the trip's driver.</param>
public sealed record CashExpenseSpec(
    DateOnly SpentOn,
    CashExpenseCategory Category,
    string Description,
    decimal Amount,
    string? ReceiptNumber,
    Guid? TruckId,
    Guid? TripId,
    Guid? SpentById);
