using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One truck on one day of the crew plan. Every active truck gets a row; an unplanned one has no
/// <see cref="AssignmentId" /> and no <see cref="UnitStatus" />.
/// </summary>
public record CrewPlanRowDto
{
    public DateOnly Date { get; set; }
    public Guid TruckId { get; set; }
    public required string TruckNumber { get; set; }
    public required string TruckName { get; set; }
    public TruckType TruckType { get; set; }
    public TruckTonnageClass? TonnageClass { get; set; }

    /// <summary>The truck's usual driver, offered as the default when planning an unplanned row.</summary>
    public Guid? MainDriverId { get; set; }

    public string? MainDriverName { get; set; }

    public Guid? AssignmentId { get; set; }
    public TruckDayStatus? UnitStatus { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? HelperId { get; set; }
    public string? HelperName { get; set; }
    public Guid? SecondHelperId { get; set; }
    public string? SecondHelperName { get; set; }
    public string? Notes { get; set; }
}
