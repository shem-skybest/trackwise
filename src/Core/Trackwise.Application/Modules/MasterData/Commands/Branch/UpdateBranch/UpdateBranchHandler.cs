using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateBranchHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateBranchCommand, Result>
{
    public async Task<Result> Handle(UpdateBranchCommand req, CancellationToken ct)
    {
        var branch = await tenantUow.Repository<Branch>().GetByIdAsync(req.Id, ct);
        if (branch is null)
        {
            return Result.Fail($"Could not find branch with ID '{req.Id}'");
        }

        // Code is deliberately not updatable. It is the stable handle other systems join on and it
        // survives a merge; renaming it would strand every legacy key pointing at this record.
        branch.Name = req.Name;
        branch.ShortName = req.ShortName;
        branch.Address = req.Address;
        branch.Phone = req.Phone;
        branch.Email = req.Email;
        branch.TaxId = req.TaxId;
        branch.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
