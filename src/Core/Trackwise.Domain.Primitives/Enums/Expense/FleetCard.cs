namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The fleet card a fill-up was charged to. Each brand bills its own monthly statement, which is
/// why the fuel log totals per card.
/// </summary>
public enum FleetCard
{
    Shell = 0,
    Petron = 1,
    Unioil = 2
}
