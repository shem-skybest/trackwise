namespace Trackwise.Mappings;

/// <summary>Display names for a trip's truck and crew, resolved by the caller in a batch.</summary>
public sealed record TripRefNames(
    string? TruckNumber,
    string? DriverName = null,
    string? HelperName = null,
    string? SecondHelperName = null);
