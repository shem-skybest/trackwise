using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Safety;

namespace Trackwise.Shared.Models;

public record IncidentDto
{
    public Guid Id { get; set; }
    /// <summary>e.g. <c>CCL-CASENO.081</c>.</summary>
    public required string CaseNumber { get; set; }

    public IncidentKind Kind { get; set; }
    public IncidentSeverity Severity { get; set; }
    public AccidentType? AccidentType { get; set; }
    public IncidentStatus Status { get; set; }

    public DateTime OccurredAt { get; set; }
    public required string Summary { get; set; }
    public string? Prevention { get; set; }
    public string? Place { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Guid? TripId { get; set; }
    public long? TripNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? HelperId { get; set; }
    public string? HelperName { get; set; }
    public Guid? SecondHelperId { get; set; }
    public string? SecondHelperName { get; set; }

    public bool AnyInjuries { get; set; }
    public int? NumberOfInjuries { get; set; }
    public string? InjuryDescription { get; set; }

    public bool VehicleDamaged { get; set; }
    public string? VehicleDamageDescription { get; set; }
    public bool VehicleDrivable { get; set; }
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

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ReopenReason { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Repairs linked to it; filled on the details read only.</summary>
    public IReadOnlyList<IncidentRepairDto> Repairs { get; set; } = [];

    /// <summary>Rescues it called for; filled on the details read only.</summary>
    public IReadOnlyList<TripRescueDto> Rescues { get; set; } = [];

    /// <summary>Photos and papers; filled on the details read only.</summary>
    public IReadOnlyList<IncidentAttachmentDto> Attachments { get; set; } = [];

    /// <summary>The linked repairs plus what was paid to third parties - never typed.</summary>
    public decimal TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>A photo, police report or other paper on an incident; download it by <see cref="Id" />.</summary>
public record IncidentAttachmentDto
{
    public Guid Id { get; set; }
    public DocumentType Type { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}

public record IncidentRepairDto
{
    public Guid Id { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? RequisitionNumber { get; set; }
    public string? Description { get; set; }
    public string? VendorName { get; set; }
    public decimal TotalCost { get; set; }
}
