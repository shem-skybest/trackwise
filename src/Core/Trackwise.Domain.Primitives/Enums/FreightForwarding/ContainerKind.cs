namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Which container shape a workfile's containers take. Ocean containers carry stuffing and
/// measurement data; trucking containers add driver/helper assignment, rates and trip dates.
/// </summary>
public enum ContainerKind
{
    Ocean = 0,
    Trucking = 1
}
