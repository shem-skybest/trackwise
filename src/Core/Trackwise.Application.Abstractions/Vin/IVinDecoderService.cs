using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Vin;

namespace Trackwise.Application.Abstractions.Vin;

/// <summary>
///    Service for decoding VINs (Vehicle Identification Numbers).
/// </summary>
public interface IVinDecoderService
{
    Task<VehicleInfoDto?> DecodeVinAsync(string vin, CancellationToken ct = default);
}
