using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

internal static class QuotationRouteResolver
{
    public static List<RouteStopDto> ResolveStops(LaneTransitDto lane)
    {
        if (lane.Stops is { Count: > 0 })
            return lane.Stops.OrderBy(s => s.Sequence).ToList();

        if (lane.OriginLocationId.HasValue && lane.DestinationLocationId.HasValue)
        {
            return
            [
                new RouteStopDto(1, lane.OriginLocationId.Value, StopType.Pickup,
                    lane.RequestedPickupWindowStart, null, "Initial Origin Pickup"),
                new RouteStopDto(2, lane.DestinationLocationId.Value, StopType.Dropoff,
                    null, lane.RequestedDeliveryWindowEnd, "Final Delivery Dropoff")
            ];
        }

        return [];
    }

    /// Returns an error message, or null when every location is valid.
    public static string? ValidateLocations(
        IReadOnlyCollection<Guid> locationIds,
        Dictionary<Guid, Location> locations,
        Guid customerId)
    {
        foreach (var locId in locationIds)
        {
            if (!locations.TryGetValue(locId, out var loc) || !loc.IsActive || loc.DeletedAt.HasValue)
                return $"Location '{locId}' is invalid, inactive, or soft-deleted.";

            if (loc.MergedIntoId.HasValue)
                return $"Location '{loc.Code}' has been merged into another site and cannot be used.";

            if (loc.Type == LocationType.Zone)
                return $"Location '{loc.Code}' is a Zone grouping. Stops must reference physical facilities or delivery points.";

            if (loc.CustomerId.HasValue && loc.CustomerId.Value != customerId)
                return $"Private Location '{loc.Code}' does not belong to Customer '{customerId}'.";
        }

        return null;
    }

    /// <summary>
    /// Computes cumulative road distance across all consecutive legs (Stop 1 -> Stop 2 -> ... -> Stop N).
    /// </summary>
    public static decimal? CalculateRouteDistance(
        List<RouteStopDto> orderedStops,
        Dictionary<Guid, Location> locations)
    {
        decimal accumulatedDistance = 0m;

        for (int i = 0; i < orderedStops.Count - 1; i++)
        {
            var currentLoc = locations[orderedStops[i].LocationId];
            var nextLoc = locations[orderedStops[i + 1].LocationId];

            if (!currentLoc.Latitude.HasValue || !currentLoc.Longitude.HasValue ||
                !nextLoc.Latitude.HasValue || !nextLoc.Longitude.HasValue)
            {
                return null; // Missing surveyed GPS coordinates on at least one leg
            }

            accumulatedDistance += QuotationRouteResolver.CalculateHaversineDistanceKm(
                currentLoc.Latitude.Value, currentLoc.Longitude.Value,
                nextLoc.Latitude.Value, nextLoc.Longitude.Value);
        }

        return Math.Round(accumulatedDistance, 2);
    }


    public static decimal CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(earthRadiusKm * c);
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}
