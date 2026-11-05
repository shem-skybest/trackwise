using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class ApprovePartnerHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataApprovalService approvals)
    : IAppRequestHandler<ApprovePartnerCommand, Result>
{
    public async Task<Result> Handle(ApprovePartnerCommand req, CancellationToken ct)
    {
        var partner = await tenantUow.Repository<BusinessPartner>().GetByIdAsync(req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        // Approving takes the record live, and live means the code is in play: the unique index is
        // filtered to live rows, so a draft can have been sitting on a code another partner has
        // since taken. Caught here rather than at the index, where it would surface as a database
        // error with no field attached to it.
        var code = partner.Code;
        var holder = await tenantUow.Repository<BusinessPartner>().Query()
            .FirstOrDefaultAsync(p => p.Code == code && p.Id != partner.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Send this record back to its steward "
                + "so the duplicate can be resolved before it goes live.");
        }

        var result = await approvals.ApproveAsync(nameof(BusinessPartner), partner, req.Note, ct);

        if (!result.IsSuccess)
        {
            return result;
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
