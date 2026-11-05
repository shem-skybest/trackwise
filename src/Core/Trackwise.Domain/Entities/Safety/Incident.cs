using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Domain.Entities.Safety;

/// <summary>
/// One thing that went wrong on the job - a road accident, a breakdown, damaged or missing goods, a
/// traffic violation - with who was involved, what it cost and what was done about it. Replaces the
/// numbered cases (<c>CCL-CASENO.###</c>) Cold Chain keeps in Monday's weekly report, and the
/// accident report it grew out of. See <c>Incident.Status.cs</c> for the review flow.
/// </summary>
public partial class Incident : AuditableEntity, ITenantEntity
{
    /// <summary>
    /// How far ahead of the server clock a reported time may be, as for trip times.
    /// </summary>
    private static readonly TimeSpan ClockSkewAllowance = TimeSpan.FromMinutes(5);

    /// <summary>The tenant's number series that case numbers come from; <c>CASE-001</c> until a tenant sets its own pattern.</summary>
    public const string CaseNumberSeries = "Incident";
    public const string DefaultCaseNumberPrefix = "CASE";
    public const int CaseNumberPadWidth = 3;

    /// <summary>
    /// The case number, e.g. <c>CCL-CASENO.081</c>: from the tenant's <c>Incident</c> number series, or
    /// Monday's own number for an imported case.
    /// </summary>
    public string CaseNumber { get; private set; } = null!;

    public IncidentKind Kind { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public AccidentType? AccidentType { get; private set; }

    public DateTime OccurredAt { get; private set; }
    public string Summary { get; private set; } = null!;

    /// <summary>What stops it happening again - the weekly report's improvement and future plans.</summary>
    public string? Prevention { get; private set; }

    public string? Place { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public Guid? TripId { get; private set; }

    /// <summary>The client whose delivery it was; from the trip when there is one.</summary>
    public Guid? CustomerId { get; private set; }

    public Guid? TruckId { get; private set; }
    public Guid? DriverId { get; private set; }
    public Guid? HelperId { get; private set; }
    public Guid? SecondHelperId { get; private set; }

    public bool AnyInjuries { get; private set; }
    public int? NumberOfInjuries { get; private set; }
    public string? InjuryDescription { get; private set; }

    public bool VehicleDamaged { get; private set; }
    public string? VehicleDamageDescription { get; private set; }
    public bool VehicleDrivable { get; private set; } = true;
    public decimal? EstimatedDamageCost { get; private set; }

    public bool PoliceReportFiled { get; private set; }
    public string? PoliceReportNumber { get; private set; }
    public string? PoliceStation { get; private set; }

    /// <summary>The barangay's blotter entry - often the only report a minor road incident gets.</summary>
    public string? BarangayBlotterNumber { get; private set; }

    public string? InsuranceClaimNumber { get; private set; }
    public DateOnly? InsuranceNotifiedOn { get; private set; }

    public decimal? ThirdPartyCompensation { get; private set; }
    public decimal? CrewChargeAmount { get; private set; }
    public string? CrewChargeNote { get; private set; }

    public virtual List<IncidentWitness> Witnesses { get; private set; } = [];
    public virtual List<IncidentThirdParty> ThirdParties { get; private set; } = [];

    /// <summary>Photos, the police report, the blotter - whatever the crew and the office collected.</summary>
    public virtual List<IncidentDocument> Documents { get; private set; } = [];

    /// <exception cref="InvalidOperationException">Thrown when the details don't hold together.</exception>
    public static Incident Record(string caseNumber, IncidentDetails details, IncidentParties parties, DateTime now)
    {
        var incident = new Incident
        {
            CaseNumber = Clean(caseNumber) ?? throw new InvalidOperationException("An incident needs a case number."),
            Status = IncidentStatus.Open
        };
        incident.Apply(details, parties, now);
        return incident;
    }

    /// <summary>Rewrites the report. Allowed until it is resolved.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the incident is resolved or closed, or the details don't hold together.
    /// </exception>
    public void Edit(IncidentDetails details, IncidentParties parties, DateTime now)
    {
        if (Status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            throw new InvalidOperationException(Status == IncidentStatus.Closed
                ? "This incident is closed and can't be changed."
                : "This incident is resolved. Reopen it to change it.");
        }

        Apply(details, parties, now);
    }

    private void Apply(IncidentDetails d, IncidentParties parties, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(d.Summary))
        {
            throw new InvalidOperationException("Say what happened.");
        }

        if (d.OccurredAt > now + ClockSkewAllowance)
        {
            throw new InvalidOperationException("The incident time can't be in the future.");
        }

        if (d.AnyInjuries && d.NumberOfInjuries is not > 0)
        {
            throw new InvalidOperationException("Enter how many people were hurt.");
        }

        if (d.Latitude.HasValue != d.Longitude.HasValue)
        {
            throw new InvalidOperationException("A pin needs both latitude and longitude.");
        }

        if (d.EstimatedDamageCost < 0 || d.ThirdPartyCompensation < 0 || d.CrewChargeAmount < 0)
        {
            throw new InvalidOperationException("Amounts can't be negative.");
        }

        if (parties.HelperId is { } helper && helper == parties.SecondHelperId)
        {
            throw new InvalidOperationException("The same helper can't be listed twice.");
        }

        Kind = d.Kind;
        Severity = d.Severity;
        AccidentType = d.Kind == IncidentKind.Accident ? d.AccidentType : null;
        OccurredAt = d.OccurredAt;
        Summary = d.Summary.Trim();
        Prevention = Clean(d.Prevention);
        Place = Clean(d.Place);
        Latitude = d.Latitude;
        Longitude = d.Longitude;

        AnyInjuries = d.AnyInjuries;
        NumberOfInjuries = d.AnyInjuries ? d.NumberOfInjuries : null;
        InjuryDescription = d.AnyInjuries ? Clean(d.InjuryDescription) : null;

        VehicleDamaged = d.VehicleDamaged;
        VehicleDamageDescription = d.VehicleDamaged ? Clean(d.VehicleDamageDescription) : null;
        VehicleDrivable = d.VehicleDrivable;
        EstimatedDamageCost = d.EstimatedDamageCost;

        PoliceReportFiled = d.PoliceReportFiled;
        PoliceReportNumber = d.PoliceReportFiled ? Clean(d.PoliceReportNumber) : null;
        PoliceStation = d.PoliceReportFiled ? Clean(d.PoliceStation) : null;
        BarangayBlotterNumber = Clean(d.BarangayBlotterNumber);
        InsuranceClaimNumber = Clean(d.InsuranceClaimNumber);
        InsuranceNotifiedOn = d.InsuranceNotifiedOn;

        ThirdPartyCompensation = d.ThirdPartyCompensation;
        CrewChargeAmount = d.CrewChargeAmount;
        CrewChargeNote = Clean(d.CrewChargeNote);

        TripId = parties.TripId;
        CustomerId = parties.CustomerId;
        TruckId = parties.TruckId;
        DriverId = parties.DriverId;
        HelperId = parties.HelperId;
        SecondHelperId = parties.SecondHelperId;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
