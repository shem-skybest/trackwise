using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class UpdateAIDispatchPolicyHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser) : IAppRequestHandler<UpdateAIDispatchPolicyCommand, Result>
{
    public async Task<Result> Handle(UpdateAIDispatchPolicyCommand request, CancellationToken ct)
    {
        var repo = tenantUow.Repository<AIDispatchPolicy>();
        var policy = await repo.Query().FirstOrDefaultAsync(ct);

        // Created on demand: directives can be written before the agent has learned anything.
        if (policy is null)
        {
            policy = new AIDispatchPolicy();
            await repo.AddAsync(policy, ct);
        }

        policy.EditManual(request.ManualContent, request.IsEnabled, currentUser.GetUserId());
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
