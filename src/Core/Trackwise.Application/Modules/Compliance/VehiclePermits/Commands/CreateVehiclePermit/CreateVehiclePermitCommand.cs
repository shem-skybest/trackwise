using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

/// <summary>Records a paper for a truck or a trailer - set exactly one. A renewal is recorded as a new paper, not an edit.</summary>
public class CreateVehiclePermitCommand : ICommand<Result<Guid>>, IVehiclePermitFields
{
    public Guid? TruckId { get; set; }
    public Guid? TrailerId { get; set; }
    public VehiclePermitType Type { get; set; }
    public string? Number { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
}
