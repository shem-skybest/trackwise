using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RejectPartnerHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataApprovalService approvals)
    : IAppRequestHandler<RejectPartnerCommand, Result>
{
    public async Task<Result> Handle(RejectPartnerCommand req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        var result = await approvals.RejectAsync(nameof(BusinessPartner), partner, req.Reason, ct);

        if (!result.IsSuccess)
        {
            return result;
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
