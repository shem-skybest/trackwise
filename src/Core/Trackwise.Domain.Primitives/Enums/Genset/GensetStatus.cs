namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// A genset's availability. Genset is a scarce shared physical resource - a container's genset add-on
/// is only bookable against a unit sitting in <see cref="Available"/>.
/// </summary>
public enum GensetStatus
{
    Available,
    InUse,
    UnderMaintenance,
    OutOfService
}
