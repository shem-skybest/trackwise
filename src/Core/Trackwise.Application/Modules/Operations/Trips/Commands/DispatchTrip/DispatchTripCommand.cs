using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

public class DispatchTripCommand : ICommand<Result<DispatchTripResultDto>>
{
    public Guid TripId { get; set; }
}
