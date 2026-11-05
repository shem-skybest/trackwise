using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class SetDriverDeviceTokenHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<SetDriverDeviceTokenCommand, Result>
{
    public async Task<Result> Handle(
        SetDriverDeviceTokenCommand req, CancellationToken ct)
    {
        // A push token registered under someone else would send that driver's trips to this phone
        if (currentUser.GetUserId() != req.UserId)
        {
            return Result.Fail("You can only register your own device.");
        }

        var driver = await tenantUow.Repository<Employee>().GetByIdAsync(req.UserId, ct);

        if (driver is null)
        {
            return Result.Fail("Could not find the specified driver");
        }

        if (!string.IsNullOrEmpty(driver.DeviceToken) && driver.DeviceToken == req.DeviceToken)
        {
            return Result.Ok();
        }

        // One phone, one driver: whoever used this phone before would otherwise have their pushes land here
        var previousHolders = await tenantUow.Repository<Employee>().Query()
            .Where(e => e.DeviceToken == req.DeviceToken && e.Id != driver.Id)
            .ToListAsync(ct);
        foreach (var previous in previousHolders)
        {
            previous.DeviceToken = null;
        }

        driver.DeviceToken = req.DeviceToken;
        tenantUow.Repository<Employee>().Update(driver);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
