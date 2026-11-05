using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public class PlanFeature : Entity, IMasterEntity
{
    public Guid PlanId { get; set; }
    public TenantFeature Feature { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
