using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

/// <summary>Corrects a paper. It cannot move to another vehicle - record a new paper there instead.</summary>
public class UpdateVehiclePermitCommand : ICommand<Result>, IHaveId, IVehiclePermitFields
{
    public Guid Id { get; set; }
    public VehiclePermitType Type { get; set; }
    public string? Number { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
}
