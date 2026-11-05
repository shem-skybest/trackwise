using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Inspections.Commands;

[RequiresFeature(TenantFeature.Safety)]
public class DecodeVinCommand : ICommand<Result<VehicleInfoDto>>
{
    public required string Vin { get; set; }
}
