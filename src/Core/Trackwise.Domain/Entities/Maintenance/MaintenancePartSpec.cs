namespace Trackwise.Domain.Entities.Maintenance;

/// <summary>One part line as typed: what it is and what it cost, before it belongs to a record.</summary>
public record MaintenancePartSpec(
    string PartName,
    string? Description,
    string? PartNumber,
    int Quantity,
    decimal UnitCost);
