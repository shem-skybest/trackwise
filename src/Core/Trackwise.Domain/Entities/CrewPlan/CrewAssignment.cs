using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One row of the dispatcher's morning plan: for one truck on one day, its crew and what the unit is
/// doing. Skybest pairs crew with a truck per day, not permanently, so this - not
/// <see cref="Truck.MainDriver" /> - is where a trip created on that truck that day takes its crew from.
/// <para>
/// At most one per truck per day. Nobody can be on two trucks' crews on the same day; that is
/// checked when the plan is saved, since it spans rows.
/// </para>
/// </summary>
public class CrewAssignment : AuditableEntity, ITenantEntity
{
    private string? notes;

    /// <summary>The plan's day, in the tenant's local calendar.</summary>
    public DateOnly Date { get; private set; }

    public Guid TruckId { get; private set; }
    public virtual Truck Truck { get; private set; } = null!;

    public TruckDayStatus UnitStatus { get; set; } = TruckDayStatus.Available;

    public Guid? DriverId { get; private set; }
    public virtual Employee? Driver { get; private set; }

    public Guid? HelperId { get; private set; }
    public virtual Helper? Helper { get; private set; }

    public Guid? SecondHelperId { get; private set; }
    public virtual Helper? SecondHelper { get; private set; }

    /// <summary>The unit's concern or type of repair, or why it is off the road.</summary>
    public string? Notes
    {
        get => notes;
        set => notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static CrewAssignment Create(DateOnly date, Guid truckId) => new() { Date = date, TruckId = truckId };

    /// <exception cref="InvalidOperationException">Thrown if the same helper is given twice.</exception>
    public void AssignCrew(Employee? driver, Helper? helper, Helper? secondHelper)
    {
        if (helper is not null && secondHelper is not null && helper.Id == secondHelper.Id)
        {
            throw new InvalidOperationException("The same helper can't be assigned twice.");
        }

        DriverId = driver?.Id;
        Driver = driver;
        HelperId = helper?.Id;
        Helper = helper;
        SecondHelperId = secondHelper?.Id;
        SecondHelper = secondHelper;
    }

    /// <summary>
    /// Takes one person off this crew, e.g. a driver who called in absent after the plan was made.
    /// A second helper moves up when the first is removed. An available unit that loses crew becomes
    /// <see cref="TruckDayStatus.Understaffed" />, the way Monday's dispatchers marked it. Returns
    /// false when the person wasn't on this crew.
    /// </summary>
    public bool RemoveCrewMember(Guid personId)
    {
        if (DriverId == personId)
        {
            DriverId = null;
            Driver = null;
        }
        else if (HelperId == personId)
        {
            HelperId = SecondHelperId;
            Helper = SecondHelperId is null ? null : SecondHelper;
            SecondHelperId = null;
            SecondHelper = null;
        }
        else if (SecondHelperId == personId)
        {
            SecondHelperId = null;
            SecondHelper = null;
        }
        else
        {
            return false;
        }

        if (UnitStatus == TruckDayStatus.Available)
        {
            UnitStatus = TruckDayStatus.Understaffed;
        }

        return true;
    }

    /// <summary>Everyone on this crew - the driver and helpers - as employee/helper ids.</summary>
    public IReadOnlyList<Guid> GetCrewIds() =>
        new[] { DriverId, HelperId, SecondHelperId }.OfType<Guid>().ToList();
}
