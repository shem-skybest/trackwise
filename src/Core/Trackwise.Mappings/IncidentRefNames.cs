namespace Trackwise.Mappings;

/// <summary>Display names for what an incident references, batched by the caller.</summary>
public sealed record IncidentRefNames(
    long? TripNumber,
    string? CustomerName,
    string? TruckNumber,
    string? DriverName,
    string? HelperName,
    string? SecondHelperName);

/// <summary>Display names for what a rescue references, batched by the caller.</summary>
public sealed record RescueRefNames(
    long TripNumber,
    string? TruckNumber,
    string? DriverName,
    string? HelperName,
    string? SecondHelperName,
    string? IncidentCaseNumber);
