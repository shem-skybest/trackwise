using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.IdentityAccess.CustomerUsers.Commands;

internal sealed class DeleteCustomerUserHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteCustomerUserCommand, CustomerUser>(tenantUow);
