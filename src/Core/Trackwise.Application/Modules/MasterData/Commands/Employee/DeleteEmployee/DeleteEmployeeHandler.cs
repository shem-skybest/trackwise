using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteEmployeeHandler(ITenantUnitOfWork tenantUow, IMasterUnitOfWork masterUow)
    : IAppRequestHandler<DeleteEmployeeCommand, Result>
{
    public async Task<Result> Handle(DeleteEmployeeCommand req, CancellationToken ct)
    {
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.Id, ct);

        if (employee is null)
        {
            return Result.Fail($"Could not find employee with ID '{req.Id}'");
        }

        // Employee.Id is a User.Id by convention (Employee.CreateEmployeeFromUser), so soft-deleting
        // this row also strips tenant access the moment GetCurrentUserPermissionsHandler / the
        // claims factory next resolve it - correct for offboarding, but a steward merging or
        // cleaning up records for unrelated reasons could trigger the same side effect by accident.
        // That risk needs the same explicit confirmation any other dependency-in-use case gets,
        // reusing the fields every other master entity's delete already carries rather than adding a
        // new flag.
        var user = await masterUow.Repository<User>().GetByIdAsync(employee.Id, ct);
        var hasLiveLogin = user is not null && user.DeletionRequestedAt is null && user.AnonymizedAt is null;

        if (hasLiveLogin && !req.ConfirmedDespiteDependents)
        {
            return Result.Fail(
                $"Employee '{employee.Name}' has an active platform login. Confirm this employee "
                + "should lose tenant access, with a reason.",
                ErrorCodes.MasterDataRecordInUse);
        }

        var deleteResult = await tenantUow.ExecuteInTransactionAsync<Result>(
            async token =>
            {
                // Loaded and edited inside the delegate: a retry replays against a cleared change
                // tracker, so anything touched above it would be detached from the second attempt on.
                var dispatchedLoads = tenantUow.Repository<Load>().Query()
                    .Where(l => l.AssignedDispatcherId == employee.Id);

                foreach (var load in dispatchedLoads)
                {
                    load.AssignedDispatcher = null;
                }

                tenantUow.Repository<Employee>().Delete(employee);
                return TransactionOutcome<Result>.Keep(Result.Ok());
            },
            ct);

        if (!deleteResult.IsSuccess)
        {
            return deleteResult;
        }

        // The master-DB side effect stays a separate, non-transactional step exactly as it was
        // before this promotion - a single local transaction cannot span the tenant and master
        // databases, a pre-existing gap this change does not attempt to close.
        var tenant = tenantUow.GetCurrentTenant();

        if (user?.Tenant is not null && user.Tenant.Id == tenant.Id)
        {
            user.Tenant = null;
            masterUow.Repository<User>().Update(user);
            await masterUow.SaveChangesAsync(ct);
        }

        return Result.Ok();
    }
}
