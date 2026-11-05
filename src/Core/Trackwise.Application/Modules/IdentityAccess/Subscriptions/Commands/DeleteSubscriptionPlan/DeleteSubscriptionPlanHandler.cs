using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class DeleteSubscriptionPlanHandler(IMasterUnitOfWork masterUow)
    : DeleteMasterEntityHandler<DeleteSubscriptionPlanCommand, SubscriptionPlan>(masterUow);
