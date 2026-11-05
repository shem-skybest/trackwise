using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The per-type rules that decide which fields and panels a workfile uses.
/// <para>
/// Static so callers can ask what a type requires <em>before</em> a file of that type exists -
/// promotion has to know whether a booking is mandatory before it has anything to construct.
/// <see cref="Workfile"/> exposes these as properties that delegate here, so the answer is the
/// same whether you ask the type or an instance.
/// </para>
/// </summary>
public sealed record WorkfileRules
{
    /// <summary>Reefer files are not booked against a carrier, so they carry no booking.</summary>
    public required bool RequiresBookingNumber { get; init; }

    /// <summary>Reefer files carry no bill of lading, so the MBL/HBL indicator does not apply.</summary>
    public required bool RequiresBillIndicator { get; init; }

    /// <summary>Reefer files share no BL number, so duplicate detection would reject valid files.</summary>
    public required bool RequiresDuplicateBlCheck { get; init; }

    /// <summary>A reefer file with no drops is almost certainly incomplete - warn, do not block.</summary>
    public required bool RequiresDeliveryLocation { get; init; }

    public required ContainerKind ContainerKind { get; init; }

    /// <summary>Only meaningful where the tenant's own trucks run the job.</summary>
    public required bool SupportsGarageTracking { get; init; }

    public required bool SupportsBudgetDistribution { get; init; }

    /// <summary>
    /// Genset has only ever been seen as an add-on to a reefer container booking - never a
    /// standalone job, never on a Regular or Trucking file.
    /// </summary>
    public required bool SupportsGenset { get; init; }

    public static WorkfileRules For(WorkfileType type) => type switch
    {
        WorkfileType.Reefer => new WorkfileRules
        {
            RequiresBookingNumber = false,
            RequiresBillIndicator = false,
            RequiresDuplicateBlCheck = false,
            RequiresDeliveryLocation = true,
            ContainerKind = ContainerKind.Trucking,
            SupportsGarageTracking = true,
            SupportsBudgetDistribution = true,
            SupportsGenset = true
        },
        WorkfileType.Trucking => new WorkfileRules
        {
            RequiresBookingNumber = true,
            RequiresBillIndicator = true,
            RequiresDuplicateBlCheck = true,
            RequiresDeliveryLocation = false,
            ContainerKind = ContainerKind.Trucking,
            SupportsGarageTracking = true,
            SupportsBudgetDistribution = true,
            SupportsGenset = false
        },
        _ => new WorkfileRules
        {
            RequiresBookingNumber = true,
            RequiresBillIndicator = true,
            RequiresDuplicateBlCheck = true,
            RequiresDeliveryLocation = false,
            ContainerKind = ContainerKind.Ocean,
            SupportsGarageTracking = false,
            SupportsBudgetDistribution = false,
            SupportsGenset = false
        }
    };
}
