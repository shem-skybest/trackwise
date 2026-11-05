using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Commands;

public sealed class UpdateDemoRequestCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public DemoRequestStatus Status { get; set; }
    public string? Notes { get; set; }
}
