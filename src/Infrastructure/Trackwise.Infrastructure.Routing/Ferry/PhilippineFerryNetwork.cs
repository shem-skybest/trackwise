using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Infrastructure.Routing.Ferry;

/// <summary>
/// The islands Skybest Logistics currently operates branches on (see `skybest-branches.ts` on the
/// Angular side: Manila, Cebu/Mandaue, Bacolod, Cagayan de Oro, Davao). Not a general Philippines
/// landmass classifier - just enough to route between these 5 branches.
/// </summary>
internal enum PhilippineIsland
{
    Luzon,
    Cebu,
    Negros,
    Mindanao
}

/// <summary>
/// A simple lat/lon bounding box used to classify a point onto one of the 4 known islands. Rough
/// by design - accurate near the 5 known Skybest branches, not meant to resolve points near the
/// (nearby, unmodeled) straits between islands.
/// </summary>
internal readonly record struct IslandBounds(double MinLon, double MaxLon, double MinLat, double MaxLat)
{
    public bool Contains(GeoPoint point)
    {
        return point.Longitude >= MinLon && point.Longitude <= MaxLon &&
               point.Latitude >= MinLat && point.Latitude <= MaxLat;
    }
}

internal sealed record FerryPort(string Id, string Name, GeoPoint Location, PhilippineIsland Island);

/// <summary>
/// A real, sourced RoRo/cargo ferry crossing (drive-on/drive-off - trucks board directly, no
/// transload). Distance/duration figures are public route data, not live schedules - refresh by
/// hand if an operator materially changes a route.
/// </summary>
internal sealed record FerryCrossing(
    string PortAId,
    string PortBId,
    double DistanceMeters,
    double DurationSeconds,
    string Source);

/// <summary>
/// Static PH inter-island port/crossing data backing <see cref="FerryRouteResolver" />. Adding a
/// new lane is adding one <see cref="FerryPort" /> (if it's a new port) and one
/// <see cref="FerryCrossing" /> here - same tier as `skybest-branches.ts` on the frontend.
/// </summary>
internal static class PhilippineFerryNetwork
{
    public static readonly IReadOnlyDictionary<PhilippineIsland, IslandBounds> IslandBoundingBoxes =
        new Dictionary<PhilippineIsland, IslandBounds>
        {
            // Main Luzon landmass - separated from the others purely by latitude, so the
            // longitude range can stay generous without risking cross-island misclassification.
            [PhilippineIsland.Luzon] = new(MinLon: 119.5, MaxLon: 124.6, MinLat: 12.0, MaxLat: 19.0),
            [PhilippineIsland.Cebu] = new(MinLon: 123.3, MaxLon: 124.3, MinLat: 9.4, MaxLat: 11.3),
            [PhilippineIsland.Negros] = new(MinLon: 122.3, MaxLon: 123.3, MinLat: 9.0, MaxLat: 10.9),
            [PhilippineIsland.Mindanao] = new(MinLon: 121.8, MaxLon: 126.6, MinLat: 5.0, MaxLat: 9.6),
        };

    // Port coordinates are town/pier-level approximations (same precision tier as
    // skybest-branches.ts's branch coordinates) - accurate enough for routing/distance, not for
    // turn-by-turn dock navigation.
    public static readonly IReadOnlyList<FerryPort> Ports =
    [
        new FerryPort("manila-north-harbor", "Manila North Harbor", new GeoPoint(120.9578, 14.6091),
            PhilippineIsland.Luzon),
        new FerryPort("cebu-city-pier", "Cebu City Pier (Pier 1)", new GeoPoint(123.9058, 10.2971),
            PhilippineIsland.Cebu),
        new FerryPort("toledo-port", "Toledo City Port", new GeoPoint(123.6389, 10.3766),
            PhilippineIsland.Cebu),
        new FerryPort("san-carlos-port", "San Carlos City Port", new GeoPoint(123.4162, 10.4907),
            PhilippineIsland.Negros),
        new FerryPort("cdo-macabalan-port", "Cagayan de Oro (Macabalan) Port", new GeoPoint(124.6389, 8.4966),
            PhilippineIsland.Mindanao),
        new FerryPort("davao-sasa-wharf", "Davao (Sasa) Wharf", new GeoPoint(125.6444, 7.1128),
            PhilippineIsland.Mindanao),
    ];

    // Sourced from public RoRo/cargo route data (MARINA nautical highway matrix; 2GO, Trans-Asia
    // and Lite Shipping/FastCat route pages), not live schedules. Durations are the vendor's
    // published average; verify against the operator you actually contract with before using a
    // number here to price a lane.
    public static readonly IReadOnlyList<FerryCrossing> Crossings =
    [
        new FerryCrossing("manila-north-harbor", "cebu-city-pier",
            DistanceMeters: 417_000, DurationSeconds: 23 * 3600,
            Source: "2GO Manila-Cebu RoRo/cargo route, ~225 nm / ~23 h"),
        new FerryCrossing("manila-north-harbor", "davao-sasa-wharf",
            DistanceMeters: 1_625_000, DurationSeconds: 59.5 * 3600,
            Source: "2GO Manila-Davao RoRo/cargo route (Masikap/Masinag), ~1,625 km / ~59.5 h"),
        new FerryCrossing("toledo-port", "san-carlos-port",
            DistanceMeters: 28_000, DurationSeconds: 1.75 * 3600,
            Source: "Lite Shipping/FastCat Toledo-San Carlos RoRo, 28 km / ~1.5-2 h"),
        new FerryCrossing("cebu-city-pier", "cdo-macabalan-port",
            DistanceMeters: 168_000, DurationSeconds: 8 * 3600,
            Source: "2GO/Trans-Asia Cebu-Cagayan de Oro RoRo/cargo route, ~168 km / ~8 h (2GO; other operators run up to ~12 h)"),
    ];
}
