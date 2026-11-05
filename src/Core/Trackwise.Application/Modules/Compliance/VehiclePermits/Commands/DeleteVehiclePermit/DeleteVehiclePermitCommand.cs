using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

public class DeleteVehiclePermitCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
