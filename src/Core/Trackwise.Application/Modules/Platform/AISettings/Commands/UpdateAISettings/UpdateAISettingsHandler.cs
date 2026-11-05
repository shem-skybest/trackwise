using Trackwise.Application.Abstractions.AI;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.SystemSettings;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.AISettings.Commands;

internal sealed class UpdateAISettingsHandler(
    ISystemSettingsService systemSettings,
    IMasterUnitOfWork masterUow) : IAppRequestHandler<UpdateAISettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateAISettingsCommand req, CancellationToken ct)
    {
        var modelInfo = LlmModelCatalog.Find(req.Model);
        if (modelInfo is null)
            return Result.Fail($"Unknown AI model '{req.Model}'.");

        // Persist the global model selection (provider is derived from the model via the catalog).
        await systemSettings.SetAsync(AISettingsKeys.Model, modelInfo.Id,
            "Platform-wide AI dispatch model", ct);
        await systemSettings.SetAsync(AISettingsKeys.ReasoningEffort, req.ReasoningEffort.ToString(),
            "Global reasoning effort for the dispatch agent and copilot", ct);

        var planRepo = masterUow.Repository<SubscriptionPlan>();
        var changed = false;
        foreach (var planUpdate in req.Plans)
        {
            var plan = await planRepo.GetByIdAsync(planUpdate.PlanId, ct);
            if (plan is null || plan.WeeklyAIBudgetUsd == planUpdate.WeeklyAIBudgetUsd)
                continue;

            plan.WeeklyAIBudgetUsd = planUpdate.WeeklyAIBudgetUsd;
            planRepo.Update(plan);
            changed = true;
        }

        if (changed)
            await masterUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
