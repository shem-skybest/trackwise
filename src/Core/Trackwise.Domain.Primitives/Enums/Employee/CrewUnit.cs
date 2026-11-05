namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// The operating unit a driver or helper normally works for. Crew are lent between units for a day
/// (see <see cref="CrewDayStatus.LentOut" />), so this is their home, not where they are today.
/// </summary>
public enum CrewUnit
{
    ColdChain,
    WingVan,
    Trucking
}
