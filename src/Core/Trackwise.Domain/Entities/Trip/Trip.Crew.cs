using Trackwise.Domain.Events;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>Who rides the trip: its driver and up to two helpers.</summary>
public partial class Trip
{
    /// <summary>
    /// Sets the trip's crew. Allowed until the first stop's arrival is recorded; from then on the crew
    /// is the historical record and stays fixed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the trip can no longer be modified, a stop's arrival is recorded, or the same helper
    /// is given twice.
    /// </exception>
    public void AssignCrew(Employee? driver, Helper? helper, Helper? secondHelper)
    {
        if (!TripStatusMachine.CanModify(Status))
        {
            throw new InvalidOperationException("The crew can't change once the trip is under way.");
        }

        EnsureNoStopRecorded("The crew");

        if (helper is not null && secondHelper is not null && helper.Id == secondHelper.Id)
        {
            throw new InvalidOperationException("The same helper can't be assigned twice.");
        }

        if (Status == TripStatus.Dispatched && DriverId != driver?.Id)
        {
            DomainEvents.Add(new TripDriverChangedEvent(Id, DriverId));
        }

        DriverId = driver?.Id;
        Driver = driver;
        HelperId = helper?.Id;
        Helper = helper;
        SecondHelperId = secondHelper?.Id;
        SecondHelper = secondHelper;
    }

    /// <summary>Everyone riding the trip - driver and helpers - as employee/helper ids.</summary>
    public (Guid? DriverId, IReadOnlyList<Guid> HelperIds) GetCrewIds()
    {
        var helpers = new[] { HelperId, SecondHelperId }.OfType<Guid>().ToList();
        return (DriverId, helpers);
    }

    /// <summary>Everyone riding the trip as one list of employee/helper ids.</summary>
    public IReadOnlyList<Guid> GetCrewMemberIds() =>
        new[] { DriverId, HelperId, SecondHelperId }.OfType<Guid>().ToList();
}
