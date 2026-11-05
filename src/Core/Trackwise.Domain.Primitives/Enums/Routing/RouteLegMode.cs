namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How a route leg is traversed. <see cref="Ferry"/> legs cross water with no road connection -
/// see `IFerryRouteResolver` (Trackwise.Application.Abstractions.Routing).
/// </summary>
public enum RouteLegMode
{
    Driving,
    Ferry
}
