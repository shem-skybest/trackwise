using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Loads.Queries;

internal sealed class GetLoadsValidator : AbstractValidator<GetLoadsQuery>
{
    public GetLoadsValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(0);
        RuleFor(i => i.PageSize).GreaterThanOrEqualTo(1);
        RuleFor(i => i.StartDate)
            .LessThan(i => i.EndDate)
            .When(i => i.StartDate.HasValue && i.EndDate.HasValue);
    }
}
