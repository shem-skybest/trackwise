using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.CustomerUsers.Commands;

internal sealed class UpdateCustomerUserHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateCustomerUserCommand, Result>
{
    public async Task<Result> Handle(UpdateCustomerUserCommand req, CancellationToken ct)
    {
        var customerUser = await tenantUow.Repository<CustomerUser>().GetByIdAsync(req.Id, ct);

        if (customerUser is null)
        {
            return Result.Fail($"Customer user with ID '{req.Id}' not found.");
        }

        if (req.DisplayName is not null)
        {
            customerUser.DisplayName = req.DisplayName;
        }

        if (req.IsActive.HasValue)
        {
            customerUser.IsActive = req.IsActive.Value;
        }

        tenantUow.Repository<CustomerUser>().Update(customerUser);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
