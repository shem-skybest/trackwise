using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

[RequiresFeature(TenantFeature.AICopilot)]
public class RejectAICopilotDecisionCommand : ICommand
{
    public Guid DecisionId { get; set; }
    public string? Reason { get; set; }
}
