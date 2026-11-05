using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(i => i.PlanId).NotEmpty();
        RuleFor(i => i.TenantId).NotEmpty();
    }
}
