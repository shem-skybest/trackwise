using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetTrucksValidator : AbstractValidator<GetTrucksQuery>
{
    public GetTrucksValidator()
    {
        RuleFor(i => i.Page).GreaterThan(0);
        RuleFor(i => i.PageSize).GreaterThan(0);
    }
}
