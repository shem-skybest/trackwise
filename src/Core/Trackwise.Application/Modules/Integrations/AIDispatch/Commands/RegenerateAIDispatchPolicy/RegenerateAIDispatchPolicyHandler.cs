using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.AIDispatch.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

/// <summary>
/// Runs synchronously: one ~1k-token call on a cheap model takes a couple of seconds, so a background
/// job plus polling would add moving parts without shortening the wait.
/// </summary>
internal sealed class RegenerateAIDispatchPolicyHandler(
    IAIDispatchPolicyLearner learner,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RegenerateAIDispatchPolicyCommand, Result<AIDispatchPolicyDto>>
{
    public async Task<Result<AIDispatchPolicyDto>> Handle(
        RegenerateAIDispatchPolicyCommand request, CancellationToken ct)
    {
        var outcome = await learner.LearnForCurrentTenantAsync(force: true, ct);

        if (!outcome.IsSuccess)
        {
            return Result<AIDispatchPolicyDto>.Fail(outcome.Error ?? "Failed to regenerate the policy.");
        }

        // Surface the skip reason (e.g. "not enough reviewed decisions yet") rather than a generic error.
        if (!outcome.Value!.Generated)
        {
            return Result<AIDispatchPolicyDto>.Fail(outcome.Value.SkipReason ?? "Nothing to learn yet.");
        }

        // A generated pass always leaves a row behind.
        var policy = await tenantUow.Repository<AIDispatchPolicy>().Query().FirstAsync(ct);

        return Result<AIDispatchPolicyDto>.Ok(policy.ToDto());
    }
}
