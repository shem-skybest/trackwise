using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Platform.Portal.Queries;

internal sealed class GetCustomerUserHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCustomerUserQuery, Result<CustomerUserDto>>
{
    public async Task<Result<CustomerUserDto>> Handle(
        GetCustomerUserQuery req,
        CancellationToken ct)
    {
        var customerUser = await tenantUow.Repository<CustomerUser>().Query()
            .Where(cu => cu.UserId == req.UserId && cu.IsActive)
            .FirstOrDefaultAsync(ct);

        if (customerUser == null)
        {
            return Result<CustomerUserDto>.Fail("Customer user not found or inactive.");
        }

        return Result<CustomerUserDto>.Ok(customerUser.ToDto());
    }
}
