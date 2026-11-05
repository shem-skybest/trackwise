using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.TimeEntries.Commands;

[RequiresFeature(TenantFeature.Timesheets)]
public class DeleteTimeEntryCommand : ICommand<Result>
{
    public required Guid Id { get; set; }
}
