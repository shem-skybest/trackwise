using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public class TruckGeolocationDto
{
    public required Guid TruckId { get; set; }
    public required Guid TenantId { get; set; }
    public required GeoPoint CurrentLocation { get; set; }
    public Address? CurrentAddress { get; set; }
    public string? TruckNumber { get; set; }
    public string? DriversName { get; set; }
}
