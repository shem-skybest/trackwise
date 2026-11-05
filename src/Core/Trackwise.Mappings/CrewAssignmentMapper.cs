using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class CrewAssignmentMapper
{
    /// <summary>
    /// People's names come in from a lookup the caller has already batched: lazy loading is on, so
    /// reading <c>assignment.Driver</c> or <c>truck.MainDriver</c> here would query per row.
    /// </summary>
    public static CrewPlanRowDto ToPlanRow(
        this Truck truck,
        DateOnly date,
        CrewAssignment? assignment,
        IReadOnlyDictionary<Guid, string> names)
    {
        return new CrewPlanRowDto
        {
            Date = date,
            TruckId = truck.Id,
            TruckNumber = truck.Number,
            TruckName = truck.Name,
            TruckType = truck.Type,
            TonnageClass = truck.TonnageClass,
            MainDriverId = truck.MainDriverId,
            MainDriverName = NameOf(truck.MainDriverId),
            AssignmentId = assignment?.Id,
            UnitStatus = assignment?.UnitStatus,
            DriverId = assignment?.DriverId,
            DriverName = NameOf(assignment?.DriverId),
            HelperId = assignment?.HelperId,
            HelperName = NameOf(assignment?.HelperId),
            SecondHelperId = assignment?.SecondHelperId,
            SecondHelperName = NameOf(assignment?.SecondHelperId),
            Notes = assignment?.Notes
        };

        string? NameOf(Guid? id) => id is { } key ? names.GetValueOrDefault(key) : null;
    }
}
