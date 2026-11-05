using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteBranchHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteBranchCommand, Result>
{
    public async Task<Result> Handle(DeleteBranchCommand req, CancellationToken ct)
    {
        var branch = await tenantUow.Repository<Branch>().GetByIdAsync(req.Id, ct);
        if (branch is null)
        {
            return Result.Fail($"Could not find branch with ID '{req.Id}'");
        }

        // Looks like a hard delete and is not: SoftDeleteSaveChangesInterceptor rewrites it into a
        // DeletedAt stamp. Handlers are meant to keep calling Delete.
        tenantUow.Repository<Branch>().Delete(branch);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
