using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class BulkDispatchLoadsCommand : ICommand
{
    public Guid[] LoadIds { get; set; } = [];
}
