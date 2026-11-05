using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBusinessPartnersValidator : AbstractValidator<GetBusinessPartnersQuery>
{
    public GetBusinessPartnersValidator()
    {
        RuleFor(i => i.Page).GreaterThan(0);
        RuleFor(i => i.PageSize).GreaterThan(0);
    }
}
