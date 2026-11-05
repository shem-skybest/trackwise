namespace Trackwise.Domain.Entities;

/// <summary>A rescue as the office reports it: the truck and crew sent, the drops they took over, when and why.</summary>
public sealed record RescueSpec(
    Truck Truck,
    Employee Driver,
    Helper? Helper,
    Helper? SecondHelper,
    IReadOnlyCollection<Guid> StopIds,
    DateTime TransferredAt,
    string Reason,
    string? Place = null,
    Guid? IncidentId = null);
