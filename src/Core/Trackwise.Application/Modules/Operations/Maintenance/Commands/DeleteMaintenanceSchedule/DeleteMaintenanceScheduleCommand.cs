using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>Removes a schedule. The services logged against it stay, unlinked.</summary>
[RequiresFeature(TenantFeature.Maintenance)]
public record DeleteMaintenanceScheduleCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
