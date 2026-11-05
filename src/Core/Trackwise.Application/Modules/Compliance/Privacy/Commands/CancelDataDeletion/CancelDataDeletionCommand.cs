using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

public class CancelDataDeletionCommand : ICommand
{
    public Guid Id { get; set; }
}
