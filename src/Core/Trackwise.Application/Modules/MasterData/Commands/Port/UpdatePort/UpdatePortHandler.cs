using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdatePortHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdatePortCommand, Result>
{
    public async Task<Result> Handle(UpdatePortCommand req, CancellationToken ct)
    {
        var terminal = await tenantUow.Repository<Terminal>().GetByIdAsync(req.Id, ct);
        if (terminal is null)
        {
            return Result.Fail($"Could not find port with ID '{req.Id}'");
        }

        if (req.BranchId is { } branchId && branchId != terminal.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        terminal.Name = req.Name;
        terminal.CountryCode = req.CountryCode;
        terminal.Type = req.Type;
        terminal.Address = req.Address;
        terminal.Notes = req.Notes;
        terminal.BranchId = req.BranchId;
        terminal.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
