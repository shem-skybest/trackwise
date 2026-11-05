using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class ConfirmLoadStatusCommand : ICommand
{
    /// <summary>Not trusted: the load's driver is checked against the caller instead.</summary>
    public Guid DriverId { get; set; }
    public Guid LoadId { get; set; }
    public LoadStatus? LoadStatus { get; set; }
}
