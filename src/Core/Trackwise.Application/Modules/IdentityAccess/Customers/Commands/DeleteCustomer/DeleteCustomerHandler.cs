using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Commands;

internal sealed class DeleteCustomerHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteCustomerCommand, Customer>(tenantUow);
