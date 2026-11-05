using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetPortsValidator : AbstractValidator<GetPortsQuery>
{
    public GetPortsValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
    }
}
