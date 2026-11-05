using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

/// <summary>The paper fields create and update share, so <see cref="VehiclePermitValidatorBase{T}" /> checks them once.</summary>
public interface IVehiclePermitFields
{
    VehiclePermitType Type { get; }
    string? Number { get; }
    string? Issuer { get; }
    DateOnly? IssuedOn { get; }
    DateOnly ExpiresOn { get; }
    decimal? Cost { get; }
    string? Notes { get; }
}
