using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public record VehiclePermitDto
{
    public Guid Id { get; set; }

    /// <summary>Exactly one of <see cref="TruckId" /> and <see cref="TrailerId" /> is set.</summary>
    public Guid? TruckId { get; set; }

    public Guid? TrailerId { get; set; }

    /// <summary>The vehicle's code (truck number or trailer code), resolved in one batched lookup.</summary>
    public string? VehicleCode { get; set; }

    public string? VehicleName { get; set; }
    public VehiclePermitType Type { get; set; }
    public string? Number { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    /// <summary>Negative once expired.</summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Whether this is the latest-expiring paper of its type for the vehicle. An older, superseded
    /// record is history, and its expiry date is no longer a problem.
    /// </summary>
    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }
}
