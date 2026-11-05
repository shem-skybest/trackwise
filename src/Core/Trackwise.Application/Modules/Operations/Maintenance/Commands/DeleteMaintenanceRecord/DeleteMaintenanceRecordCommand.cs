using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

/// <summary>Removes a record logged by mistake. Its schedule keeps the service date it was moved on to.</summary>
[RequiresFeature(TenantFeature.Maintenance)]
public record DeleteMaintenanceRecordCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
