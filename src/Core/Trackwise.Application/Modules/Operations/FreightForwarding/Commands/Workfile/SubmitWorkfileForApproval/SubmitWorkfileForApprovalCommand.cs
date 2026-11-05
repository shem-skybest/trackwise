using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

[RequiresFeature(TenantFeature.Workfiles)]
public class SubmitWorkfileForApprovalCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
