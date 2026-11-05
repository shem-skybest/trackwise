using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public class Subscription : Entity, IMasterEntity
{
    public SubscriptionStatus Status { get; set; }
    public required Guid TenantId { get; set; }
    public virtual required Tenant Tenant { get; set; }
    public required Guid PlanId { get; set; }
    public virtual required SubscriptionPlan Plan { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
}
