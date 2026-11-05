using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

public class RequestDataExportCommand : ICommand<Result<Guid>>
{
}
