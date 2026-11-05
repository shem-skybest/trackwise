using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

public class CreateTripCommand : ICommand<Result<Guid>>
{
    public string Name { get; set; } = null!;
    public Guid? TruckId { get; set; }
    public Guid? HelperId { get; set; }

    /// <summary>
    /// Who drives this trip. When no crew is given at all, the trip takes the crew planned for its
    /// truck that day, and failing that the truck's main driver.
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>A second helper, for trucks that run with two.</summary>
    public Guid? SecondHelperId { get; set; }
    public IEnumerable<CreateTripLoadCommand>? NewLoads { get; set; }
    public IEnumerable<Guid>? AttachedLoadIds { get; set; }
    public IEnumerable<TripStopDto>? OptimizedStops { get; set; }

    /// <summary>
    ///     Total distance from the route optimizer (in kilometers).
    /// </summary>
    public double? TotalDistance { get; set; }
}
