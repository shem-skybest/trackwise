using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Safety.Commands;

[RequiresFeature(TenantFeature.Safety)]
public record ReviewDriverBehaviorEventCommand : ICommand<Result<DriverBehaviorEventDto>>
{
    public required Guid Id { get; set; }
    public string? ReviewNotes { get; set; }
    public bool IsDismissed { get; set; }
}
