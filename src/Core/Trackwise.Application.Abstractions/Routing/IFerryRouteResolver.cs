using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Routing;

/// <summary>
/// Resolves a multi-leg drive+ferry route between two points that may sit on different Philippine
/// islands with no road connection between them. Mapbox's Directions/Matrix APIs are driving-only
/// and silently fabricate a road path across open ocean for such pairs (they return `code: "Ok"`
/// with a real-looking but nonexistent route) - this resolver is the source of truth instead.
/// </summary>
public interface IFerryRouteResolver
{
    /// <summary>
    /// Returns the ferry-aware route, or null when both points are on the same known island (or
    /// at least one point falls outside the modeled island network) - callers should fall back to
    /// ordinary driving directions/haversine distance in that case.
    /// </summary>
    FerryAwareRouteDto? TryResolve(GeoPoint origin, GeoPoint destination);
}
