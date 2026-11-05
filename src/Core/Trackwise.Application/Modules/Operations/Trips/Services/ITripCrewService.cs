using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Services;

/// <summary>The resolved crew for a trip: its driver and up to two helpers.</summary>
public sealed record TripCrew(Employee? Driver, Helper? Helper, Helper? SecondHelper);

/// <summary>Looks up a trip's crew by id and guards against double-booking people across trucks.</summary>
public interface ITripCrewService : IApplicationService
{
    /// <summary>Loads the driver and helpers; fails naming whichever id doesn't exist.</summary>
    Task<Result<TripCrew>> ResolveAsync(
        Guid? driverId, Guid? helperId, Guid? secondHelperId, CancellationToken ct = default);

    /// <summary>
    /// A message naming the conflict when anyone on <paramref name="trip"/> is already on another
    /// dispatched or in-transit trip on a <i>different</i> truck; null when there is none. The same
    /// truck's back-to-back trips (a "2nd trip") are not a conflict.
    /// </summary>
    Task<string?> FindCrewConflictAsync(Trip trip, CancellationToken ct = default);
}
