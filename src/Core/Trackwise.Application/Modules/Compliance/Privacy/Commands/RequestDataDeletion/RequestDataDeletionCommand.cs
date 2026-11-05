using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

public class RequestDataDeletionCommand : ICommand<Result<Guid>>
{
    public string? Reason { get; set; }
}
