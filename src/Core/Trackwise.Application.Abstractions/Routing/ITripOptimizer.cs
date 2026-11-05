using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Routing;

namespace Trackwise.Application.Abstractions.Routing;

/// <summary>
///     Service for optimizing trip routes.
/// </summary>
public interface ITripOptimizer
{
    Task<IReadOnlyList<TripStopDto>> OptimizeAsync(OptimizeTripParams @params, CancellationToken ct = default);
}

public record OptimizeTripParams(
    IReadOnlyList<TripStopDto> Stops,
    GeoPoint? Start,
    GeoPoint? End,
    int VehicleCapacity);
