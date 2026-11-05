using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

/// <summary>
/// Runs a learning pass now instead of waiting for the nightly job. Returns the resulting policy
/// so the page can render it without a second round trip.
/// </summary>
[RequiresFeature(TenantFeature.AgenticDispatch)]
public class RegenerateAIDispatchPolicyCommand : ICommand<Result<AIDispatchPolicyDto>>;
