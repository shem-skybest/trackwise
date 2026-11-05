using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetTrailersValidator : AbstractValidator<GetTrailersQuery>
{
    public GetTrailersValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
    }
}
