using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class SubmitPartnerHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataApprovalService approvals)
    : IAppRequestHandler<SubmitPartnerCommand, Result>
{
    public async Task<Result> Handle(SubmitPartnerCommand req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        // A partner with no role is a partner no workfile slot can use, and an approver reading a
        // record like that has nothing to decide about. Checked here rather than in the validator
        // because it is a fact about the record, not about the request.
        if (partner.Roles.Count == 0)
        {
            return Result.Fail(
                $"{partner.Code} has no roles, so there is nothing for an approver to accept it as.");
        }

        var result = await approvals.SubmitAsync(nameof(BusinessPartner), partner, req.Note, ct);

        if (!result.IsSuccess)
        {
            return result;
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
