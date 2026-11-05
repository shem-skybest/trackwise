using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetGensetsValidator : AbstractValidator<GetGensetsQuery>
{
    public GetGensetsValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
    }
}
