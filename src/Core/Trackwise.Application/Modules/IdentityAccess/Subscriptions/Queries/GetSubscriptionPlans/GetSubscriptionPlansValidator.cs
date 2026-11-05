using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

internal sealed class GetSubscriptionPlansValidator : AbstractValidator<GetSubscriptionPlansQuery>
{
    public GetSubscriptionPlansValidator()
    {
        RuleFor(i => i.Page)
            .GreaterThanOrEqualTo(0);

        RuleFor(i => i.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
