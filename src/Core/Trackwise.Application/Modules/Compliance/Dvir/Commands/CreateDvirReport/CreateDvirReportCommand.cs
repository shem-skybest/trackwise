using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

[RequiresFeature(TenantFeature.Dvir)]
public record CreateDvirReportCommand : ICommand<Result<DvirReportDto>>
{
    public required Guid TruckId { get; set; }
    public required Guid DriverId { get; set; }
    public required DvirType Type { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? OdometerReading { get; set; }
    public string? DriverNotes { get; set; }
    public string? DriverSignature { get; set; }
    public Guid? TripId { get; set; }
    public List<CreateDvirDefectRequest> Defects { get; set; } = [];
}
