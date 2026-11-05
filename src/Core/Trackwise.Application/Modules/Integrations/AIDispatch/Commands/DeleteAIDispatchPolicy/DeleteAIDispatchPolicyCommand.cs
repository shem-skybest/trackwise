using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

/// <summary>
/// Erases the learned policy and any dispatcher directives. The nightly job re-learns from the
/// surviving decision history unless the policy is also switched off - the UI has to say so.
/// </summary>
[RequiresFeature(TenantFeature.AgenticDispatch)]
public class DeleteAIDispatchPolicyCommand : ICommand;
