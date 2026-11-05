using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetContainerTypesValidator : AbstractValidator<GetContainerTypesQuery>
{
    public GetContainerTypesValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
    }
}
