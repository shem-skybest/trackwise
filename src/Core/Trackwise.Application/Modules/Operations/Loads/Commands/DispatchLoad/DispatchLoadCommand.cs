using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class DispatchLoadCommand : ICommand
{
    public Guid Id { get; set; }
}
