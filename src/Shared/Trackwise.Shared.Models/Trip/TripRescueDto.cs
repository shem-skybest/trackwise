namespace Trackwise.Shared.Models;

public record TripRescueDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public long TripNumber { get; set; }

    public Guid TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? HelperId { get; set; }
    public string? HelperName { get; set; }
    public Guid? SecondHelperId { get; set; }
    public string? SecondHelperName { get; set; }

    public DateTime TransferredAt { get; set; }
    public string? Place { get; set; }
    public required string Reason { get; set; }
    public Guid? IncidentId { get; set; }
    public string? IncidentCaseNumber { get; set; }

    /// <summary>When the rescue truck left its garage and got back.</summary>
    public DateTime? GarageDepartedAt { get; set; }

    public DateTime? GarageArrivedAt { get; set; }

    /// <summary>The drops it took over.</summary>
    public IReadOnlyList<Guid> StopIds { get; set; } = [];

    public string? RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; }
}
