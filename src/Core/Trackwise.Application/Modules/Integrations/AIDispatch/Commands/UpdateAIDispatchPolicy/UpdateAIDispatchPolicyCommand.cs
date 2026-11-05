using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

[RequiresFeature(TenantFeature.AgenticDispatch)]
public class UpdateAIDispatchPolicyCommand : ICommand
{
    /// <summary>Dispatcher directives. Null or empty clears them; the learned section is untouched.</summary>
    public string? ManualContent { get; set; }

    /// <summary>False pauses both prompt injection and nightly learning.</summary>
    public bool IsEnabled { get; set; } = true;
}
