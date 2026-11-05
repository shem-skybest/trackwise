namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Which side called a booking off. Monday keeps "Cancelled" and "Cancelled by Client" as separate
/// statuses; here it is one status plus who cancelled.
/// </summary>
public enum CancellationParty
{
    Company = 0,
    Client = 1
}
