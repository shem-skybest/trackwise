using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Application.Modules.Compliance.Incidents;

/// <summary>The fields an incident report is created and edited with.</summary>
public abstract class IncidentFields
{
    public IncidentKind Kind { get; set; }
    public IncidentSeverity Severity { get; set; }
    public AccidentType? AccidentType { get; set; }

    /// <summary>When it happened, with a timezone.</summary>
    public DateTime OccurredAt { get; set; }

    public string Summary { get; set; } = "";

    /// <summary>What stops it happening again.</summary>
    public string? Prevention { get; set; }

    public string? Place { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>The trip it happened on. Its client is used when no client is given.</summary>
    public Guid? TripId { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? HelperId { get; set; }
    public Guid? SecondHelperId { get; set; }

    public bool AnyInjuries { get; set; }
    public int? NumberOfInjuries { get; set; }
    public string? InjuryDescription { get; set; }

    public bool VehicleDamaged { get; set; }
    public string? VehicleDamageDescription { get; set; }
    public bool VehicleDrivable { get; set; } = true;
    public decimal? EstimatedDamageCost { get; set; }

    public bool PoliceReportFiled { get; set; }
    public string? PoliceReportNumber { get; set; }
    public string? PoliceStation { get; set; }
    public string? BarangayBlotterNumber { get; set; }
    public string? InsuranceClaimNumber { get; set; }
    public DateOnly? InsuranceNotifiedOn { get; set; }

    public decimal? ThirdPartyCompensation { get; set; }
    public decimal? CrewChargeAmount { get; set; }
    public string? CrewChargeNote { get; set; }

    internal IncidentDetails ToDetails() => new()
    {
        Kind = Kind,
        Severity = Severity,
        AccidentType = AccidentType,
        OccurredAt = OccurredAt.ToUniversalTime(),
        Summary = Summary,
        Prevention = Prevention,
        Place = Place,
        Latitude = Latitude,
        Longitude = Longitude,
        AnyInjuries = AnyInjuries,
        NumberOfInjuries = NumberOfInjuries,
        InjuryDescription = InjuryDescription,
        VehicleDamaged = VehicleDamaged,
        VehicleDamageDescription = VehicleDamageDescription,
        VehicleDrivable = VehicleDrivable,
        EstimatedDamageCost = EstimatedDamageCost,
        PoliceReportFiled = PoliceReportFiled,
        PoliceReportNumber = PoliceReportNumber,
        PoliceStation = PoliceStation,
        BarangayBlotterNumber = BarangayBlotterNumber,
        InsuranceClaimNumber = InsuranceClaimNumber,
        InsuranceNotifiedOn = InsuranceNotifiedOn,
        ThirdPartyCompensation = ThirdPartyCompensation,
        CrewChargeAmount = CrewChargeAmount,
        CrewChargeNote = CrewChargeNote
    };
}
