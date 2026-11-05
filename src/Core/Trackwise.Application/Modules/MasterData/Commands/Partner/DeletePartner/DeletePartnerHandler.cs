using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeletePartnerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeletePartnerCommand, Result>
{
    public async Task<Result> Handle(DeletePartnerCommand req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        // A submission left open over a deleted record would sit in an approver's queue forever,
        // pointing at something they cannot open. Withdrawing it is the honest close: the request
        // keeps its history, it simply stops waiting on anyone.
        await WithdrawOpenSubmissionAsync(partner, ct);

        // No hand-rolled dependency check: the probe runs in the pipeline off the entity's declared
        // rules, which for a partner is the five counterparty slots on an open workfile.
        tenantUow.Repository<BusinessPartner>().Delete(partner);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private async Task WithdrawOpenSubmissionAsync(BusinessPartner partner, CancellationToken ct)
    {
        var open = await tenantUow.Repository<MasterDataApprovalRequest>().Query()
            .Where(r => r.EntityType == nameof(BusinessPartner)
                        && r.EntityId == partner.Id
                        && r.Decision == null)
            .ToListAsync(ct);

        foreach (var request in open)
        {
            request.Decision = MasterDataApprovalDecision.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewNote = "Withdrawn - the record was deleted before anyone reviewed it.";
        }

        if (open.Count > 0)
        {
            partner.ApprovalState = MasterDataApprovalState.Draft;
        }
    }
}
