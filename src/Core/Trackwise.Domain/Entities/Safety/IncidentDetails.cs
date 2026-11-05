using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Domain.Entities.Safety;

/// <summary>What an incident report says happened - everything an edit can change except who and what was involved.</summary>
public sealed record IncidentDetails
{
    public required IncidentKind Kind { get; init; }
    public required IncidentSeverity Severity { get; init; }

    /// <summary>Only for an accident; dropped for any other kind.</summary>
    public AccidentType? AccidentType { get; init; }

    /// <summary>When it happened, in UTC - the reported time.</summary>
    public required DateTime OccurredAt { get; init; }

    public required string Summary { get; init; }

    /// <summary>What stops it happening again, e.g. "Check P.O. validity before dispatch".</summary>
    public string? Prevention { get; init; }

    /// <summary>Where it happened in words, e.g. "Brgy. Bagumbong North, Caloocan".</summary>
    public string? Place { get; init; }

    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    public bool AnyInjuries { get; init; }
    public int? NumberOfInjuries { get; init; }
    public string? InjuryDescription { get; init; }

    public bool VehicleDamaged { get; init; }
    public string? VehicleDamageDescription { get; init; }
    public bool VehicleDrivable { get; init; } = true;
    public decimal? EstimatedDamageCost { get; init; }

    public bool PoliceReportFiled { get; init; }
    public string? PoliceReportNumber { get; init; }
    public string? PoliceStation { get; init; }
    public string? BarangayBlotterNumber { get; init; }

    public string? InsuranceClaimNumber { get; init; }
    public DateOnly? InsuranceNotifiedOn { get; init; }

    /// <summary>Paid to someone outside the company, e.g. a property owner.</summary>
    public decimal? ThirdPartyCompensation { get; init; }

    /// <summary>The part of the cost the crew pays back, once decided.</summary>
    public decimal? CrewChargeAmount { get; init; }

    /// <summary>Who is charged and how, e.g. "Driver, salary deduction over 4 cut-offs".</summary>
    public string? CrewChargeNote { get; init; }
}
