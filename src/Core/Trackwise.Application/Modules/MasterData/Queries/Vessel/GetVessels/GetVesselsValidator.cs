using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetVesselsValidator : AbstractValidator<GetVesselsQuery>
{
    public GetVesselsValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
    }
}
