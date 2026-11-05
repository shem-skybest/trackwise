using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Queries;

/// <summary>
/// Papers for one vehicle (give <see cref="TruckId" /> or <see cref="TrailerId" />, history included),
/// or across the whole fleet (neither given: current papers of active vehicles only - sold units'
/// lapsed registrations are nobody's problem). Sorted by expiry, soonest first.
/// </summary>
public class GetVehiclePermitsQuery : IQuery<Result<IList<VehiclePermitDto>>>
{
    public Guid? TruckId { get; set; }
    public Guid? TrailerId { get; set; }
    public VehiclePermitType? Type { get; set; }

    /// <summary>Only current papers expiring within this many days - already expired ones included.</summary>
    public int? ExpiringWithinDays { get; set; }
}
