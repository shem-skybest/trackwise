using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Routing.Ferry;
using Xunit;

namespace Trackwise.Infrastructure.Routing.Tests;

public class FerryRouteResolverTests
{
    private static readonly FerryRouteResolver sut = new();

    // Skybest branch coordinates (see skybest-branches.ts) - Manila/Luzon, Cebu, Bacolod/Negros,
    // Cagayan de Oro & Davao/Mindanao.
    private static readonly GeoPoint Manila = new(120.976429, 14.597762);
    private static readonly GeoPoint Cebu = new(123.94273, 10.326905);
    private static readonly GeoPoint Bacolod = new(122.970524, 10.670661);
    private static readonly GeoPoint CagayanDeOro = new(124.664505, 8.482775);
    private static readonly GeoPoint Davao = new(125.624062, 7.098687);

    [Fact]
    public void TryResolve_ManilaToDavao_UsesDirectCrossing()
    {
        var route = sut.TryResolve(Manila, Davao);

        Assert.NotNull(route);
        Assert.Contains(route.Legs, l => l.Mode == RouteLegMode.Ferry);
        // Real Manila<->Davao RoRo crossing is ~1,625 km - miles off Mapbox's fabricated ~1,336 km
        // driving-only number, but should be in the right ballpark, not a straight haversine line.
        Assert.InRange(route.TotalDistanceMeters, 1_400_000, 1_900_000);
    }

    [Fact]
    public void TryResolve_BacolodToDavao_ComposesThroughCebuHub()
    {
        // Negros<->Mindanao has no direct sourced crossing - must route via Cebu (2 ferry hops).
        var route = sut.TryResolve(Bacolod, Davao);

        Assert.NotNull(route);
        Assert.Equal(2, route.Legs.Count(l => l.Mode == RouteLegMode.Ferry));
    }

    [Fact]
    public void TryResolve_CebuToCagayanDeOro_UsesDirectCrossing()
    {
        var route = sut.TryResolve(Cebu, CagayanDeOro);

        Assert.NotNull(route);
        Assert.Single(route.Legs, l => l.Mode == RouteLegMode.Ferry);
    }

    [Fact]
    public void TryResolve_SameIsland_ReturnsNull()
    {
        // Cagayan de Oro and Davao are both on Mindanao - ordinary driving, no ferry.
        var route = sut.TryResolve(CagayanDeOro, Davao);

        Assert.Null(route);
    }

    [Fact]
    public void TryResolve_OutsidePhilippines_ReturnsNull()
    {
        // Dallas -> Chicago - well outside the modeled island network.
        var route = sut.TryResolve(new GeoPoint(-96.797, 32.7767), new GeoPoint(-87.6298, 41.8781));

        Assert.Null(route);
    }
}
