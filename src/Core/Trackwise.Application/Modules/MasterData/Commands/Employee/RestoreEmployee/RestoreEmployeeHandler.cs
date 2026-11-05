using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreEmployeeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreEmployeeCommand, Result>
{
    public async Task<Result> Handle(RestoreEmployeeCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var employee = await tenantUow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == req.Id, ct);

        if (employee is null)
        {
            return Result.Fail($"Could not find employee with ID '{req.Id}'");
        }

        if (employee.DeletedAt is null)
        {
            return Result.Fail("That employee is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = employee.Code;
        var holder = await tenantUow.Repository<Employee>().Query()
            .FirstOrDefaultAsync(e => e.Code == code && e.Id != employee.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        employee.DeletedAt = null;
        employee.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
