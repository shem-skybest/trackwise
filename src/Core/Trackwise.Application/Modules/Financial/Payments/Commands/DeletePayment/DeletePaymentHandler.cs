using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class DeletePaymentHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeletePaymentCommand, Payment>(tenantUow);
