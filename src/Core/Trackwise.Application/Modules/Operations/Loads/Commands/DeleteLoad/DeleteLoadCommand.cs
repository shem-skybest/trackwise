using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class DeleteLoadCommand : ICommand
{
    public Guid Id { get; set; }
}
