using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateHelperHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateHelperCommand, Result>
{
    public async Task<Result> Handle(UpdateHelperCommand req, CancellationToken ct)
    {
        var helper = await tenantUow.Repository<Helper>().GetByIdAsync(req.Id, ct);
        if (helper is null)
        {
            return Result.Fail($"Could not find helper with ID '{req.Id}'");
        }

        if (req.BranchId is { } branchId && branchId != helper.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        helper.Name = req.Name;
        helper.PhoneNumber = req.PhoneNumber;
        helper.Address = req.Address;
        helper.Notes = req.Notes;
        helper.EmploymentType = req.EmploymentType;
        helper.HomeUnit = req.HomeUnit;
        helper.BranchId = req.BranchId;
        helper.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
