using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Commands;

public sealed class DeleteDemoRequestCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
