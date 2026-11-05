using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class SetEmployeePasswordHandler(
    ITenantUnitOfWork tenantUow,
    UserManager<User> userManager,
    ICurrentUserService currentUser)
    : IAppRequestHandler<SetEmployeePasswordCommand, Result>
{
    public async Task<Result> Handle(SetEmployeePasswordCommand req, CancellationToken ct)
    {
        var callerId = currentUser.GetUserId();
        if (callerId is null)
        {
            return Result.Fail("User not authenticated.");
        }

        if (callerId == req.UserId)
        {
            return Result.Fail("Change your own password from your account settings instead.");
        }

        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(req.UserId, ct);
        var user = await userManager.FindByIdAsync(req.UserId.ToString());

        // The user id is a global master-DB key, so without the tenant check a manager could take
        // over any login on the platform by guessing its id.
        if (employee is null || user is null || user.TenantId != tenantUow.GetCurrentTenant().Id)
        {
            return Result.Fail("Could not find that employee.");
        }

        var targetRoles = await userManager.GetRolesAsync(user);
        if (targetRoles.Contains(AppRoles.SuperAdmin) || targetRoles.Contains(AppRoles.Admin))
        {
            return Result.Fail("Platform admin passwords can't be set from here.");
        }

        if (employee.Role?.Name == TenantRoles.Owner)
        {
            var caller = await userManager.FindByIdAsync(callerId.Value.ToString());
            IList<string> callerRoles = caller is null ? [] : await userManager.GetRolesAsync(caller);
            if (!callerRoles.Contains(AppRoles.SuperAdmin) && !callerRoles.Contains(AppRoles.Admin))
            {
                return Result.Fail("Only Super Admins and Admins can set an Owner's password.");
            }
        }

        // Validated up front: once the old password is removed, a rejected new one would leave the
        // account with no password at all.
        foreach (var validator in userManager.PasswordValidators)
        {
            var check = await validator.ValidateAsync(userManager, user, req.Password);
            if (!check.Succeeded)
            {
                return Fail(check);
            }
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removed = await userManager.RemovePasswordAsync(user);
            if (!removed.Succeeded)
            {
                return Fail(removed);
            }
        }

        var added = await userManager.AddPasswordAsync(user, req.Password);
        return added.Succeeded ? Result.Ok() : Fail(added);
    }

    private static Result Fail(IdentityResult result) =>
        Result.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
}
