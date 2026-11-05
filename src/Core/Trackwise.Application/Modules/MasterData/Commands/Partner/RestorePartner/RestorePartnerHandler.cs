using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestorePartnerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestorePartnerCommand, Result>
{
    public async Task<Result> Handle(RestorePartnerCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var partner = await tenantUow.Repository<BusinessPartner>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);

        if (partner is null)
        {
            return Result.Fail($"Could not find business partner with ID '{req.Id}'");
        }

        if (partner.DeletedAt is null)
        {
            return Result.Fail("That business partner is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = partner.Code;
        var holder = await tenantUow.Repository<BusinessPartner>().Query()
            .FirstOrDefaultAsync(p => p.Code == code && p.Id != partner.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        partner.DeletedAt = null;
        partner.DeletedBy = null;

        // Restored as whatever it was: an approved partner comes back live, a draft comes back a
        // draft. Deletion is not a review, so it must not hand out an approval nobody gave - nor
        // take one back that somebody did.
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
