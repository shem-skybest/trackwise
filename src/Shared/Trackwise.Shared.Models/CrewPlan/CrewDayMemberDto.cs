using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>One driver or helper on one day of the crew plan: their status and the truck they're on.</summary>
public record CrewDayMemberDto
{
    /// <summary>The employee id for a driver, the helper id for a helper.</summary>
    public Guid PersonId { get; set; }

    public CrewMemberKind Kind { get; set; }
    public required string Name { get; set; }
    public string? Code { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>False for someone retired from the roster who still has a status or truck that day.</summary>
    public bool IsActive { get; set; }

    /// <summary><see cref="CrewDayStatus.Available" /> when nothing was recorded for the day.</summary>
    public CrewDayStatus Status { get; set; }

    public string? Notes { get; set; }

    /// <summary>The truck whose crew plan they're on that day, if any.</summary>
    public Guid? TruckId { get; set; }

    public string? TruckNumber { get; set; }
}
