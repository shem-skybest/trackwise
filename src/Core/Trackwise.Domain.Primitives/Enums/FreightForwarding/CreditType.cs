namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How the customer settles this file. Pairs with <c>Workfile.CreditTermDays</c>.
/// </summary>
public enum CreditType
{
    Cash = 0,
    Charge = 1,
    Prepaid = 2,
    Collect = 3
}
